namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using Bogus;
    using Xunit;

    public class QueueDeleteTests : Faker {
        QueueDelete RandomSubject => new (
            queueName : Random.Word(),
            ifUnused  : Random.Bool(),
            ifEmpty   : Random.Bool(),
            noWait    : Random.Bool()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>(8);
            var value  = RandomSubject;

            value.Serialize(buffer);
            QueueDelete.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.IfEmpty, actual: deserialized.IfEmpty);
            Assert.Equal(expected: value.IfUnused, actual: deserialized.IfUnused);
            Assert.Equal(expected: value.NoWait, actual: deserialized.NoWait);
            Assert.Equal(expected: value.QueueName, actual: deserialized.QueueName);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = QueueDelete.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            QueueDelete.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }

    public class QueueDeleteOkTests : Faker {
        QueueDeleteOk RandomSubject => new (
            messageCount : Random.UInt()
        );

        [Fact]
        public void SerializationIsSymmetric() {
            var buffer = new ArrayBufferWriter<Byte>();
            var value  = RandomSubject;

            value.Serialize(buffer);
            QueueDeleteOk.Deserialize(buffer.WrittenMemory.Span, out var deserialized, out var _);

            Assert.Equal(expected: value.MessageCount, actual: deserialized.MessageCount);
        }

        [Fact]
        public void DeserializationFailsWithInsufficientData() {
            var result = QueueDeleteOk.Deserialize(Array.Empty<Byte>(), out var _, out var _);

            Assert.False(result);
        }

        [Fact]
        public void DeserializationReturnsSurplusData() {
            var value  = RandomSubject;
            var extra  = Random.UInt();
            var buffer = new ArrayBufferWriter<Byte>();

            buffer.WriteSerializable(value)
                .WriteUInt32LE(extra);

            QueueDeleteOk.Deserialize(buffer.WrittenMemory.Span, out var _, out var surplus);

            Assert.Equal(expected: sizeof(UInt32), actual: surplus.Length);
            Assert.Equal(expected: extra, actual: BitConverter.ToUInt32(surplus));
        }
    }
}
