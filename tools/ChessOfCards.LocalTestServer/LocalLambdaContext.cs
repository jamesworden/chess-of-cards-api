using Amazon.Lambda.Core;

namespace ChessOfCards.LocalTestServer;

/// <summary>
/// Mock Lambda context for local testing.
/// </summary>
public class LocalLambdaContext : ILambdaContext
{
    public string AwsRequestId => Guid.NewGuid().ToString();
    public IClientContext ClientContext => null!;
    public string FunctionName => "LocalTestFunction";
    public string FunctionVersion => "1.0";
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn =>
        "arn:aws:lambda:local:123456789012:function:LocalTestFunction";
    public ILambdaLogger Logger { get; }
    public string LogGroupName => "/aws/lambda/LocalTestFunction";
    public string LogStreamName => "local-test-stream";
    public int MemoryLimitInMB => 512;
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);

    public LocalLambdaContext(ILogger logger)
    {
        Logger = new LocalLambdaLogger(logger);
    }
}

/// <summary>
/// Adapter to use ASP.NET Core ILogger as Lambda ILambdaLogger.
/// </summary>
public class LocalLambdaLogger : ILambdaLogger
{
    private readonly ILogger _logger;

    public LocalLambdaLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void Log(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogLine(string message)
    {
        _logger.LogInformation(message);
    }
}
