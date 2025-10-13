using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository for managing pending games in DynamoDB
/// </summary>
public class PendingGameRepository : IPendingGameRepository
{
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    public PendingGameRepository(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _context = new DynamoDBContext(
            dynamoDbClient,
            new DynamoDBContextConfig { TableNamePrefix = string.Empty }
        );
        _tableName = tableName;
    }

    public async Task<PendingGameRecord> CreateAsync(PendingGameRecord game)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        await _context.SaveAsync(game, config);
        return game;
    }

    public async Task<PendingGameRecord?> GetByGameCodeAsync(string gameCode)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        return await _context.LoadAsync<PendingGameRecord>(gameCode, config);
    }

    public async Task<PendingGameRecord?> GetByHostConnectionIdAsync(string hostConnectionId)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = "HostConnectionIndex",
        };

        var search = _context.QueryAsync<PendingGameRecord>(hostConnectionId, config);
        var results = await search.GetRemainingAsync();
        return results.FirstOrDefault();
    }

    public async Task<List<PendingGameRecord>> GetAllAsync()
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        var search = _context.ScanAsync<PendingGameRecord>(new List<ScanCondition>(), config);
        return await search.GetRemainingAsync();
    }

    public async Task<bool> DeleteAsync(string gameCode)
    {
        try
        {
            var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

            await _context.DeleteAsync<PendingGameRecord>(gameCode, config);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
