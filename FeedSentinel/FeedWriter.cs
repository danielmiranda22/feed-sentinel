using FeedSentinel.Worker.Logging;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FeedSentinel.Worker;

public interface IWriter
{
    Task SaveDataAsync(Feed feed, string name, CancellationToken ct);
}

public class FeedWriter : IWriter
{
    private readonly ILogger<FeedWriter> _logger;
    private readonly TimeProvider _provider;
    private readonly string _root;

    public FeedWriter(
        IOptions<WatcherOptions> options,
        ILogger<FeedWriter> logger,
        TimeProvider provider)
    {
        _logger = logger;
        _provider = provider;

        _root = Path.GetFullPath(options.Value.OutputPath);
        if (!Path.EndsInDirectorySeparator(_root)) {
            _root += Path.DirectorySeparatorChar;
        }

        // Create the folder once, not on every save.
        Directory.CreateDirectory(_root);
    }

    public async Task SaveDataAsync(Feed feed, string name, CancellationToken ct)
    {
        var fileName = $"{name}-{_provider.GetUtcNow():yyyyMMdd-HHmmss}.json";

        // GetFullPath resolves "..", mixed slashes and absolute paths,
        // compare where the file will really be written.
        var filePath = Path.GetFullPath(Path.Combine(_root, fileName));

        // names are validated at startup, but never write outside the output folder.
        if (!filePath.StartsWith(_root, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Refusing to write outside '{_root}': '{filePath}'.");
        }

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, feed, JsonOptions, ct);

        _logger.DataSaved(feed.Items.Count, filePath);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
