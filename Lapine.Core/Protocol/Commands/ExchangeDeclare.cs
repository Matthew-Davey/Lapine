namespace Lapine.Protocol.Commands {
    using System;
    using System.Buffers;
    using System.Collections.Generic;

    sealed class ExchangeDeclare : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0A);

        public String ExchangeName { get; }
        public String ExchangeType { get; }
        public Boolean Passive { get; }
        public Boolean Durable { get; }
        public Boolean AutoDelete { get; }
        public Boolean Internal { get; }
        public Boolean NoWait { get; }
        public IReadOnlyDictionary<String, Object> Arguments { get; }

        public ExchangeDeclare(String exchangeName, String exchangeType, Boolean passive, Boolean durable, Boolean autoDelete, Boolean @internal, Boolean noWait, IReadOnlyDictionary<String, Object> arguments) {
            ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            ExchangeType = exchangeType ?? throw new ArgumentNullException(nameof(exchangeType));
            Passive      = passive;
            AutoDelete   = autoDelete;
            Durable      = durable;
            Internal     = @internal;
            NoWait       = noWait;
            Arguments    = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer
                .WriteInt16BE(0) // reserved-1
                .WriteShortString(ExchangeName)
                .WriteShortString(ExchangeType)
                .WriteBits(Passive, Durable, AutoDelete, Internal, NoWait)
                .WriteFieldTable(Arguments);

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ExchangeDeclare result, out ReadOnlySpan<Byte> surplus) {
            if (buffer.ReadShortString(out var exchangeName, out surplus) &&
                surplus.ReadShortString(out var exchangeType, out surplus) &&
                surplus.ReadBits(out var bits, out surplus) &&
                surplus.ReadFieldTable(out var arguments, out surplus))
            {
                result = new ExchangeDeclare(exchangeName, exchangeType, bits[0], bits[1], bits[2], bits[3], bits[4], arguments);
                return true;
            }
            else {
                result = default;
                return false;
            }
        }
    }

    sealed class ExchangeDeclareOk : ICommand {
        public (Byte ClassId, Byte MethodId) CommandId => (0x28, 0x0B);

        public IBufferWriter<Byte> Serialize(IBufferWriter<Byte> writer) =>
            writer;

        static public Boolean Deserialize(in ReadOnlySpan<Byte> buffer, out ExchangeDeclareOk result, out ReadOnlySpan<Byte> surplus) {
            surplus = buffer;
            result = new ExchangeDeclareOk();
            return true;
        }
    }
}
