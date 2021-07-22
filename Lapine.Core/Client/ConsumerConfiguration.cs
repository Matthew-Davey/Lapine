namespace Lapine.Client {
    using System;

    using static Lapine.Client.Acknowledgements;

    public record ConsumerConfiguration(MessageHandler Handler, Int32 MaxDegreeOfParallelism, Acknowledgements Acknowledgements, Boolean Exclusive) {
        static public ConsumerConfiguration Create(MessageHandler handler) => new(
            Handler               : handler,
            MaxDegreeOfParallelism: 4,
            Acknowledgements      : Manual,
            Exclusive             : false
        );
    }
}
