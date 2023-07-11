namespace Lapine.Client;

public class ConnectionTests : Faker {
    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void ConnectAsGuest(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client configured to connect to the broker".x(async () => {
            connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
                AuthenticationStrategy = new PlainAuthenticationStrategy("guest", "guest"),
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
            var connections = await broker.GetConnectionsAsync().ToListAsync();

            connections.Should().Contain(new BrokerProxy.Connection(
                AuthMechanism : connectionConfiguration.AuthenticationStrategy.Mechanism,
                User          : "guest",
                State         : BrokerProxy.ConnectionState.Running,
                PeerProperties: connectionConfiguration.PeerProperties
            ));
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void ConnectAsUser(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration, String username, String password) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has a configured user".x(async () => {
            await broker.AddUserAsync(username = Person.UserName, password = Random.Utf16String(16));
            await broker.SetPermissionsAsync("/", username);
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
            var connections = await broker.GetConnectionsAsync().ToListAsync();

            connections.Should().Contain(new BrokerProxy.Connection(
                AuthMechanism : connectionConfiguration.AuthenticationStrategy.Mechanism,
                User          : username,
                State         : BrokerProxy.ConnectionState.Running,
                PeerProperties: connectionConfiguration.PeerProperties
            ));
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void ConnectWithInvalidCredentials(String brokerVersion, AmqpClient subject, BrokerProxy broker, Exception? connectionError) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client configured to connect to the broker as an invalid user".x(async () => {
            var connectionConfiguration = await broker.GetConnectionConfigurationAsync() with {
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
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void Disconnect(String brokerVersion, AmqpClient subject, BrokerProxy broker) {
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
            var connections = await broker.GetConnectionsAsync().ToListAsync();

            connections.Should().BeEmpty();
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void ConnectToVirtualHost(String brokerVersion, AmqpClient subject, BrokerProxy broker, ConnectionConfiguration connectionConfiguration, String virtualHost) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has a virtual host configured".x(async () => {
            await broker.AddVirtualHostAsync(virtualHost = Random.AlphaNumeric(8));
            await broker.SetPermissionsAsync(virtualHost, "guest");
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
            var connections = await broker.GetConnectionsAsync().ToListAsync();

            connections.Should().Contain(new BrokerProxy.Connection(
                AuthMechanism : connectionConfiguration.AuthenticationStrategy.Mechanism,
                User          : "guest",
                State         : BrokerProxy.ConnectionState.Running,
                PeerProperties: connectionConfiguration.PeerProperties
            ));
        });
    }
}
