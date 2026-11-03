using System.Text.Json;
using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Worker;

/// <summary>
/// Remembers which entries of each source were already seen, across restarts.
/// </summary>
public interface ISeenStore
{
    /// <summary>Ids already seen for this source (empty if never fetched).</summary>
    IReadOnlySet<string> GetSeen(Uri source);

    Task SetSeenAsync(Uri source, IReadOnlySet<string> ids, CancellationToken ct);
}

/// <summary>
/// Keeps the state in memory and persists it to a single JSON file.
/// Not thread-safe: the watcher processes sources one at a time.
/// </summary>
public sealed class JsonSeenStore : ISeenStore
{
    private const string FileName = "state.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    // Key: source URL (stable even if the source is renamed). Value: entry ids.
    private readonly Dictionary<string, HashSet<string>> _state;

    public JsonSeenStore(IOptions<WatcherOptions> options, ILogger<JsonSeenStore> logger)
    {
        var root = Path.GetFullPath(options.Value.OutputPath);
        Directory.CreateDirectory(root);

        _path = Path.Combine(root, FileName);
        _state = Load(_path);

        logger.StateLoaded(_state.Count, _path);
    }

    public IReadOnlySet<string> GetSeen(Uri source) =>
      _state.TryGetValue(source.AbsoluteUri, out var ids) ? ids : [];

    public async Task SetSeenAsync(Uri source, IReadOnlySet<string> ids, CancellationToken ct)
    {
        _state[source.AbsoluteUri] = [.. ids];

        // Atomic write: write a temp file, then replace the real one in a single step.
        // A crash mid-write leaves the previous state.json intact.
        var tempPath = _path + ".tmp";
        await using (var stream = File.Create(tempPath)) {
            await JsonSerializer.SerializeAsync(stream, _state, JsonOptions, ct);
            stream.Flush(flushToDisk: true);
        }
        File.Move(tempPath, _path, overwrite: true);
    }

    private static Dictionary<string, HashSet<string>> Load(string path)
    {
        if (!File.Exists(path)) {
            return [];
        }

        try {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<Dictionary<string, HashSet<string>>>(stream) ?? [];
        } catch (JsonException ex) {
            // Fail fast: a corrupted state file is a real problem; don't hide it by starting over.
            throw new InvalidOperationException(
                $"State file '{path}' is corrupted. Fix it or delete it to start over.", ex);
        }
    }
}