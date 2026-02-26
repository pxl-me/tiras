namespace Oscilloscope.Server.Application;

public interface IUdpPacketDeserializer
{
    UdpSignalPacket Deserialize(ReadOnlySpan<byte> data);
}
