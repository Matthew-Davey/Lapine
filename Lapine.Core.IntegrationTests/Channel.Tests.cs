namespace Lapine {
    using System;
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    public class ChannelTests : Faker {
        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void RedeclareExchangeWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception exception) {
            "Given a running broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker with an open channel".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
                channel = await subject.OpenChannelAsync();
            }).Teardown(async () => await subject.DisposeAsync());
            "And the broker has an exchange declared".x(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Create(Random.String2(8)));
            });
            "When the client attempts to redeclare the exchange with a different parameter".x(async () => {
                exception = await Record.ExceptionAsync(async () => {
                    await channel.DeclareExchangeAsync(exchangeDefinition with {
                        Durability = Durability.Ephemeral
                    });
                });
            });
            "Then a precondition failed exception is thrown".x(() => {
                exception.Should().NotBeNull();
                exception.Should().BeOfType(Type.GetType("Lapine.Client.PreconditionFailedException, Lapine.Core", throwOnError: true));
            });
        }
    }
}
