using System.ComponentModel.DataAnnotations;
using FeedSentinel.Worker.Config;

namespace FeedSentinel.Tests;

public class WatcherSourceTests
{
    [Theory]
    [InlineData("https://github.com/dotnet/runtime/releases.atom", false)]
    [InlineData("http://example.test/feed", true)]   // http allowed only by explicit opt-in
    public void Accepts_https_and_opted_in_http(string url, bool allowInsecureHttp)
    {
        var errors = Validate(new WatcherSource { Url = new Uri(url), Name = "Runtime", AllowInsecureHttp = allowInsecureHttp });

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("http://example.test/feed", false)]   // secure by default
    [InlineData("file:///C:/Windows/win.ini", true)]  // the flag never allows other schemes
    [InlineData("ftp://example.test/feed", true)]
    public void Rejects_insecure_or_unknown_schemes(string url, bool allowInsecureHttp)
    {
        var errors = Validate(new WatcherSource { Url = new Uri(url), Name = "Runtime", AllowInsecureHttp = allowInsecureHttp });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WatcherSource.Url)));
    }

    [Theory]
    [InlineData("../escaped")]
    [InlineData(@"..\escaped")]
    [InlineData("C:/temp/rooted")]
    [InlineData("ASP.NET Core")]
    public void Rejects_names_that_could_escape_the_output_folder(string name)
    {
        var errors = Validate(new WatcherSource { Url = new Uri("https://example.test/feed"), Name = name });

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WatcherSource.Name)));
    }

    private static List<ValidationResult> Validate(WatcherSource source)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(source, new ValidationContext(source), errors, validateAllProperties: true);
        return errors;
    }
}