# FeedSentinel

A .NET 10 Worker Service that periodically fetches data from URLs configured in
`appsettings.json` and saves it to files.

🚧 Work in progress...

## Run

	dotnet run --project FeedSentinel

Configure in `appsettings.json`:

| Key | Description | Default |
|---|---|---|
| `Watcher:Source` | Feed URL (https only) | required |
| `Watcher:Interval` | Time between fetches (1s – 1 day) | `00:00:05` |
| `Watcher:OutputPath` | Folder for saved responses | `data` |
