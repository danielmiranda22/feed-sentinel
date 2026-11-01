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
| `Watcher:Sources` | List of sources (at least one) | required |
| `Sources[].Url` | Feed URL, e.g. `https://github.com/{owner}/{repo}/releases.atom` | required |
| `Sources[].Name` | Letters, digits, space, `-`, `_` (used in file names) | required |
| `Sources[].AllowInsecureHttp` | Allow `http://` for this source | `false` |

Only **new** releases are saved, as `{Name}-{yyyyMMdd-HHmmss}.json`.
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