using System.Threading.Channels;

namespace ECommerceWebsite.Services;

public record EmailQueueItem(string ToEmail, string Subject, string HtmlContent, string? TextContent = null);

public interface IEmailBackgroundQueue
{
    ValueTask QueueEmailAsync(EmailQueueItem item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<EmailQueueItem> DequeueAsync(CancellationToken cancellationToken);
}

public class EmailBackgroundQueue : IEmailBackgroundQueue
{
    private readonly Channel<EmailQueueItem> _channel;

    public EmailBackgroundQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<EmailQueueItem>(options);
    }

    public ValueTask QueueEmailAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(item, cancellationToken);
    }

    public async IAsyncEnumerable<EmailQueueItem> DequeueAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var item))
            {
                yield return item;
            }
        }
    }
}


