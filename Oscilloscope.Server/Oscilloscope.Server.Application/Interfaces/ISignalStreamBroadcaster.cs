namespace Oscilloscope.Server.Application;

public interface ISignalStreamBroadcaster
{
    void Publish(SignalFrame frame);
    IAsyncEnumerable<SignalFrame> Subscribe(CancellationToken ct);
}
