using System.Buffers.Binary;
using Oscilloscope.Server.Application;
using Oscilloscope.Server.Domain;

namespace Oscilloscope.Server.Infrastructure;

public sealed class UdpPacketDeserializer : IUdpPacketDeserializer
{
    public UdpSignalPacket Deserialize(ReadOnlySpan<byte> data)
    {
        // header = int(4) + long(8) + int(4) = 16
        if (data.Length < 16)
            throw new InvalidDataException($"Packet too short: {data.Length}");

        int typeRaw = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0, 4));
        long tsMs = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(4, 8));
        int count = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(12, 4));

        if (!Enum.IsDefined(typeof(SignalType), typeRaw))
            throw new InvalidDataException($"Unknown SignalType: {typeRaw}");

        if (count < 0)
            throw new InvalidDataException($"Invalid sampleCount: {count}");

        int expectedLen = 16 + count * 4;
        if (data.Length != expectedLen)
            throw new InvalidDataException($"Packet length mismatch. Got={data.Length}, Expected={expectedLen}");

        var samples = new float[count];
        int offset = 16;

        for (int i = 0; i < count; i++)
        {
            int bits = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
            samples[i] = BitConverter.Int32BitsToSingle(bits);
            offset += 4;
        }

        var type = (SignalType)typeRaw;
        var ts = DateTimeOffset.FromUnixTimeMilliseconds(tsMs);

        return new UdpSignalPacket(type, ts, samples);
    }
}
