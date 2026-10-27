using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

public class FeedWriter(
    IOptions<WatcherOptions> options,
    ILogger<FeedWriter> logger) : IWriter
{
    private readonly string _outputPath = options.Value.OutputPath;

    public async Task SaveDataAsync(
        string data,
        string name,
        CancellationToken ct)
    {
        Directory.CreateDirectory(_outputPath);

        var fileName = $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_outputPath, fileName);

        await File.WriteAllTextAsync(filePath, data, ct);
     
        logger.DataSaved(data.Length, filePath);
    }
}

public interface IWriter
{
    Task SaveDataAsync(string data, string name, CancellationToken ct);
}