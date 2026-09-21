using Amazon;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSendTransportContextTests
{
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "send-links-caller-lifetime-through-topology")]
    public async Task Send_LinksCallerCancellationThroughTopologyAndProviderOperationsAsync(bool topic)
    {
        using var transportLifetime = new CancellationTokenSource();
        using var callerLifetime = new CancellationTokenSource();
        using ServiceProvider provider = CreateAdmittedHost(out AmazonSqsBusConfiguration busConfiguration);
        var topologyPipe = new CancellationBlockingPipe();
        ClientContext clientContext = CreateClientContext(transportLifetime.Token, null);
        var transport = new SendTransport<ClientContext>(CreateSendTransport(busConfiguration, topic, topologyPipe, clientContext));

        Task send = transport.SendAsync(
            new Message(), new ConfiguredSendPipe(busConfiguration.Serialization.CreateSerializerCollection()), callerLifetime.Token);

        try
        {
            Task first = await Task.WhenAny(send, topologyPipe.Entered)
                .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
            if (ReferenceEquals(first, send))
                await send;
            await topologyPipe.Entered;

            callerLifetime.Cancel();
            Task completed = await Task.WhenAny(send, Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));

            Assert.Same(send, completed);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
            Assert.True(topologyPipe.ObservedToken.IsCancellationRequested);
        }
        finally
        {
            callerLifetime.Cancel();
            transportLifetime.Cancel();
            await IgnoreCancellationAsync(send).WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-TELEMETRY", "topic-identifies-aws-sns")]
    public void TopicTransport_ReportsTheCanonicalAmazonSnsMessagingSystem()
    {
        using ServiceProvider provider = CreateAdmittedHost(out AmazonSqsBusConfiguration busConfiguration);
        var transport = Assert.IsType<TopicSendTransportContext>(CreateSendTransport(
            busConfiguration, true, new CompletedPipe<ClientContext>(), CreateClientContext(CancellationToken.None, null)));

        Assert.Equal("aws.sns", transport.ActivitySystem);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-HEADERS", "correlation-id-uses-canonical-header")]
    public async Task TopicSend_UsesTheCanonicalCorrelationIdHeaderAsync()
    {
        PublishBatchRequestEntry? published = null;
        using ServiceProvider provider = CreateAdmittedHost(out AmazonSqsBusConfiguration busConfiguration);
        ClientContext clientContext = CreateClientContext(CancellationToken.None, request => published = request);
        var transport = new SendTransport<ClientContext>(
            CreateSendTransport(busConfiguration, true, new CompletedPipe<ClientContext>(), clientContext));
        var correlationId = Guid.NewGuid();

        await transport.SendAsync(
                new Message(),
                new ConfiguredSendPipe(busConfiguration.Serialization.CreateSerializerCollection(), correlationId),
                TestContext.Current.CancellationToken)
            .WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.NotNull(published);
        MessageAttributeValue attribute = Assert.Contains(MessageHeaders.CorrelationId, published.MessageAttributes);
        Assert.Equal(correlationId.ToString(), attribute.StringValue);
        if (!string.Equals(MessageHeaders.CorrelationId, nameof(SendContext<Message>.CorrelationId), StringComparison.Ordinal))
            Assert.DoesNotContain(nameof(SendContext<Message>.CorrelationId), published.MessageAttributes);
    }

    private static ServiceProvider CreateAdmittedHost(out AmazonSqsBusConfiguration busConfiguration)
    {
        busConfiguration = new AmazonSqsBusConfiguration(
            new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
        var settings = new AmazonSqsHostSettings(
            RegionEndpoint.EUCentral1,
            null,
            false,
            new Uri("amazonsqs://eu-central-1/"),
            new AmazonSqsClientContextCacheOptions(),
            () => throw new InvalidOperationException("The connection factory must not run in this unit test."),
            null);
        var services = new ServiceCollection();
        AmazonSqsBusConfiguration configuration = busConfiguration;
        services.AddViciOneServiceBus(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            registration.SetBusFactory(new AdmissionBusFactory(configuration, settings));
        });
        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IBusControl>();
        return provider;
    }

    private static SendTransportContext<ClientContext> CreateSendTransport(
        AmazonSqsBusConfiguration busConfiguration, bool topic, IPipe<ClientContext> topologyPipe, ClientContext clientContext)
    {
        ReceiveEndpointContext receiveEndpointContext = InterfaceProxy<ReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => busConfiguration.Serialization.CreateSerializerCollection(),
            _ => Default(method.ReturnType)
        });
        IClientContextSupervisor supervisor = InterfaceProxy<IClientContextSupervisor>.Create((method, args) => method.Name switch
        {
            "get_Ready" => Task.CompletedTask,
            "get_Completed" => Task.CompletedTask,
            "get_SendStopping" => CancellationToken.None,
            nameof(IClientContextSupervisor.SendAsync) => ((IPipe<ClientContext>)args![0]!).SendAsync(clientContext),
            _ => Default(method.ReturnType)
        });

        return topic
            ? new TopicSendTransportContext(busConfiguration.HostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders")
            : new QueueSendTransportContext(busConfiguration.HostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders");
    }

    private static ClientContext CreateClientContext(CancellationToken cancellationToken, Action<PublishBatchRequestEntry>? publish)
    {
        return InterfaceProxy<ClientContext>.Create((method, args) => method.Name switch
        {
            "get_CancellationToken" => cancellationToken,
            nameof(ClientContext.PublishAsync) => RecordPublishAsync(args, publish),
            _ => Default(method.ReturnType)
        });
    }

    private static Task RecordPublishAsync(object?[]? arguments, Action<PublishBatchRequestEntry>? publish)
    {
        publish?.Invoke(Assert.IsType<PublishBatchRequestEntry>(arguments![1]));
        return Task.CompletedTask;
    }

    private static object? Default(Type returnType) => returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class CancellationBlockingPipe : IPipe<ClientContext>
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public CancellationToken ObservedToken { get; private set; }

        public async Task SendAsync(ClientContext context)
        {
            ObservedToken = context.CancellationToken;
            _entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CompletedPipe<TContext> : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => Task.CompletedTask;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class ConfiguredSendPipe(ISerialization serialization, Guid? correlationId = null) : IPipe<SendContext<Message>>
    {
        public Task SendAsync(SendContext<Message> context)
        {
            context.Serialization = serialization;
            context.Serializer = serialization.GetMessageSerializer();
            context.SourceAddress = new Uri("amazonsqs://eu-central-1/source");
            context.DestinationAddress = new Uri("amazonsqs://eu-central-1/orders");
            context.CorrelationId = correlationId;
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class AdmissionBusFactory(
        AmazonSqsBusConfiguration busConfiguration,
        AmazonSqsHostSettings settings)
        : TransportRegistrationBusFactory<IAmazonSqsReceiveEndpointConfigurator>(busConfiguration.HostConfiguration)
    {
        public override IBusInstance CreateBus(
            IBusRegistrationContext context,
            IEnumerable<IBusInstanceSpecification> specifications,
            string busName)
        {
            var configurator = new AmazonSqsBusFactoryConfigurator(busConfiguration);
            configurator.Host(settings);
            return CreateBus<AmazonSqsBusFactoryConfigurator, IAmazonSqsBusFactoryConfigurator>(
                configurator, context, null, specifications);
        }
    }

    private sealed record Message;
}
