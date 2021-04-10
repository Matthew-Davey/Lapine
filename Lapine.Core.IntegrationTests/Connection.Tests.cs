namespace Lapine {
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;

    public class ConnectionTests : Faker {
        [Scenario]
        public void ConnectToLocalBrokerAsGuest(AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration) {
            "Given a running broker".x(async () => {
                broker = await BrokerProxy.StartAsync();
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client configured to connect to the broker".x(async () => {
                connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
                    PeerProperties = PeerProperties.Empty with {
                        Product            = Commerce.Product(),
                        Version            = System.Semver(),
                        ClientProvidedName = Random.AlphaNumeric(16)
                    }
                };
                subject = new AmqpClient(connectionConfiguration);
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to connect".x(async () => {
                await subject.ConnectAsync();
            });
            "Then the broker should report a connected client".x(async () => {
                var connections = await broker.GetConnectionsAsync();

                connections.Should().Contain(new BrokerProxy.Connection(
                    User:    "guest",
                    State:   "running",
                    Product: connectionConfiguration.PeerProperties.Product,
                    Version: connectionConfiguration.PeerProperties.Version,
                    Name:    connectionConfiguration.PeerProperties.ClientProvidedName
                ));
            });
        }
    }
}
