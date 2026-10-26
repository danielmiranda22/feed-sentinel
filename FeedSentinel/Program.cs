using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptionsWithValidateOnStart<WatcherOptions>()
    .Bind(builder.Configuration.GetSection(WatcherOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(o => o.Source is null || o.Source.Scheme == Uri.UriSchemeHttps,
        "Watcher:Source must use https.");
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();

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