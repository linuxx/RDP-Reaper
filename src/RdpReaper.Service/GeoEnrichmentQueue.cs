using System.Threading.Channels;

namespace RdpReaper.Service;

public sealed class GeoEnrichmentQueue
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    public ValueTask EnqueueAsync(string ip)
    {
        return _channel.Writer.WriteAsync(ip);
    }

    public IAsyncEnumerable<string> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
