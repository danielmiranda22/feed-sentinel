using FeedSentinel.Worker;
using FeedSentinel.Worker.Config;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace FeedSentinel.Tests;

public class FeedWriterTests : IDisposable
{
    private static readonly Feed Feed = new("tag:test", "Test", "", [new("1", "v1", "https://example.test/1", "", DateTimeOffset.UnixEpoch)]);

    private readonly TempDir _dir = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 3, 14, 30, 0, TimeSpan.Zero));

    public void Dispose() => _dir.Dispose();

    [Fact]
    public async Task Names_the_file_after_the_source_and_the_UTC_time()
    {
        await CreateWriter().SaveDataAsync(Feed, "Runtime", CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_dir.Path, "Runtime-20261103-143000.json")));
    }

    // Second layer of the path traversal defence: names are validated at startup,
    // but the writer must refuse on its own as well.
    [Theory]
    [MemberData(nameof(NamesThatEscape))]
    public async Task Refuses_to_write_outside_the_output_folder(string name)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateWriter().SaveDataAsync(Feed, name, CancellationToken.None));
    }

    // "../" and an absolute path escape on every OS. Backslashes and drive letters
    // only mean something on Windows: on Linux they are plain file-name characters.
    public static TheoryData<string> NamesThatEscape()
    {
        var names = new TheoryData<string> { "../escaped", "/tmp/rooted" };
        if (OperatingSystem.IsWindows()) {
            names.Add(@"..\escaped");
            names.Add("C:/temp/rooted");
        }
        return names;
    }

    private FeedWriter CreateWriter() =>
        new(Options.Create(new WatcherOptions { OutputPath = _dir.Path }), NullLogger<FeedWriter>.Instance, _time);
}