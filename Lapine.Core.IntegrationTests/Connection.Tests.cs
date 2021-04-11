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
                        ClientProvidedName = Random.AlphaNumeric(16),
                        Copyright          = $"Copyright © {Date.Past():yyyy} {Company.CompanyName()}",
                        Information        = Lorem.Sentence(),
                        Platform           = Lorem.Sentence(),
                        Product            = Commerce.ProductName(),
                        Version            = System.Semver(),
                    }
                };
                subject = new AmqpClient(connectionConfiguration);
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to connect".x(async () => {
                await subject.ConnectAsync();
            });
            "Then the broker should report the connected client".x(async () => {
                var connections = await broker.GetConnections().ToListAsync();

                connections.Should().Contain(new BrokerProxy.Connection(
                    AuthMechanism : connectionConfiguration.AuthenticationStrategy.Mechanism,
                    User          : "guest",
                    State         : BrokerProxy.ConnectionState.Running,
                    PeerProperties: connectionConfiguration.PeerProperties
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
                var connections = await broker.GetConnections().ToListAsync();

                connections.Should().BeEmpty();
            });
        }
    }
}
