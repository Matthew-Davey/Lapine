using System.Reactive.Linq;
using Lapine.Agents;
using Lapine.Client;
using Lapine.Protocol;

using static System.Text.Encoding;

var connectionConfiguration = ConnectionConfiguration.Default with {
    PeerProperties    = PeerProperties.Default with {
        Product            = "Lapine.Workbench",
        ClientProvidedName = "Lapine.Workbench"
    }
};

var socket2 = new SocketAgent();
socket2.Events
    .OfType<SocketAgent.Protocol.FrameReceived>()
    .Subscribe(message => Console.WriteLine(message.Frame));

socket2.Connect(connectionConfiguration.Endpoints[0], connectionConfiguration.ConnectionTimeout);

await Task.Delay(5000);

//var amqpClient = new AmqpClient(connectionConfiguration);

// await amqpClient.ConnectAsync();
// var channel = await amqpClient.OpenChannelAsync();
// await channel.DeclareExchangeAsync(ExchangeDefinition.Direct("test.exchange"));
// await channel.DeclareQueueAsync(QueueDefinition.Create("test.queue"));
// await channel.BindQueueAsync(Binding.Create("test.exchange", "test.queue"));
//
// await channel.PublishAsync(
//     exchange    : "test.exchange",
//     routingKey  : "#",
//     message     : (MessageProperties.Empty, UTF8.GetBytes("Test Message 1")),
//     routingFlags: RoutingFlags.None
// );
//
// await channel.EnablePublisherConfirms();
//
// await channel.PublishAsync(
//     exchange    : "test.exchange",
//     routingKey  : "#",
//     message     : (MessageProperties.Empty, UTF8.GetBytes("Test Message 2")),
//     routingFlags: RoutingFlags.None
// );
//
// await channel.PublishAsync(
//     exchange    : "test.exchange",
//     routingKey  : "#",
//     message     : (MessageProperties.Empty, UTF8.GetBytes("Test Message 3")),
//     routingFlags: RoutingFlags.None
// );
//
// await Task.Delay(TimeSpan.FromMilliseconds(-1));
// await channel.CloseAsync();
// await amqpClient.DisposeAsync();
