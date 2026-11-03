# FeedSentinel

A .NET 10 Worker Service that watches GitHub release feeds and answers one
question: *did something I depend on ship a new version?*

🚧 Work in progress...

## Run

```bash
dotnet run --project FeedSentinel
```

## Configuration

`appsettings.json`:

| Key | Description | Default |
|---|---|---|
| `Watcher:Interval` | Time between checks (1s – 1 day) | `00:15:00` (`00:00:05` in Development) |
| `Watcher:OutputPath` | Folder for saved releases and `state.json` | `data` |
| `Watcher:MaxConcurrency` | Sources checked at the same time (1–16) | `4` |
| `Watcher:Sources` | List of sources (at least one) | required |
| `Sources[].Url` | Feed URL, e.g. `https://github.com/{owner}/{repo}/releases.atom` | required |
| `Sources[].Name` | Letters, digits, space, `-`, `_` (used in file names) | required |
| `Sources[].AllowInsecureHttp` | Allow `http://` for this source | `false` |

Only **new** releases are saved, as `{Name}-{yyyyMMdd-HHmmss}.json` (UTC).
Releases already seen are kept in `state.json`, so restarts don't repeat them.

## Design decisions

- **Fail fast.** All configuration is validated at startup; an invalid config
  stops the service with exit code 1 and a message naming the bad source.
- **Secure by default.** Sources must use https unless a source explicitly
  opts in with `AllowInsecureHttp`.
- **No writes outside `OutputPath`.** Source names are allow-listed and the
  final path is checked against the output folder (path traversal).
- **Safe XML.** Feeds are parsed with DTD processing disabled (XXE).
- **One failure doesn't stop the watcher.** Network errors, timeouts and
  non-2xx responses are logged per source; the next tick retries.
- **State survives crashes.** `state.json` is written atomically (temp file +
  replace).
- **Bounded concurrency.** Up to `MaxConcurrency` sources are checked at the
  same time with `Parallel.ForEachAsync`; the state store is thread-safe.
- **Testable time.** Timestamps come from an injected `TimeProvider`, not
  `DateTime.Now`.

## Measurements

One check over N sources, each answering after a simulated 200 ms
(`FeedSentinel.Benchmarks`, fake `HttpMessageHandler`, no real network):

| Sources | MaxConcurrency | Sequential | Concurrent | Ratio |
|---|---|---:|---:|---:|
| 10 | 4 | 2,039 ms | 613 ms | 0.30 |
| 10 | 8 | 2,044 ms | 405 ms | 0.20 |
| 50 | 4 | 10,174 ms | 2,677 ms | 0.26 |
| 50 | 8 | 10,177 ms | 1,424 ms | 0.14 |

Allocations are the same in both cases. The work is I/O-bound, so a bounded
`Parallel.ForEachAsync` is enough; Channels were not needed (no producers and
consumers running at different speeds).

```bash
dotnet run -c Release --project FeedSentinel.Benchmarks -- --filter *CheckAllFeeds*
```