namespace Lapine {
    using System;
    using System.Linq;
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;

    public class ConnectionTests : Faker {
        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void ConnectToLocalBrokerAsGuest(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration) {
            "Given a running broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
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
            "Then the broker should report the connected client".x(async () => {
                var connections = await broker.GetConnectionsAsync().ToListAsync();

                connections.Should().Contain(new BrokerProxy.Connection(
                    User:    "guest",
                    State:   "running",
                    Product: connectionConfiguration.PeerProperties.Product,
                    Version: connectionConfiguration.PeerProperties.Version,
                    Name:    connectionConfiguration.PeerProperties.ClientProvidedName
                ));
            });
        }

        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DisconnectFromLocalBroker(String brokerVersion, AmqpClient subject, BrokerProxy broker) {
            "Given a running broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client connected to the broker".x(async () => {
                subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
                await subject.ConnectAsync();
            });
            "When the client disconnects".x(async () => {
                await subject.DisposeAsync();
            });
            "Then the broker should report no open connections".x(async () => {
                var connections = await broker.GetConnectionsAsync().ToListAsync();

                connections.Should().BeEmpty();
            });
        }
    }
}
