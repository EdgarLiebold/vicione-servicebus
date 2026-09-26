using System.Collections.Concurrent;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqTemporaryReplyTests
{
    readonly ITestOutputHelper _output;

    public ActiveMqTemporaryReplyTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0455", "envelope-request-uses-provider-temporary-reply-queue")]
    public Task EnvelopeRequest_UsesProviderTemporaryReplyQueueAsync(string flavor) =>
        AssertTemporaryReplyQueueAsync(flavor, rawSerializer: false);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0456", "raw-request-uses-provider-temporary-reply-queue")]
    public Task RawRequest_UsesProviderTemporaryReplyQueueAsync(string flavor) =>
        AssertTemporaryReplyQueueAsync(flavor, rawSerializer: true);

    private async Task AssertTemporaryReplyQueueAsync(string flavor, bool rawSerializer)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(
            flavor,
            rawSerializer ? "reply-raw" : "reply-envelope");
        string queueName = fixture.Name("service");
        Guid correlationId = Guid.NewGuid();
        var replyObserved = NewObservation<ReplyObservation>();
        var handlerEntered = NewObservation<bool>();
        var responseSent = NewObservation<bool>();
        var handlerFailure = NewObservation<Exception>();
        var transportObservations = new TransportObservations();
        var responseCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            if (rawSerializer)
                configurator.UseRawJsonSerializer();

            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<ReplyRequest>(async context =>
            {
                handlerEntered.TrySetResult(true);
                Interlocked.Increment(ref responseCount);
                try
                {
                    ActiveMqReceiveContext transport = context.Advanced().ReceiveContext.GetPayload<ActiveMqReceiveContext>();
                    IDestination replyTo = Assert.IsAssignableFrom<IDestination>(transport.TransportMessage.NMSReplyTo);
                    replyObserved.TrySetResult(new ReplyObservation(
                        replyTo.IsTemporary,
                        replyTo.IsQueue,
                        replyTo.IsTopic,
                        ToEndpointAddress(replyTo),
                        context.Message.CorrelationId));
                    await context.RespondAsync(new ReplyResponse(context.Message.CorrelationId));
                    responseSent.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    handlerFailure.TrySetResult(exception);
                    throw;
                }
            }));
        });
        using ConnectHandle sendObservation = bus.ConnectSendObserver(transportObservations);
        using ConnectHandle receiveObservation = bus.ConnectReceiveObserver(transportObservations);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bool started = false;
        Exception? primaryFailure = null;
        Task<Response<ReplyResponse>>? responseTask = null;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<ReplyRequest> client = bus.CreateRequestClient<ReplyRequest>(
                new Uri($"queue:{queueName}"),
                new RequestTimeout(fixture.OperationTimeout));
            responseTask = client.GetResponseAsync<ReplyResponse>(
                    new ReplyRequest(correlationId),
                    requestCancellation.Token);
            TimeSpan watchdog = fixture.OperationTimeout + TimeSpan.FromSeconds(5);
            using var watchdogCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            watchdogCancellation.CancelAfter(watchdog);
            foreach (Task phase in new Task[] { responseTask, responseSent.Task })
            {
                Task first = await Task.WhenAny(phase, handlerFailure.Task).WaitAsync(watchdogCancellation.Token);
                if (handlerFailure.Task.IsCompletedSuccessfully)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(await handlerFailure.Task).Throw();
                await first;
            }
            Response<ReplyResponse> response = await responseTask;
            ReplyObservation observed = await replyObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(correlationId, observed.CorrelationId);
            Assert.True(observed.IsTemporary);
            Assert.True(observed.IsQueue);
            Assert.False(observed.IsTopic);
            Assert.False(string.IsNullOrWhiteSpace(observed.Address.AbsolutePath.Trim('/')));
            Assert.DoesNotContain(queueName, observed.Address.ToString(), StringComparison.Ordinal);
            Assert.True(string.IsNullOrEmpty(observed.Address.UserInfo));
            Assert.Equal(1, Volatile.Read(ref responseCount));
            SentObservation requestSent = await transportObservations.RequestSent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            SentObservation replySent = await transportObservations.ReplySent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.NotNull(requestSent.RequestId);
            Assert.Equal(requestSent.RequestId, replySent.RequestId);
            Assert.Equal(requestSent.RequestId, response.RequestId);
            Assert.Equal(observed.Address, replySent.NativeDestination);
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            _output.WriteLine($"Flavor={flavor}; Queue={queueName}; CorrelationId={correlationId}; "
                + $"HandlerEntered={handlerEntered.Task.IsCompleted}; ReplyObserved={replyObserved.Task.Status}; "
                + $"ResponseSent={responseSent.Task.IsCompleted}; ClientResponse={responseTask?.Status}; "
                + $"HandlerCalls={Volatile.Read(ref responseCount)}");
            if (replyObserved.Task.IsCompletedSuccessfully)
                _output.WriteLine($"ReplyTo={replyObserved.Task.Result}");
            if (handlerFailure.Task.IsCompletedSuccessfully)
                _output.WriteLine($"HandlerFailure={handlerFailure.Task.Result}");
            foreach (string observation in transportObservations.Events)
                _output.WriteLine(observation);
            using var diagnosticsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                var queue = await fixture.GetClassicQueueStatisticsAsync(queueName, diagnosticsTimeout.Token);
                _output.WriteLine($"QueueStatistics={queue}");
            }
            catch (Exception diagnosticsFailure)
            {
                _output.WriteLine($"QueueStatisticsFailure={diagnosticsFailure}");
            }
            throw;
        }
        finally
        {
            if (responseTask is not null)
                _ = responseTask.ContinueWith(task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            Exception? cleanupFailure = null;
            try
            {
                await requestCancellation.CancelAsync();
            }
            catch (Exception exception)
            {
                cleanupFailure = exception;
                _output.WriteLine($"RequestCancellationFailure={exception}");
            }
            if (started)
            {
                try
                {
                    await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    cleanupFailure = cleanupFailure is null ? exception : new AggregateException(cleanupFailure, exception);
                    _output.WriteLine($"BusStopFailure={exception}");
                }
            }
            if (primaryFailure is null && cleanupFailure is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Uri ToEndpointAddress(IDestination destination) => destination switch
    {
        IQueue queue => new Uri($"queue:{queue.QueueName}"),
        ITopic topic => new Uri($"topic:{topic.TopicName}"),
        _ => throw new InvalidDataException(
            $"Destination type '{destination.GetType().FullName}' is neither a queue nor a topic."),
    };

    public sealed record ReplyRequest(Guid CorrelationId);
    public sealed record ReplyResponse(Guid CorrelationId);

    private sealed record ReplyObservation(
        bool IsTemporary,
        bool IsQueue,
        bool IsTopic,
        Uri Address,
        Guid CorrelationId);

    private sealed record SentObservation(Guid? RequestId, Uri? NativeDestination);

    private sealed class TransportObservations : ISendObserver, IReceiveObserver
    {
        readonly ConcurrentQueue<string> _events = new();
        int _eventCount;

        public TaskCompletionSource<SentObservation> RequestSent { get; } = NewObservation<SentObservation>();
        public TaskCompletionSource<SentObservation> ReplySent { get; } = NewObservation<SentObservation>();
        public IEnumerable<string> Events => _events.ToArray();

        void Record(string value)
        {
            if (Interlocked.Increment(ref _eventCount) <= 32)
                _events.Enqueue(value);
        }

        public Task PreSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            Uri? nativeDestination = context.TryGetPayload(out ActiveMqSendContext? transport)
                && transport.ReplyDestination is { } destination ? ToEndpointAddress(destination) : null;
            var observation = new SentObservation(context.RequestId, nativeDestination);
            Record($"Sent Type={typeof(T).Name}; RequestId={context.RequestId}; ContentType={context.ContentType}; "
                + $"Destination={context.DestinationAddress}; NativeDestination={nativeDestination}");
            if (typeof(T) == typeof(ReplyRequest))
                RequestSent.TrySetResult(observation);
            if (typeof(T) == typeof(ReplyResponse))
                ReplySent.TrySetResult(observation);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class
        {
            Record($"SendFault Type={typeof(T).Name}; RequestId={context.RequestId}; Exception={exception}");
            return Task.CompletedTask;
        }

        public Task PreReceiveAsync(ReceiveContext context)
        {
            try
            {
                ActiveMqReceiveContext transport = context.GetPayload<ActiveMqReceiveContext>();
                IMessage message = transport.TransportMessage;
                context.TransportHeaders.TryGetHeader(MessageHeaders.RequestId, out object? requestId);
                Record($"Receiving Input={context.InputAddress}; NativeDestination={ToEndpointAddress(message.NMSDestination)}; "
                    + $"RequestIdHeader={requestId}; ContentType={context.ContentType}; MessageId={message.NMSMessageId}");
            }
            catch (Exception exception)
            {
                Record($"ReceiveObservationFailure={exception}");
            }
            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(ReceiveContext context)
        {
            Record($"Received Input={context.InputAddress}");
            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            Record($"Consumed Type={typeof(T).Name}; RequestId={context.RequestId}; Consumer={consumerType}");
            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            Record($"ConsumeFault Type={typeof(T).Name}; RequestId={context.RequestId}; Exception={exception}");
            return Task.CompletedTask;
        }

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception)
        {
            Record($"ReceiveFault Input={context.InputAddress}; Exception={exception}");
            return Task.CompletedTask;
        }
    }
}
