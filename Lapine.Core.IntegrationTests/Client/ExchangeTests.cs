namespace Lapine.Client;

public class ExchangeTests : Faker {
    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareDirectExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client declares a direct exchange".x(async () => {
            await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Direct(Random.String2(12)));
        });
        "Then the exchange is created on the broker".x(async () => {
            var exchanges = await broker.GetExchangesAsync().ToListAsync();

            exchanges.Should().Contain(exchangeDefinition);
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareFanoutExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client declares a fanout exchange".x(async () => {
            await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Fanout(Random.String2(12)));
        });
        "Then the exchange is created on the broker".x(async () => {
            var exchanges = await broker.GetExchangesAsync().ToListAsync();

            exchanges.Should().Contain(exchangeDefinition);
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareHeadersExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client declares a headers exchange".x(async () => {
            await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Headers(Random.String2(12)));
        });
        "Then the exchange is created on the broker".x(async () => {
            var exchanges = await broker.GetExchangesAsync().ToListAsync();

            exchanges.Should().Contain(exchangeDefinition);
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareTopicExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client declares a topic exchange".x(async () => {
            await channel.DeclareExchangeAsync(exchangeDefinition = ExchangeDefinition.Topic(Random.String2(12)));
        });
        "Then the exchange is created on the broker".x(async () => {
            var exchanges = await broker.GetExchangesAsync().ToListAsync();

            exchanges.Should().Contain(exchangeDefinition);
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareExchangeWithInsufficientPermission(String brokerVersion, BrokerProxy broker, String user, String password, AmqpClient subject, Channel channel, Exception? exception) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has a user without permission to declare an exchange".x(async () => {
            await broker.AddUserAsync(user = Person.UserName, password = Random.String2(16));
            await broker.SetPermissionsAsync("/", user, configure: "^$");
        });
        "And a client connected to the broker as the user".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync() with {
                AuthenticationStrategy = new PlainAuthenticationStrategy(user, password)
            });
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client declares a topic exchange".x(async () => {
            exception = await Record.ExceptionAsync(async () => {
                await channel.DeclareExchangeAsync(ExchangeDefinition.Direct(Random.String2(12)));
            });
        });
        "Then an access refused exception is thrown".x(() => {
            exception.Should().NotBeNull();
            exception.Should().BeOfType(Type.GetType("Lapine.Client.AccessRefusedException, Lapine.Core"));
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void RedeclareExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception? exception) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has an exchange declared".x(async () => {
            exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
            await broker.ExchangeDeclareAsync(exchangeDefinition);
        });
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client attempts to redeclare the exchange".x(async () => {
            exception = await Record.ExceptionAsync(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition);
            });
        });
        "Then no exception is thrown".x(() => {
            exception.Should().BeNull();
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void RedeclareExchangeWithDifferentParameters(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition, Exception? exception) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has an exchange declared".x(async () => {
            exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
            await broker.ExchangeDeclareAsync(exchangeDefinition);
        });
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client attempts to redeclare the exchange with a different parameter".x(async () => {
            exception = await Record.ExceptionAsync(async () => {
                await channel.DeclareExchangeAsync(exchangeDefinition with {
                    Durability = Durability.Transient
                });
            });
        });
        "Then a precondition failed exception is thrown".x(() => {
            exception.Should().NotBeNull();
            exception.Should().BeOfType(Type.GetType("Lapine.Client.PreconditionFailedException, Lapine.Core"));
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeclareExchangeWithReservedPrefix(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, Exception? exception) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client attempts to declare an exchange with a reserved prefix".x(async () => {
            exception = await Record.ExceptionAsync(async () => {
                await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("amq.notallowed"));
            });
        });
        "Then an access refused exception is thrown".x(() => {
            exception.Should().NotBeNull();
            exception.Should().BeOfType(Type.GetType("Lapine.Client.AccessRefusedException, Lapine.Core"));
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeleteExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, ExchangeDefinition exchangeDefinition) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And the broker has an exchange declared".x(async () => {
            exchangeDefinition = ExchangeDefinition.Direct(Lorem.Word());
            await broker.ExchangeDeclareAsync(exchangeDefinition);
        });
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client deletes the exchange".x(async () => {
            await channel.DeleteExchangeAsync(exchangeDefinition.Name);
        });
        "Then the exchange should no longer exist on the broker".x(async () => {
            var exchanges = await broker.GetExchangesAsync().ToListAsync();
            exchanges.Should().NotContain(exchangeDefinition);
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeleteNonExistentExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, Exception? error) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion, enableManagement: true);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client deletes a non-existent exchange".x(async () => {
            error = await Record.ExceptionAsync(async () => {
                await channel.DeleteExchangeAsync("not.exist");
            });
        });
        "Then no exception is thrown".x(() => {
            error.Should().BeNull();
        });
    }

    [Scenario]
    [Example("3.12")]
    [Example("3.11")]
    [Example("3.10")]
    [Example("3.9")]
    [Example("3.8")]
    [Example("3.7")]
    public void DeleteDefaultExchange(String brokerVersion, BrokerProxy broker, AmqpClient subject, Channel channel, Exception? exception) {
        $"Given a running RabbitMQ v{brokerVersion} broker".x(async () => {
            broker = await BrokerProxy.StartAsync(brokerVersion);
        }).Teardown(async () => await broker.DisposeAsync());
        "And a client connected to the broker with an open channel".x(async () => {
            subject = new AmqpClient(await broker.GetConnectionConfigurationAsync());
            await subject.ConnectAsync();
            channel = await subject.OpenChannelAsync();
        }).Teardown(async () => await subject.DisposeAsync());
        "When the client deletes the default exchange".x(async () => {
            exception = await Record.ExceptionAsync(async () =>
                await channel.DeleteExchangeAsync("amq.default")
            );
        });
        "Then an access refused exception is thrown".x(() => {
            exception.Should().NotBeNull();
            exception.Should().BeOfType(Type.GetType("Lapine.Client.AccessRefusedException, Lapine.Core"));
        });
    }
}
