using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class FeedWriter : IWriter
{
    private readonly ILogger<FeedWriter> _logger;
    private readonly string _root;

    public FeedWriter(IOptions<WatcherOptions> options, ILogger<FeedWriter> logger)
    {
        _logger = logger;

        _root = Path.GetFullPath(options.Value.OutputPath);
        if (!Path.EndsInDirectorySeparator(_root))
            _root += Path.DirectorySeparatorChar;

        // Create the folder once, not on every save.
        Directory.CreateDirectory(_root);
    }

    public async Task SaveDataAsync(string data, string name, CancellationToken ct)
    {
        var fileName = $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.json";

        // GetFullPath resolves "..", mixed slashes and absolute paths,
        // compare where the file will really be written.
        var filePath = Path.GetFullPath(Path.Combine(_root, fileName));

        // names are validated at startup, but never write outside the output folder.
        if (!filePath.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Refusing to write outside '{_root}': '{filePath}'.");

        await File.WriteAllTextAsync(filePath, data, ct);

        _logger.DataSaved(data.Length, filePath);
    }
}

public interface IWriter
{
    Task SaveDataAsync(string data, string name, CancellationToken ct);
}