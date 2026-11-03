using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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

    // How many sources are checked at the same time. 1 = one after the other.
    [Range(1, 16)]
    public int MaxConcurrency { get; set; } = 4;
}

public partial class WatcherSource : IValidatableObject
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

        if (!isHttps && !isAllowedHttp) {
            yield return new ValidationResult(
               $"Source '{Name}' must use https (or set AllowInsecureHttp for http).", [nameof(Url)]
               );
        }

        Regex regex = AllowedListRegex();
        Match match = regex.Match(Name);
        if (!match.Success) {
            yield return new ValidationResult(
                $"Source '{Name}' contains invalid characters. Only letters, numbers, spaces, underscores, and hyphens are allowed.", [nameof(Name)]
            );
        }

    }

    [GeneratedRegex(@"^[A-Za-z0-9 _-]+$")]
    private static partial Regex AllowedListRegex();
}
