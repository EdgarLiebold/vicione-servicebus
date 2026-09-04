using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqErrorTransportTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0405", "nested-contract-type-mismatch-publishes-one-complete-receive-fault")]
    public async Task NestedContractTypeMismatch_MovesOneCompleteFault(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "nested-type-mismatch");
        string queueName = fixture.Name("input");
        Guid messageId = Guid.NewGuid();
        var consumed = NewObservation<ConsumeContext<MalformedMessage>>();
        var faulted = NewObservation<ConsumeContext<ReceiveFault>>();
        var transportObserver = new FaultTransportObserver();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<MalformedMessage>(context =>
                {
                    consumed.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        using ConnectHandle publishObserver = bus.ConnectPublishObserver(transportObserver);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HostReceiveEndpointHandle? faultEndpoint = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            faultEndpoint = bus.ConnectReceiveEndpoint(endpoint => endpoint.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            }));
            await faultEndpoint.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send<MalformedMessage>(
                    new { Count = 1 },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.Serializer = new CopyBodySerializer(
                            SystemTextJsonMessageSerializer.JsonContentType,
                            new StringMessageBody(CreateMalformedEnvelope(messageId)));
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<ReceiveFault> actual = await faulted.Task.WaitAsync(
                fixture.OperationTimeout,
                cancellationToken);
            ReceiveFault fault = actual.Message;
            ExceptionInfo exception = Assert.Single(fault.Exceptions);

            await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            faultEndpoint = null;
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(messageId, fault.FaultedMessageId);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType.MediaType, fault.ContentType);
            Assert.NotEqual(Guid.Empty, fault.FaultId);
            Assert.NotNull(fault.Host);
            Assert.Equal(TypeCache<JsonException>.ShortName, exception.ExceptionType);
            Assert.Contains("System.Int32", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, transportObserver.ReceiveFaultPublishCount);
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            if (faultEndpoint is not null)
                await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-RECEIVE-FAULT", "unreadable-envelope-preserves-transport-message-id")]
    public async Task UnreadableEnvelope_PreservesTransportMessageIdInReceiveFault(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "invalid-envelope");
        string queueName = fixture.Name("input");
        Guid messageId = Guid.NewGuid();
        var consumed = NewObservation<ConsumeContext<UnreadableMessage>>();
        var faulted = NewObservation<ConsumeContext<ReceiveFault>>();
        var transportObserver = new FaultTransportObserver();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<UnreadableMessage>(context =>
                {
                    consumed.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        using ConnectHandle publishObserver = bus.ConnectPublishObserver(transportObserver);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HostReceiveEndpointHandle? faultEndpoint = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            faultEndpoint = bus.ConnectReceiveEndpoint(endpoint => endpoint.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            }));
            await faultEndpoint.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send<UnreadableMessage>(
                    new { Value = "must-not-dispatch" },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.Serializer = new CopyBodySerializer(
                            SystemTextJsonMessageSerializer.JsonContentType,
                            new StringMessageBody("<not-an-envelope/>"));
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<ReceiveFault> actual = await faulted.Task.WaitAsync(
                fixture.OperationTimeout,
                cancellationToken);
            ReceiveFault fault = actual.Message;
            ExceptionInfo exception = Assert.Single(fault.Exceptions);

            await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            faultEndpoint = null;
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(messageId, fault.FaultedMessageId);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType.MediaType, fault.ContentType);
            Assert.NotEqual(Guid.Empty, fault.FaultId);
            Assert.NotNull(fault.Host);
            Assert.Equal(TypeCache<SerializationException>.ShortName, exception.ExceptionType);
            Assert.Contains("deserializing the message envelope", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, transportObserver.ReceiveFaultPublishCount);
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            if (faultEndpoint is not null)
                await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0413", "serialization-fault-moves-one-complete-envelope")]
    public async Task SerializationFault_MovesOneCompleteSanitizedEnvelope(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "error-transport");
        string queueName = fixture.Name("input");
        string errorQueueName = $"{queueName}_error";
        Guid correlationId = Guid.NewGuid();
        var moved = NewObservation<ErrorObservation>();
        Uri? receivedInputAddress = null;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<FaultingMessage>(context =>
                {
                    receivedInputAddress = context.ReceiveContext.InputAddress;
                    throw new SerializationException(IntentionalFailureMessage);
                });
            });
            configurator.ReceiveEndpoint(errorQueueName, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<FaultingMessage>(context =>
                {
                    moved.TrySetResult(new ErrorObservation(
                        context.ReceiveContext.InputAddress,
                        context.CorrelationId,
                        context.SourceAddress,
                        context.DestinationAddress,
                        context.ResponseAddress,
                        context.FaultAddress,
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultMessage, (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.Reason, (string?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultInputAddress, (Uri?)null),
                        context.ReceiveContext.TransportHeaders.Get(MessageHeaders.Host.MachineName, (string?)null)));
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Uri? inputAddress = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(
                    new FaultingMessage(correlationId),
                    context =>
                    {
                        context.CorrelationId = correlationId;
                        inputAddress = context.DestinationAddress;
                        context.ResponseAddress = bus.Address;
                        context.FaultAddress = bus.Address;
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ErrorObservation actual = await moved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Uri expectedInputAddress = Assert.IsType<Uri>(inputAddress);
            Uri expectedFaultInputAddress = Assert.IsType<Uri>(receivedInputAddress);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            ActiveMqBroker.ClassicQueueStatistics errorQueue = await fixture.GetClassicQueueStatistics(
                errorQueueName,
                cancellationToken);

            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(errorQueueName, actual.ErrorQueueAddress.AbsolutePath.Trim('/'));
            Assert.Equal(bus.Address, actual.SourceAddress);
            Assert.Equal(expectedInputAddress, actual.DestinationAddress);
            Assert.Equal(bus.Address, actual.ResponseAddress);
            Assert.Equal(bus.Address, actual.FaultAddress);
            Assert.Equal(IntentionalFailureMessage, actual.FaultMessage);
            Assert.Equal("fault", actual.Reason);
            Assert.Equal(expectedFaultInputAddress, actual.FaultInputAddress);
            Assert.Equal(HostMetadataCache.Host.MachineName, actual.HostMachineName);
            Assert.Equal(1, errorQueue.EnqueueCount);
            Assert.Equal(1, errorQueue.DequeueCount);
            Assert.Equal(0, errorQueue.QueueSize);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0425", "raw-invalid-nms-message-publishes-one-receive-fault")]
    public async Task RawInvalidNmsMessage_MovesOneCompleteFault(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "raw-invalid");
        string queueName = fixture.Name("input");
        const string correlationId = "AB76E632-8550-49B9-A119-BBEB84D53355";
        var consumed = NewObservation<ConsumeContext<RawMessage>>();
        var faulted = NewObservation<ConsumeContext<ReceiveFault>>();
        var transportObserver = new FaultTransportObserver();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                // A raw producer uses a separate NMS connection and therefore cannot see the
                // product connection's broker-generated TemporaryQueue registration. A durable,
                // run-scoped logical queue is the actual cross-connection provider boundary.
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<RawMessage>(context =>
                {
                    consumed.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        using ConnectHandle publishObserver = bus.ConnectPublishObserver(transportObserver);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HostReceiveEndpointHandle? faultEndpoint = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            faultEndpoint = bus.ConnectReceiveEndpoint(endpoint => endpoint.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            }));
            await faultEndpoint.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            using IConnection connection = fixture.CreateConnection();
            using ISession session = connection.CreateSession(AcknowledgementMode.ClientAcknowledge);
            using IMessageProducer producer = session.CreateProducer(session.GetQueue(queueName));
            IMessage raw = session.CreateMessage();
            raw.NMSCorrelationID = correlationId;
            await producer.SendAsync(raw).WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<ReceiveFault> actual = await faulted.Task.WaitAsync(
                fixture.OperationTimeout,
                cancellationToken);
            ReceiveFault fault = actual.Message;
            ExceptionInfo exception = Assert.Single(fault.Exceptions);

            await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            faultEndpoint = null;
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.NotEqual(Guid.Empty, fault.FaultId);
            Assert.NotNull(fault.Host);
            Assert.Equal(TypeCache<SerializationException>.ShortName, exception.ExceptionType);
            Assert.Contains("deserializing the message envelope", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, transportObserver.ReceiveFaultPublishCount);
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            if (faultEndpoint is not null)
                await faultEndpoint.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private const string IntentionalFailureMessage = "Intentional ActiveMQ serialization failure.";

    private static string CreateMalformedEnvelope(Guid messageId) => $$"""
        {
          "messageId": "{{messageId:D}}",
          "messageType": [
            "{{MessageUrn.ForTypeString<MalformedMessage>()}}"
          ],
          "message": {
            "count": false
          }
        }
        """;

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface UnreadableMessage
    {
        string Value { get; }
    }

    public interface MalformedMessage
    {
        int Count { get; }
    }

    private sealed record FaultingMessage(Guid CorrelationId);
    private sealed record RawMessage(string Value);

    private sealed record ErrorObservation(
        Uri ErrorQueueAddress,
        Guid? CorrelationId,
        Uri? SourceAddress,
        Uri? DestinationAddress,
        Uri? ResponseAddress,
        Uri? FaultAddress,
        string? FaultMessage,
        string? Reason,
        Uri? FaultInputAddress,
        string? HostMachineName);

    private sealed class FaultTransportObserver : IPublishObserver
    {
        private int _receiveFaultPublishCount;

        public int ReceiveFaultPublishCount => Volatile.Read(ref _receiveFaultPublishCount);

        public Task PrePublish<T>(PublishContext<T> context)
            where T : class
        {
            if (context.Message is ReceiveFault)
                Interlocked.Increment(ref _receiveFaultPublishCount);
            return Task.CompletedTask;
        }

        public Task PostPublish<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFault<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }
}
