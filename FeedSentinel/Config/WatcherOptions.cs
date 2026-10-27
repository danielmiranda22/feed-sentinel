using Microsoft.Extensions.Options;
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

    [Required, MinLength(1), ValidateEnumeratedItems]
    public List<WatcherSource>? Sources { get; set; }
}

public class WatcherSource : IValidatableObject
{
    [Required]
    public Uri? Url { get; set; }
    private string _name = string.Empty;
    [Required]
    public string Name {
        get => _name;
        set {
            _name = value?.Trim() ?? string.Empty;
        }
    }
    public bool AllowInsecureHttp { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Url is null)
            yield break;

        var isHttps = Url.Scheme == Uri.UriSchemeHttps;
        var isAllowedHttp = Url.Scheme == Uri.UriSchemeHttp && AllowInsecureHttp;

        if(!isHttps && !isAllowedHttp)
        {
            yield return new ValidationResult(
               $"Source '{Name}' must use https (or set AllowInsecureHttp for http).",
               [nameof(Url)]);
        }
    }
}
