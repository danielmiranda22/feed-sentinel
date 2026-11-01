using System.ServiceModel.Syndication;
using System.Xml;

namespace FeedSentinel.Worker;

public interface IParser
{
    Feed Parse(string data);
}


public class FeedParser : IParser
{
    public Feed Parse(string data)
    {
        // Use XmlReaderSettings to prevent DTD (Document Type Definition) processing and disable external entity resolution for security.
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(new StringReader(data), settings);

        // Load detects Atom or RSS automatically.
        SyndicationFeed feed = SyndicationFeed.Load(reader);

        return new Feed(
            feed.Id,
            feed.Title.Text,
            feed.Description?.Text ?? string.Empty,
            feed.Items.Select(item => new FeedItem(
                item.Id,
                item.Title.Text,
                item.Links.FirstOrDefault()?.Uri.ToString() ?? string.Empty,
                (item.Content as TextSyndicationContent)?.Text ?? string.Empty,
                item.LastUpdatedTime)).ToList());
    }
}
