using System.ServiceModel.Syndication;
using System.Xml;

namespace FeedSentinel.Worker;

public class FeedParser : IParser
{
    public Feed Parse(string data, CancellationToken cancellationToken = default)
    {
        // Use XmlReaderSettings to prevent DTD (Document Type Definition) processing and disable external entity resolution for security.
        var settings = new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using var reader = XmlReader.Create(new StringReader(data), settings);

        // Read the feed using Atom10FeedFormatter.
        SyndicationFeed feed = SyndicationFeed.Load(reader);

        return new Feed(
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

public interface IParser
{
    Feed Parse(string data, CancellationToken cancellationToken = default);
}


