using System.Collections.Concurrent;
using System.Threading.Channels;
using Oscilloscope.Server.Application;

namespace Oscilloscope.Server.Infrastructure;

//механізм з backpressure / bounded queue (TODO: drop-oldest, щоб не зїсти RAM).

public sealed class InMemorySignalStreamBroadcaster : ISignalStreamBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<SignalFrame>> _subs = new();

    public void Publish(SignalFrame frame)
    {
        foreach (var (_, ch) in _subs)
            ch.Writer.TryWrite(frame);
    }

    public IAsyncEnumerable<SignalFrame> Subscribe(CancellationToken ct)
    {
        var id = Guid.NewGuid();

        var ch = Channel.CreateBounded<SignalFrame>(new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _subs[id] = ch;
        return ReadAllAndCleanup(id, ch, ct);

        async IAsyncEnumerable<SignalFrame> ReadAllAndCleanup(
            Guid subId,
            Channel<SignalFrame> channel,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(token))
                    yield return item;
            }
            finally
            {
                _subs.TryRemove(subId, out _);
            }
        }
    }
}
