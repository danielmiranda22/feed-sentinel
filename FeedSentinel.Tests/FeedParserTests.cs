using System.Xml;
using FeedSentinel.Worker;

namespace FeedSentinel.Tests;

public class FeedParserTests
{
    private readonly FeedParser _parser = new();

    [Fact]
    public void Reads_the_entries_of_an_Atom_feed()
    {
        var feed = _parser.Parse(AtomFeed.With("v1.0.0", "v1.1.0"));

        Assert.Equal(["v1.0.0", "v1.1.0"], feed.Items.Select(i => i.Id));
        Assert.Equal("https://example.test/v1.0.0", feed.Items[0].Link);
        Assert.Equal("Notes for v1.0.0", feed.Items[0].Description);
        Assert.NotEqual(default, feed.Items[0].LastUpdatedTime);
    }

    // XXE / "billion laughs": feeds come from servers we don't control.
    [Fact]
    public void Refuses_XML_with_a_DTD()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE lolz [<!ENTITY lol "lol">]>
            <feed xmlns="http://www.w3.org/2005/Atom"><title>&lol;</title></feed>
            """;

        Assert.Throws<XmlException>(() => _parser.Parse(xml));
    }
}