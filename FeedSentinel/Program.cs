using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddOptionsWithValidateOnStart<WatcherOptions>()
    .Bind(builder.Configuration.GetSection(WatcherOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();
