namespace Lapine {
    using System;
    using System.Linq;
    using Lapine.Client;
    using Bogus;
    using FluentAssertions;
    using Xbehave;
    using Xunit;

    public class ConnectionTests : Faker {
        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void ConnectToLocalBrokerAsGuest(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
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
        public void ConnectToLocalBrokerAsUser(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration, String username, String password) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a configured user".x(async () => {
                await broker.AddUser(username = Person.UserName, password = Random.Utf16String(16));
                await broker.SetPermissions("/", username);
            });
            "And a client configured to connect to the broker as that user".x(async () => {
                connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
                    AuthenticationStrategy = new PlainAuthenticationStrategy(username, password)
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
                    User          : username,
                    State         : BrokerProxy.ConnectionState.Running,
                    PeerProperties: connectionConfiguration.PeerProperties
                ));
            });
        }

        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void ConnectToLocalBrokerWithInvalidCredentials(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration, Exception connectionError) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And a client configured to connect to the broker as an invalid user".x(async () => {
                connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
                    AuthenticationStrategy = new PlainAuthenticationStrategy(Person.UserName, Random.Utf16String(16))
                };
                subject = new AmqpClient(connectionConfiguration);
            }).Teardown(async () => await subject.DisposeAsync());
            "When the client attempts to connect".x(async () => {
                connectionError = await Record.ExceptionAsync(async () => await subject.ConnectAsync());
            });
            "Then the client should have thrown a connection error".x(() => {
                connectionError.Should().NotBeNull();
            });
        }

        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void DisconnectFromLocalBroker(String brokerVersion, AmqpClient subject, BrokerProxy broker) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
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

        [Scenario]
        [Example("3.8-alpine")]
        [Example("3.7-alpine")]
        public void ConnectToVirtualHost(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration, String virtualHost) {
            $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
                broker = await BrokerProxy.StartAsync(brokerVersion);
            }).Teardown(async () => await broker.DisposeAsync());
            "And the broker has a virtual host configured".x(async () => {
                await broker.AddVirtualHost(virtualHost = Random.AlphaNumeric(8));
                await broker.SetPermissions(virtualHost, "guest");
            });
            "And a client configured to connect to the broker".x(async () => {
                connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
                    VirtualHost = virtualHost
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
    }
}
