using System.ComponentModel.DataAnnotations;

namespace FeedSentinel.Worker.Config;

public class WatcherOptions
{
    public const string SectionName = "Watcher";

    [Required]
    public string OutputPath { get; set; } = "data";

    // Significant: The interval must be between 1 seconds and 1 day (24 hours).
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);

    [Required]
    public Uri? Source { get; set; }
}
