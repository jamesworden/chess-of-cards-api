using Amazon.Lambda.Core;

namespace ChessOfCards.ConnectionHandler.Tests.Mocks;

/// <summary>
/// Mock implementation of ILambdaContext for testing.
/// </summary>
public class MockLambdaContext : ILambdaContext
{
    private readonly List<string> _logs = new();

    public string AwsRequestId => "test-request-id";
    public IClientContext ClientContext => null!;
    public string FunctionName => "TestFunction";
    public string FunctionVersion => "1.0";
    public ICognitoIdentity Identity => null!;
    public string InvokedFunctionArn =>
        "arn:aws:lambda:us-east-1:123456789012:function:TestFunction";
    public ILambdaLogger Logger => new MockLambdaLogger(_logs);
    public string LogGroupName => "/aws/lambda/TestFunction";
    public string LogStreamName => "2024/01/01/[$LATEST]test";
    public int MemoryLimitInMB => 512;
    public TimeSpan RemainingTime => TimeSpan.FromMinutes(5);

    public IReadOnlyList<string> Logs => _logs;
}

/// <summary>
/// Mock implementation of ILambdaLogger.
/// </summary>
public class MockLambdaLogger : ILambdaLogger
{
    private readonly List<string> _logs;

    public MockLambdaLogger(List<string> logs)
    {
        _logs = logs;
    }

    public void Log(string message)
    {
        _logs.Add(message);
    }

    public void LogLine(string message)
    {
        _logs.Add(message);
    }
}
