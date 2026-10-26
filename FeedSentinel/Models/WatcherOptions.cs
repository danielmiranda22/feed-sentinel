using System.ComponentModel.DataAnnotations;

namespace FeedSentinel.Worker.Models;

public class WatcherOptions
{
    public const string SectionName = "Watcher";

    [Required]
    public string OutputPath { get; set; } = "temp";

    // Significant: The interval must be between 0 seconds and 1 day (24 hours).
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);

    [Required]
    public string Source { get; set; } = string.Empty;
}
