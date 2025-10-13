using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository for managing game timers in DynamoDB
/// </summary>
public class GameTimerRepository : IGameTimerRepository
{
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    public GameTimerRepository(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _context = new DynamoDBContext(dynamoDbClient, new DynamoDBContextConfig
        {
            TableNamePrefix = string.Empty
        });
        _tableName = tableName;
    }

    public async Task<GameTimerRecord> CreateAsync(GameTimerRecord timer)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        await _context.SaveAsync(timer, config);
        return timer;
    }

    public async Task<GameTimerRecord?> GetByIdAsync(string timerId)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        return await _context.LoadAsync<GameTimerRecord>(timerId, config);
    }

    public async Task<List<GameTimerRecord>> GetExpiringTimersAsync(string timerType, long expiresBeforeTimestamp)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = "ExpiryIndex",
            QueryFilter = new List<ScanCondition>
            {
                new ScanCondition("expiresAt", ScanOperator.LessThanOrEqual, expiresBeforeTimestamp)
            }
        };

        var search = _context.QueryAsync<GameTimerRecord>(timerType, config);
        return await search.GetRemainingAsync();
    }

    public async Task<GameTimerRecord> UpdateAsync(GameTimerRecord timer)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName
        };

        await _context.SaveAsync(timer, config);
        return timer;
    }

    public async Task<bool> DeleteAsync(string timerId)
    {
        try
        {
            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = _tableName
            };

            await _context.DeleteAsync<GameTimerRecord>(timerId, config);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAllForGameAsync(string gameCode)
    {
        try
        {
            // Query all timers with IDs starting with this game code
            var config = new DynamoDBOperationConfig
            {
                OverrideTableName = _tableName
            };

            // Scan for timers matching this game code (not ideal but works for cleanup)
            var search = _context.ScanAsync<GameTimerRecord>(
                new List<ScanCondition>
                {
                    new ScanCondition("gameCode", ScanOperator.Equal, gameCode)
                },
                config
            );

            var timers = await search.GetRemainingAsync();

            foreach (var timer in timers)
            {
                await DeleteAsync(timer.TimerId);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
