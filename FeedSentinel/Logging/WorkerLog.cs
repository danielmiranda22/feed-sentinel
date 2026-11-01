namespace FeedSentinel.Worker.Logging;

public static partial class WorkerLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Critical, Message = "Configuration is invalid: {Failures}")]
    public static partial void ConfigurationInvalid(this ILogger logger, string failures);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Critical, Message = "Host terminated unexpectedly")]
    public static partial void HostTerminated(this ILogger logger, Exception ex);


    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Sentinel started, watching {Count} sources every {Interval}")]
    public static partial void SentinelStarted(this ILogger logger, int count, TimeSpan interval);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Requesting {Url}")]
    public static partial void RequestingData(this ILogger logger, Uri url);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Saved {Count} entries to {FilePath}")]
    public static partial void DataSaved(this ILogger logger, int count, string filePath);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error, Message = "Failed to fetch {Url}")]
    public static partial void FetchFailed(this ILogger logger, Uri url, Exception ex);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Warning, Message = "Source '{Name}' uses insecure HTTP: {Url}")]
    public static partial void InsecureSourceAllowed(this ILogger logger, string name, Uri url);
}
