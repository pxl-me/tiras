using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscilloscope.Emulator.transport
{
    using System.Buffers.Binary;
    using Oscilloscope.Emulator.models;

    public sealed class UdpPacketSerializer
    {
        // Header: int signalType (4 bytes) + long timestamp (8 bytes) + int sampleCount (4 bytes for count = 100)
        // Payload: sampleCount * float (400 bytes -> float is 4 bytes(?), count is 100)
        public byte[] Serialize(SignalType type, long timestamp, ReadOnlySpan<float> samples)
        {
            const int headerSize = 4 + 8 + 4;

            var totalSize = headerSize + samples.Length * 4;
            var bytes = new byte[totalSize];

            var span = bytes.AsSpan();

            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0, 4), (int)type);
            BinaryPrimitives.WriteInt64LittleEndian(span.Slice(4, 8), timestamp);
            BinaryPrimitives.WriteInt32LittleEndian(span.Slice(12, 4), samples.Length);

            int offset = headerSize;

            for (int i = 0; i < samples.Length; i++)
            {
                // float -> int bits -> write little-endian
                int bits = BitConverter.SingleToInt32Bits(samples[i]);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(offset, 4), bits);
                offset += 4;
            }

            return bytes;
        }
    }
}
