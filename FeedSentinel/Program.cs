using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptionsWithValidateOnStart<WatcherOptions>()
    .Bind(builder.Configuration.GetSection(WatcherOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddHostedService<Watcher>();
builder.Services.AddHttpClient();

// DI
builder.Services.AddTransient<IScraper, FeedFetcher>();
builder.Services.AddTransient<IWriter, FeedWriter>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try {
    await host.StartAsync();
    await host.WaitForShutdownAsync();
    return 0;
} catch (OptionsValidationException ex) {
    logger.ConfigurationInvalid(string.Join("; ", ex.Failures));
    return 1;
} catch (Exception ex) {
    logger.HostTerminated(ex);
    return 1;
}