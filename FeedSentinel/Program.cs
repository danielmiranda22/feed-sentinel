using FeedSentinel.Worker;
using FeedSentinel.Worker.Logging;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptionsWithValidateOnStart<WatcherOptions>()
    .Bind(builder.Configuration.GetSection(WatcherOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddHostedService<Watcher>();

// DI
builder.Services.AddTransient<IFeedChecker, FeedChecker>();
// The watcher lives for the whole process, so this client is never recycled by the factory.
// Recycle connections ourselves so DNS changes are picked up.
builder.Services.AddHttpClient<IFeedFetcher, FeedFetcher>()
    .UseSocketsHttpHandler((handler, _) => handler.PooledConnectionLifetime = TimeSpan.FromMinutes(5))
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan); 
builder.Services.AddTransient<IParser, FeedParser>();
builder.Services.AddTransient<IWriter, FeedWriter>();
// Singleton: holds the seen ids in memory for the whole lifetime of the service.
builder.Services.AddSingleton<ISeenStore, JsonSeenStore>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try {
    await host.StartAsync();
    await host.WaitForShutdownAsync();
    return 0;
} catch (OptionsValidationException ex) {
    logger.ConfigurationInvalid(failures: string.Join("; ", ex.Failures));
    return 1;
} catch (Exception ex) {
    logger.HostTerminated(ex);
    return 1;
}