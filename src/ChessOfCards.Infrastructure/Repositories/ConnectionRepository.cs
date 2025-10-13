using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using ChessOfCards.Infrastructure.Models;

namespace ChessOfCards.Infrastructure.Repositories;

/// <summary>
/// Repository for managing WebSocket connections in DynamoDB
/// </summary>
public class ConnectionRepository : IConnectionRepository
{
    private readonly IDynamoDBContext _context;
    private readonly string _tableName;

    public ConnectionRepository(IAmazonDynamoDB dynamoDbClient, string tableName)
    {
        _context = new DynamoDBContext(
            dynamoDbClient,
            new DynamoDBContextConfig { TableNamePrefix = string.Empty }
        );
        _tableName = tableName;
    }

    public async Task<ConnectionRecord> CreateAsync(ConnectionRecord connection)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        await _context.SaveAsync(connection, config);
        return connection;
    }

    public async Task<ConnectionRecord?> GetByConnectionIdAsync(string connectionId)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        return await _context.LoadAsync<ConnectionRecord>(connectionId, config);
    }

    public async Task<List<ConnectionRecord>> GetByGameCodeAsync(string gameCode)
    {
        var config = new DynamoDBOperationConfig
        {
            OverrideTableName = _tableName,
            IndexName = "GameCodeIndex",
        };

        var search = _context.QueryAsync<ConnectionRecord>(gameCode, config);
        return await search.GetRemainingAsync();
    }

    public async Task<ConnectionRecord> UpdateAsync(ConnectionRecord connection)
    {
        var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

        await _context.SaveAsync(connection, config);
        return connection;
    }

    public async Task<bool> DeleteAsync(string connectionId)
    {
        try
        {
            var config = new DynamoDBOperationConfig { OverrideTableName = _tableName };

            await _context.DeleteAsync<ConnectionRecord>(connectionId, config);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
