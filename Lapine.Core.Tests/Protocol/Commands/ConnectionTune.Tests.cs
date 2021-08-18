namespace Lapine.Protocol.Commands {
    using System;
    using Bogus;
    using Xunit;

    public class ConnectionTuneTests : Faker {
        ConnectionTune RandomSubject => new (ChannelMax: Random.UShort(), FrameMax: Random.UInt(), Heartbeat: Random.UShort());

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            ConnectionTune.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ChannelMax, actual: deserialized?.ChannelMax);
            Assert.Equal(expected: value.FrameMax, actual: deserialized?.FrameMax);
            Assert.Equal(expected: value.Heartbeat, actual: deserialized?.Heartbeat);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ConnectionTune.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ConnectionTune.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class ConnectionTuneOkTests : Faker {
        ConnectionTuneOk RandomSubject => new (ChannelMax: Random.UShort(), FrameMax: Random.UInt(), Heartbeat: Random.UShort());

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new MemoryBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            ConnectionTuneOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.ChannelMax, actual: deserialized?.ChannelMax);
            Assert.Equal(expected: value.FrameMax, actual: deserialized?.FrameMax);
            Assert.Equal(expected: value.Heartbeat, actual: deserialized?.Heartbeat);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = ConnectionTuneOk.Deserialize(Span<Byte>.Empty, out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new MemoryBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            ConnectionTuneOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
