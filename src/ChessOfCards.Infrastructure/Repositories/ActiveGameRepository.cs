using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository for managing active games in DynamoDB
/// </summary>
public class ActiveGameRepository : IActiveGameRepository
{
    private readonly IDynamoDBContext _context;
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public ActiveGameRepository(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _client = dynamoDbClient;
        _context = new DynamoDBContext(dynamoDbClient, new DynamoDBContextConfig
        {
            TableNamePrefix = string.Empty
        });
        _tableName = tableName;
    }

    public async Task<ActiveGameRecord> CreateAsync(ActiveGameRecord game)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        await _context.SaveAsync(game, config);
        return game;
    }

    public async Task<ActiveGameRecord?> GetByGameCodeAsync(string gameCode)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        return await _context.LoadAsync<ActiveGameRecord>(gameCode, config);
    }

    public async Task<ActiveGameRecord?> GetByConnectionIdAsync(string connectionId)
    {
        // Try host connection index
        var hostConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = "HostConnectionIndex"
        };

        var hostSearch = _context.QueryAsync<ActiveGameRecord>(connectionId, hostConfig);
        var hostResults = await hostSearch.GetRemainingAsync();
        if (hostResults.Any())
        {
            return hostResults.First();
        }

        // Try guest connection index
        var guestConfig = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = "GuestConnectionIndex"
        };

        var guestSearch = _context.QueryAsync<ActiveGameRecord>(connectionId, guestConfig);
        var guestResults = await guestSearch.GetRemainingAsync();
        if (guestResults.Any())
        {
            return guestResults.First();
        }

        return null;
    }

    public async Task<ActiveGameRecord?> UpdateAsync(ActiveGameRecord game)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var newVersion = game.Version + 1;

            var request = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "gameCode", new AttributeValue { S = game.GameCode } }
                },
                UpdateExpression = "SET gameState = :gameState, " +
                                   "isHostPlayersTurn = :isHostPlayersTurn, " +
                                   "hasEnded = :hasEnded, " +
                                   "wonBy = :wonBy, " +
                                   "updatedAt = :updatedAt, " +
                                   "#version = :newVersion, " +
                                   "hostDisconnectedAt = :hostDisconnectedAt, " +
                                   "guestDisconnectedAt = :guestDisconnectedAt",
                ConditionExpression = "#version = :expectedVersion",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    { "#version", "version" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":gameState", new AttributeValue { S = game.GameState } },
                    { ":isHostPlayersTurn", new AttributeValue { BOOL = game.IsHostPlayersTurn } },
                    { ":hasEnded", new AttributeValue { BOOL = game.HasEnded } },
                    { ":wonBy", new AttributeValue { S = game.WonBy } },
                    { ":updatedAt", new AttributeValue { N = now.ToString() } },
                    { ":expectedVersion", new AttributeValue { N = game.Version.ToString() } },
                    { ":newVersion", new AttributeValue { N = newVersion.ToString() } },
                    { ":hostDisconnectedAt", game.HostDisconnectedAt.HasValue
                        ? new AttributeValue { N = game.HostDisconnectedAt.Value.ToString() }
                        : new AttributeValue { NULL = true } },
                    { ":guestDisconnectedAt", game.GuestDisconnectedAt.HasValue
                        ? new AttributeValue { N = game.GuestDisconnectedAt.Value.ToString() }
                        : new AttributeValue { NULL = true } }
                },
                ReturnValues = ReturnValue.ALL_NEW
            };

            var response = await _client.UpdateItemAsync(request);

            // Update the game object with new version
            game.Version = newVersion;
            game.UpdatedAt = now;

            return game;
        }
        catch (ConditionalCheckFailedException)
        {
            // Version mismatch - optimistic locking failed
            Console.WriteLine($"Optimistic locking failed for game {game.GameCode}");
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string gameCode)
    {
        try
        {
            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = _tableName
            };

            await _context.DeleteAsync<ActiveGameRecord>(gameCode, config);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
