using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FeedSentinel.Tests;

public class JsonSeenStoreTests : IDisposable
{
    private static readonly Uri Runtime = new("https://example.test/runtime/releases.atom");

    private readonly TempDir _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task Remembers_ids_across_restarts()
    {
        await CreateStore().SetSeenAsync(Runtime, new HashSet<string> { "v1", "v2" }, CancellationToken.None);

        var afterRestart = CreateStore();

        Assert.Equal(["v1", "v2"], afterRestart.GetSeen(Runtime).Order());
    }

    // Sources are checked concurrently, so writes to state.json happen at the same time.
    [Fact]
    public async Task Survives_concurrent_writes()
    {
        var store = CreateStore();

        await Parallel.ForEachAsync(Enumerable.Range(0, 200), new ParallelOptions { MaxDegreeOfParallelism = 16 }, async (i, ct) => {
            var url = new Uri($"https://example.test/repo-{i % 50}/releases.atom");
            _ = store.GetSeen(url);
            await store.SetSeenAsync(url, new HashSet<string> { $"id-{i}" }, ct);
        });

        Assert.Empty(Directory.GetFiles(_dir.Path, "*.tmp"));
        Assert.Single(CreateStore().GetSeen(new Uri("https://example.test/repo-0/releases.atom")));
    }

    // Fail fast: a corrupted state file is a real problem, not something to hide.
    [Fact]
    public void Refuses_a_corrupted_state_file()
    {
        File.WriteAllText(Path.Combine(_dir.Path, "state.json"), """{"broken":""");

        Assert.Throws<InvalidOperationException>(CreateStore);
    }

    private JsonSeenStore CreateStore() =>
        new(Options.Create(new WatcherOptions { OutputPath = _dir.Path }), NullLogger<JsonSeenStore>.Instance);
}