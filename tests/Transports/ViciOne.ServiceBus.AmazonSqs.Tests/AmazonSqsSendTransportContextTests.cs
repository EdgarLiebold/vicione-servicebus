using System.Net.Mime;
using Amazon;
using Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsSendTransportContextTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "send-links-caller-lifetime-through-topology")]
    public async Task Send_LinksCallerCancellationThroughTopologyAndProviderOperationsAsync(bool topic)
    {
        using var transportLifetime = new CancellationTokenSource();
        using var callerLifetime = new CancellationTokenSource();
        var topologyPipe = new CancellationBlockingPipe();
        SendTransportContext<ClientContext> transport = CreateSendTransport(topic, topologyPipe);
        ClientContext clientContext = CreateClientContext(transportLifetime.Token, null);
        var sendContext = new AmazonSqsMessageSendContext<Message>(new Message(), CancellationToken.None);

        Task send = transport.SendAsync(clientContext, sendContext, callerLifetime.Token);
        await topologyPipe.Entered.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            callerLifetime.Cancel();
            Task completed = await Task.WhenAny(send, Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));

            Assert.Same(send, completed);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
            Assert.True(topologyPipe.ObservedToken.IsCancellationRequested);
        }
        finally
        {
            transportLifetime.Cancel();
            await IgnoreCancellationAsync(send);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-TELEMETRY", "topic-identifies-aws-sns")]
    public void TopicTransport_ReportsTheCanonicalAmazonSnsMessagingSystem()
    {
        var transport = Assert.IsType<TopicSendTransportContext>(CreateSendTransport(true, new CompletedPipe<ClientContext>()));

        Assert.Equal("aws.sns", transport.ActivitySystem);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-HEADERS", "correlation-id-uses-canonical-header")]
    public async Task TopicSend_UsesTheCanonicalCorrelationIdHeaderAsync()
    {
        PublishBatchRequestEntry? published = null;
        ClientContext clientContext = CreateClientContext(CancellationToken.None, request => published = request);
        var transport = Assert.IsType<TopicSendTransportContext>(CreateSendTransport(true, new CompletedPipe<ClientContext>()));
        var correlationId = Guid.NewGuid();
        var sendContext = new AmazonSqsMessageSendContext<Message>(new Message(), CancellationToken.None)
        {
            CorrelationId = correlationId,
            Serializer = CreateSerializer()
        };

        await transport.SendAsync(clientContext, sendContext, TestContext.Current.CancellationToken);

        Assert.NotNull(published);
        MessageAttributeValue attribute = Assert.Contains(MessageHeaders.CorrelationId, published.MessageAttributes);
        Assert.Equal(correlationId.ToString(), attribute.StringValue);
        if (!string.Equals(MessageHeaders.CorrelationId, nameof(sendContext.CorrelationId), StringComparison.Ordinal))
            Assert.DoesNotContain(nameof(sendContext.CorrelationId), published.MessageAttributes);
    }

    private static SendTransportContext<ClientContext> CreateSendTransport(bool topic, IPipe<ClientContext> topologyPipe)
    {
        var settings = new AmazonSqsHostSettings(
            RegionEndpoint.EUCentral1,
            null,
            false,
            new Uri("amazonsqs://eu-central-1/"),
            new AmazonSqsClientContextCacheOptions(),
            () => throw new InvalidOperationException("The connection factory must not run in this unit test."),
            null);
        IAmazonSqsHostConfiguration hostConfiguration = InterfaceProxy<IAmazonSqsHostConfiguration>.Create((method, _) => method.Name switch
        {
            "get_Settings" => settings,
            _ => Default(method.ReturnType)
        });
        ISerialization serialization = InterfaceProxy<ISerialization>.Create((method, _) => Default(method.ReturnType));
        ReceiveEndpointContext receiveEndpointContext = InterfaceProxy<ReceiveEndpointContext>.Create((method, _) => method.Name switch
        {
            "get_Serialization" => serialization,
            _ => Default(method.ReturnType)
        });
        IClientContextSupervisor supervisor = InterfaceProxy<IClientContextSupervisor>.Create((method, _) => Default(method.ReturnType));

        return topic
            ? new TopicSendTransportContext(hostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders")
            : new QueueSendTransportContext(hostConfiguration, receiveEndpointContext, supervisor, topologyPipe, "orders");
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

    private static IMessageSerializer CreateSerializer()
    {
        return InterfaceProxy<IMessageSerializer>.Create((method, _) => method.Name switch
        {
            "get_ContentType" => new ContentType("application/json"),
            nameof(IMessageSerializer.GetMessageBody) => new StringMessageBody("{}"),
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

    private sealed record Message;
}
