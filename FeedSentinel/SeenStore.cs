using FeedSentinel.Worker.Config;
using FeedSentinel.Worker.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

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
/// Thread-safe: sources are checked concurrently.
/// </summary>
public sealed class JsonSeenStore : ISeenStore
{
    private const string FileName = "state.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;
    // Key: source URL (stable even if the source is renamed). Value: entry ids.
    private readonly ConcurrentDictionary<string, HashSet<string>> _state;

    // Only one write to state.json at a time (they share the same .tmp file).
    private readonly SemaphoreSlim _writeLock = new(1, 1);

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
        // Replace the set, never change it: a reader holding the old set is not affected.
        _state[source.AbsoluteUri] = [.. ids];

        await _writeLock.WaitAsync(ct);
        try {
            // Atomic write: write a temp file, then replace the real one in a single step.
            // A crash mid-write leaves the previous state.json intact.
            var tempPath = _path + ".tmp";
            await using (var stream = File.Create(tempPath)) {
                await JsonSerializer.SerializeAsync(stream, _state, JsonOptions, ct);
                stream.Flush(flushToDisk: true);
            }
            File.Move(tempPath, _path, overwrite: true);
        } finally {
            _writeLock.Release();
        }
    }

    private static ConcurrentDictionary<string, HashSet<string>> Load(string path)
    {
        if (!File.Exists(path)) {
            return [];
        }

        try {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, HashSet<string>>>(stream) ?? [];
        } catch (JsonException ex) {
            // Fail fast: a corrupted state file is a real problem; don't hide it by starting over.
            throw new InvalidOperationException(
                $"State file '{path}' is corrupted. Fix it or delete it to start over.", ex);
        }
    }
}