# FeedSentinel

A .NET 10 Worker Service that periodically fetches data from URLs configured in
`appsettings.json` and saves it to files.

🚧 Work in progress...

## Run

	dotnet run --project FeedSentinel

## Configuration

`appsettings.json`:

| Key | Description | Default |
|---|---|---|
| `Watcher:Interval` | Time between fetches (1s – 1 day) | `00:00:05` |
| `Watcher:OutputPath` | Folder for saved responses | `data` |
| `Watcher:Sources` | List of sources (at least one) | required |
| `Sources[].Url` | Source URL | required |
| `Sources[].Name` | Letters, digits, space, `-`, `_` (used in file names) | required |
| `Sources[].AllowInsecureHttp` | Allow `http://` for this source | `false` |

Each response is saved as `{Name}-{yyyyMMdd-HHmmss}.json` in `OutputPath`.

## Design decisions

- **Fail fast.** All configuration is validated at startup; an invalid config
  stops the service with exit code 1 and a message naming the bad source.
- **Secure by default.** Sources must use https unless a source explicitly
  opts in with `AllowInsecureHttp`.
- **No writes outside `OutputPath`.** Source names are allow-listed and the
  final path is checked against the output folder (path traversal).
- **One failure doesn't stop the watcher.** Network errors, timeouts and
  non-2xx responses are logged per source; the next tick retries.