using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorDispatchTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS-MEDIATOR", "missing-policy-fails-before-materialization")]
    public void MissingLimits_FailsBeforeDirectAndContainerMediatorMaterialization()
    {
        ConfigurationException direct = Assert.Throws<ConfigurationException>(() =>
            Bus.Factory.CreateMediator(_ => { }));
        using ServiceProvider provider = new ServiceCollection()
            .AddMediator(_ => { })
            .BuildServiceProvider();
        ConfigurationException container = Assert.Throws<ConfigurationException>(() =>
            provider.GetRequiredService<IMediator>());

        Assert.Equal(
            "Message limits for bus 'mediator': MaxBodyBytes is not declared. Call mediator.Limits(...) with explicit byte limits.",
            direct.Message);
        Assert.Equal(direct.Message, container.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "direct-configuration-invoked-once")]
    public async Task DirectMediatorConfiguration_IsInvokedExactlyOnceAsync()
    {
        var invocations = 0;

        await using IMediator mediator = Bus.Factory.CreateMediator(configuration =>
        {
            Interlocked.Increment(ref invocations);
            configuration.Limits(MessageLimits.Conservative);
        });

        Assert.Equal(1, Volatile.Read(ref invocations));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "custom-request-address-preserved")]
    public async Task CustomRequestEndpoint_PreservesItsLogicalDestinationAsync()
    {
        await using IMediator mediator = Bus.Factory.CreateMediator(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.Handler<AddressedRequest>(context =>
                context.RespondAsync(new AddressedResponse(context.DestinationAddress)));
        });
        var logicalAddress = new Uri("loopback://localhost/logical-request-target");
        IRequestClient<AddressedRequest> client = mediator.CreateRequestClient<AddressedRequest>(logicalAddress);

        Response<AddressedResponse> response = await client.GetResponseAsync<AddressedResponse>(
            new AddressedRequest(NewId.NextGuid()),
            TestContext.Current.CancellationToken).WaitAsync(
            OperationTimeout(),
            TestContext.Current.CancellationToken);

        Assert.Equal(logicalAddress, response.Message.DestinationAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS-MEDIATOR", "serialized-body-rejected-before-handler")]
    public async Task OversizedSerializedBody_IsRejectedBeforeMediatorDispatchAsync()
    {
        var handled = 0;
        IMediator mediator = Bus.Factory.CreateMediator(configuration =>
        {
            configuration.Limits(new MessageLimits { MaxBodyBytes = 64, MaxEnvelopeBytes = 64, MaxJsonDepth = 32 });
            configuration.Handler<DispatchMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        MessageTooLargeException failure = await Assert.ThrowsAsync<MessageTooLargeException>(() =>
            mediator.SendAsync(new DispatchMessage(new string('x', 256)), TestContext.Current.CancellationToken));

        Assert.True(failure.ActualBytes > failure.MaximumBytes);
        Assert.Equal(64, failure.MaximumBytes);
        Assert.Equal(new Uri("loopback://localhost/mediator"), failure.InputAddress);
        Assert.Equal(0, Volatile.Read(ref handled));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-LIMITS-MEDIATOR", "json-depth-exact-boundary")]
    public async Task JsonDepth_AllowsTheConfiguredDepthAndRejectsTheNextLevelAsync()
    {
        var handled = 0;
        IMediator mediator = Bus.Factory.CreateMediator(configuration =>
        {
            configuration.Limits(new MessageLimits { MaxBodyBytes = 4096, MaxEnvelopeBytes = 4096, MaxJsonDepth = 3 });
            configuration.Handler<DepthMessage>(_ =>
            {
                Interlocked.Increment(ref handled);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        await mediator.SendAsync(Depth(3), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            mediator.SendAsync(Depth(4), TestContext.Current.CancellationToken));

        Assert.Equal(1, Volatile.Read(ref handled));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "short-circuit-filter-responds-and-notifies-consumed")]
    public async Task ShortCircuitFilter_RespondsWithoutInvokingConsumerAndStillNotifiesConsumedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ShortCircuitObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<ShortCircuitConsumer>();
                configuration.ConfigureMediator((context, mediator) =>
                    mediator.UseConsumeFilter(typeof(ShortCircuitFilter<>), context));
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        IMediator mediator = provider.GetRequiredService<IMediator>();
        var observer = new ShortCircuitConsumeObserver(observation);
        using ConnectHandle handle = mediator.ConnectConsumeObserver(observer);
        IRequestClient<ShortCircuitRequest> client = mediator.CreateRequestClient<ShortCircuitRequest>();
        Guid correlationId = NewId.NextGuid();

        Response<ShortCircuitResponse> response = await client.GetResponseAsync<ShortCircuitResponse>(
            new ShortCircuitRequest(correlationId),
            cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid postConsumed = await observation.PostConsumed.Task.WaitAsync(timeout, cancellationToken);

        Assert.Equal(correlationId, response.Message.CorrelationId);
        Assert.Equal("filter", response.Message.Source);
        Assert.Equal(correlationId, postConsumed);
        Assert.Equal(1, observation.FilterInvocations);
        Assert.Equal(0, observation.ConsumerInvocations);
        Assert.False(observation.ConsumeFault.Task.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "dynamic-handler-connect-disconnect")]
    public async Task DynamicHandler_ReceivesBeforeDisconnectAndNotAfterDisconnectAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = Bus.Factory.CreateMediator(configuration => configuration.Limits(MessageLimits.Conservative));
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        var received = 0;
        var first = new DispatchMessage("first");
        var second = new DispatchMessage("second");

        using (ConnectHandle handle = mediator.ConnectHandler<DispatchMessage>(context =>
               {
                   Assert.Same(first, context.Message);
                   Interlocked.Increment(ref received);
                   return Task.CompletedTask;
               }))
        {
            await mediator.PublishAsync(first, cancellationToken).WaitAsync(timeout, cancellationToken);
            Assert.Equal(1, Volatile.Read(ref received));
        }

        await mediator.PublishAsync(second, cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(1, Volatile.Read(ref received));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-and-publish-exactly-once")]
    public async Task SendAndPublish_DeliverTheirExactMessagesExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var sentCount = 0;
        var publishedCount = 0;
        var sent = new SentMessage(NewId.NextGuid());
        var published = new PublishedMessage(NewId.NextGuid());
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<SentMessage>(context =>
            {
                Assert.Same(sent, context.Message);
                Interlocked.Increment(ref sentCount);
                return Task.CompletedTask;
            });
            configurator.Handler<PublishedMessage>(context =>
            {
                Assert.Same(published, context.Message);
                Interlocked.Increment(ref publishedCount);
                return Task.CompletedTask;
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        await mediator.SendAsync(sent, cancellationToken).WaitAsync(timeout, cancellationToken);
        await mediator.PublishAsync(published, cancellationToken).WaitAsync(timeout, cancellationToken);

        Assert.Equal(1, Volatile.Read(ref sentCount));
        Assert.Equal(1, Volatile.Read(ref publishedCount));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-original-exception")]
    public async Task Send_PropagatesTheOriginalHandlerExceptionAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new MediatorDispatchException("handler failed");
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<DispatchMessage>(_ => throw expected);
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        MediatorDispatchException actual = await Assert.ThrowsAsync<MediatorDispatchException>(() =>
            mediator.SendAsync(new DispatchMessage("fault"), cancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "send-caller-cancellation")]
    public async Task Send_PropagatesRequestedCallerCancellationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = NewSignal();
        var never = NewSignal();
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<DispatchMessage>(async context =>
            {
                entered.TrySetResult();
                await never.Task.WaitAsync(context.CancellationToken);
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        Task send = mediator.SendAsync(new DispatchMessage("cancel"), source.Token);
        await entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "request-caller-cancellation")]
    public async Task Request_PropagatesRequestedCallerCancellationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = NewSignal();
        var never = NewSignal();
        IMediator mediator = Bus.Factory.CreateMediator(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.Handler<RequestMessage>(async context =>
            {
                entered.TrySetResult();
                await never.Task.WaitAsync(context.CancellationToken);
                await context.RespondAsync(new ResponseMessage(context.Message.CorrelationId));
            });
        });
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);
        IRequestClient<RequestMessage> client = mediator.CreateRequestClient<RequestMessage>(new RequestTimeout(TimeSpan.FromMinutes(1)));
        var request = new RequestMessage(NewId.NextGuid());

        Task<Response<ResponseMessage>> response = client.GetResponseAsync<ResponseMessage>(request, source.Token);
        await entered.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        source.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => response);
        Assert.True(source.IsCancellationRequested);
        Assert.True(response.IsCanceled);
        Assert.True(exception.CancellationToken.CanBeCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DISPATCH", "missing-consumer-mandatory-boundary")]
    public async Task PublishWithoutConsumer_UsesTheMandatoryBoundaryAsync(bool mandatory)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IMediator mediator = Bus.Factory.CreateMediator(configuration => configuration.Limits(MessageLimits.Conservative));
        await using IAsyncDisposable lifetime = Assert.IsAssignableFrom<IAsyncDisposable>(mediator);

        Task publish = mediator.PublishAsync(
            new DispatchMessage(mandatory ? "mandatory" : "optional"),
            context => { context.Mandatory = mandatory; },
            cancellationToken);

        if (mandatory)
        {
            MessageNotConsumedException exception =
                await Assert.ThrowsAsync<MessageNotConsumedException>(() => publish);
            Assert.Contains("not consumed", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            await publish;
            Assert.True(publish.IsCompletedSuccessfully);
        }
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static DepthMessage Depth(int levels)
    {
        DepthMessage? current = null;
        for (var index = 0; index < levels; index++)
            current = new DepthMessage { Child = current };

        return current!;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record DispatchMessage(string Value);

    private sealed class DepthMessage
    {
        public DepthMessage? Child { get; init; }
    }

    private sealed record SentMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record PublishedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record RequestMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record ResponseMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record AddressedRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record AddressedResponse(Uri? DestinationAddress);

    private sealed class MediatorDispatchException(string message) : Exception(message);

    public sealed record ShortCircuitRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ShortCircuitResponse(Guid CorrelationId, string Source) : CorrelatedBy<Guid>;

    public sealed class ShortCircuitObservation
    {
        private int _filterInvocations;
        private int _consumerInvocations;

        public int FilterInvocations => Volatile.Read(ref _filterInvocations);

        public int ConsumerInvocations => Volatile.Read(ref _consumerInvocations);

        public TaskCompletionSource<Guid> PostConsumed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Exception> ConsumeFault { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordFilter() => Interlocked.Increment(ref _filterInvocations);

        public void RecordConsumer() => Interlocked.Increment(ref _consumerInvocations);
    }

    public sealed class ShortCircuitFilter<T>(ShortCircuitObservation observation) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            if (context is ConsumeContext<ShortCircuitRequest> request)
            {
                observation.RecordFilter();
                await context.NotifyConsumedAsync(context.Advanced().ReceiveContext.ElapsedTime, nameof(ShortCircuitFilter<T>));
                await request.RespondAsync(new ShortCircuitResponse(
                    request.Message.CorrelationId,
                    "filter"));
                return;
            }

            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("shortCircuit");
    }

    public sealed class ShortCircuitConsumer(ShortCircuitObservation observation) : IConsumer<ShortCircuitRequest>
    {
        public async Task ConsumeAsync(ConsumeContext<ShortCircuitRequest> context)
        {
            observation.RecordConsumer();
            await context.RespondAsync(new ShortCircuitResponse(
                context.Message.CorrelationId,
                "consumer"));
        }
    }

    public sealed class ShortCircuitConsumeObserver(ShortCircuitObservation observation) : IConsumeObserver
    {
        public Task PreConsumeAsync<T>(ConsumeContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostConsumeAsync<T>(ConsumeContext<T> context)
            where T : class
        {
            if (context.Message is ShortCircuitRequest request)
                observation.PostConsumed.TrySetResult(request.CorrelationId);

            return Task.CompletedTask;
        }

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
            where T : class
        {
            observation.ConsumeFault.TrySetResult(exception);
            return Task.CompletedTask;
        }
    }
}
