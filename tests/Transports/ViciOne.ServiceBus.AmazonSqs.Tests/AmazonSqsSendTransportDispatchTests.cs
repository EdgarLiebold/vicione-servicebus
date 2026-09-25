using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using Topic = ViciOne.ServiceBus.AmazonSqs.Topology.Topic;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSendTransportDispatchTests
{
    [Theory]
    [InlineData("queue:orders", "orders", false)]
    [InlineData("topic:events", "events", true)]
    [InlineData("amazonsqs://eu-central-1/events?type=topic", "events", true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-TOPOLOGY", "send-transport-address-selects-provider-dispatch")]
    public async Task CreatedTransport_DispatchesToTheAddressSelectedProviderAsync(
        string destination, string entityName, bool topic)
    {
        using ServiceProvider provider = CreateHost(out var bus, out var topology);
        await using var client = new RecordingClientContext();
        IClientContextSupervisor parent = InterfaceProxy<IClientContextSupervisor>.Create((method, args) => method.Name switch
        {
            "get_Ready" or "get_Completed" => Task.CompletedTask,
            "get_SendStopping" => CancellationToken.None,
            nameof(IClientContextSupervisor.AddSendAgent) => null,
            nameof(IClientContextSupervisor.SendAsync) => ((IPipe<ClientContext>)args![0]!).SendAsync(client),
            _ => throw new NotSupportedException(method.Name)
        });
        SqsReceiveEndpointContext endpoint = InterfaceProxy<SqsReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => bus.Serialization.CreateSerializerCollection(),
            _ => throw new NotSupportedException(method.Name)
        });
        var supervisor = new ConnectionContextSupervisor(bus.HostConfiguration, topology);
        await using var transport = Assert.IsType<SendTransport<ClientContext>>(
            await supervisor.CreateSendTransportAsync(
                endpoint, parent, new Uri(destination), TestContext.Current.CancellationToken));

        await transport.SendAsync(new Message("contract-body"),
            new SerializedSendPipe(bus.Serialization.CreateSerializerCollection(), new Uri(destination)),
            TestContext.Current.CancellationToken);

        Assert.Equal(topic ? 1 : 0, client.Publishes.Count);
        Assert.Equal(topic ? 0 : 1, client.Sends.Count);
        if (topic)
        {
            var published = Assert.Single(client.Publishes);
            Assert.Equal(entityName, published.Name);
            Assert.Contains("contract-body", published.Request.Message, StringComparison.Ordinal);
            Assert.Equal([entityName], client.DeclaredTopics);
            Assert.Empty(client.DeclaredQueues);
        }
        else
        {
            var sent = Assert.Single(client.Sends);
            Assert.Equal(entityName, sent.Name);
            Assert.Contains("contract-body", sent.Request.MessageBody, StringComparison.Ordinal);
            Assert.Equal([entityName], client.DeclaredQueues);
            Assert.Empty(client.DeclaredTopics);
        }

    }

    private static ServiceProvider CreateHost(out AmazonSqsBusConfiguration bus, out IAmazonSqsTopologyConfiguration topology)
    {
        topology = new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology());
        bus = new AmazonSqsBusConfiguration(topology);
        var settings = new AmazonSqsHostSettings(
            RegionEndpoint.EUCentral1, null, false, new Uri("amazonsqs://eu-central-1/"),
            new AmazonSqsClientContextCacheOptions(),
            () => throw new InvalidOperationException("No AWS connection is expected."), null);
        var services = new ServiceCollection();
        AmazonSqsBusConfiguration configuration = bus;
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetBusFactory(new AdmissionBusFactory(configuration, settings));
        });
        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IBusControl>();
        return provider;
    }

    private sealed class SerializedSendPipe(ISerialization serialization, Uri destination) : IPipe<SendContext<Message>>
    {
        public Task SendAsync(SendContext<Message> context)
        {
            context.Serialization = serialization;
            context.Serializer = serialization.GetMessageSerializer();
            context.SourceAddress = new Uri("amazonsqs://eu-central-1/source");
            context.DestinationAddress = destination;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) { }
    }

    private sealed class RecordingClientContext : BasePipeContext, ClientContext, IAsyncDisposable
    {
        private readonly IAmazonSQS _sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        private readonly IAmazonSimpleNotificationService _sns =
            InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) => throw new NotSupportedException(method.Name));
        private readonly List<IAsyncDisposable> _resolved = [];

        public List<string> DeclaredQueues { get; } = [];
        public List<string> DeclaredTopics { get; } = [];
        public List<(string Name, SendMessageBatchRequestEntry Request)> Sends { get; } = [];
        public List<(string Name, PublishBatchRequestEntry Request)> Publishes { get; } = [];
        public ConnectionContext ConnectionContext => throw new NotSupportedException();

        public Task<QueueInfo> CreateQueueAsync(ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            DeclaredQueues.Add(queue.EntityName);
            var info = new QueueInfo(queue.EntityName, $"https://sqs.example/{queue.EntityName}",
                new Dictionary<string, string> { [QueueAttributeName.QueueArn] = $"arn:aws:sqs:eu-central-1:123456789012:{queue.EntityName}" },
                _sqs, token, true);
            _resolved.Add(info);
            return Task.FromResult(info);
        }

        public Task<TopicInfo> CreateTopicAsync(Topic topic, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            DeclaredTopics.Add(topic.EntityName);
            var info = new TopicInfo(topic.EntityName,
                $"arn:aws:sns:eu-central-1:123456789012:{topic.EntityName}", _sns, token, true);
            _resolved.Add(info);
            return Task.FromResult(info);
        }

        public Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Sends.Add((queueName, request));
            return Task.CompletedTask;
        }

        public Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Publishes.Add((topicName, request));
            return Task.CompletedTask;
        }

        public Task<bool> CreateQueueSubscriptionAsync(Topic topic, ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue,
            CancellationToken token) => throw new NotSupportedException();
        public Task DeleteTopicAsync(Topic topic, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteQueueAsync(ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue, CancellationToken token) => throw new NotSupportedException();
        public Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken token) => throw new NotSupportedException();
        public Task PurgeQueueAsync(string queueName, CancellationToken token) => throw new NotSupportedException();
        public Task<IList<Amazon.SQS.Model.Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime,
            CancellationToken token) => throw new NotSupportedException();
        public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken token) => throw new NotSupportedException();
        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken token) =>
            throw new NotSupportedException();

        public async ValueTask DisposeAsync()
        {
            foreach (IAsyncDisposable resource in _resolved)
                await resource.DisposeAsync();
        }
    }

    private sealed class AdmissionBusFactory(AmazonSqsBusConfiguration bus, AmazonSqsHostSettings settings)
        : TransportRegistrationBusFactory<IAmazonSqsReceiveEndpointConfigurator>(bus.HostConfiguration)
    {
        public override IBusInstance CreateBus(IBusRegistrationContext context,
            IEnumerable<IBusInstanceSpecification> specifications, string busName)
        {
            var configurator = new AmazonSqsBusFactoryConfigurator(bus);
            configurator.Host(settings);
            return CreateBus<AmazonSqsBusFactoryConfigurator, IAmazonSqsBusFactoryConfigurator>(
                configurator, context, null, specifications);
        }
    }

    private sealed record Message(string Text);
}
