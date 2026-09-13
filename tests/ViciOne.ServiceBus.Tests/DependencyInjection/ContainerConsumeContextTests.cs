using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware.InMemoryOutbox;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ContainerConsumeContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-CONTEXT", "message-context-is-the-scoped-endpoint-provider")]
    public async Task ScopedConsumer_ReceivesTheExactMessageOwnedEndpointContextAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ContextObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<PlainContextConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            var message = new ContextCommand(NewId.NextGuid());
            await harness.Bus.PublishAsync(message, cancellationToken);
            ContextSnapshot snapshot = await observation.Captured.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, snapshot.CorrelationId);
            Assert.True(snapshot.HasMessageContextPayload);
            Assert.Same(snapshot.ConsumeContext, snapshot.PublishEndpoint);
            Assert.Same(snapshot.ConsumeContext, snapshot.SendEndpointProvider);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(OutboxShape.DependencyWithRegistrationContext)]
    [InlineData(OutboxShape.DependencyWithoutRegistrationContext)]
    [InlineData(OutboxShape.BatchWithRetryAndRegistrationContext)]
    [InlineData(OutboxShape.BatchWithRetryWithoutRegistrationContext)]
    [InlineData(OutboxShape.BatchPlainWithRegistrationContext)]
    [InlineData(OutboxShape.BatchPlainWithoutRegistrationContext)]
    [InlineData(OutboxShape.DirectWithRegistrationContext)]
    [InlineData(OutboxShape.DirectWithoutRegistrationContext)]
    [RequirementCoverage("REQ-VSB-CONTAINER-OUTBOX-CONTEXT", "dependency-batch-and-direct-injection-matrix")]
    public async Task FaultedConsumer_UsesTheExactOutboxContextAndDiscardsEverySideEffectAsync(OutboxShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ContextObservation();
        await using ServiceProvider provider = BuildOutboxProvider(shape, observation, timeout);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        bool started = true;

        try
        {
            var message = new ContextCommand(NewId.NextGuid());
            if (IsBatch(shape))
            {
                ContextCommand[] batch = Enumerable.Range(0, 4)
                    .Select(index => index == 0 ? message : new ContextCommand(NewId.NextGuid()))
                    .ToArray();
                await harness.Bus.PublishBatchAsync(batch, cancellationToken);
            }
            else
                await harness.Bus.PublishAsync(message, cancellationToken);

            ContextSnapshot snapshot = await observation.Captured.Task.WaitAsync(timeout, cancellationToken);
            IPublishedMessage<Fault<ContextCommand>> terminalFault = await harness.Published
                .SelectAsync<Fault<ContextCommand>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            Assert.Contains(
                terminalFault.Context.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedContextFailure>.ShortName);
            if (IsBatch(shape))
            {
                Assert.True(snapshot.HasBatchOutboxPayload);
                Assert.Equal(4, snapshot.BatchLength);
                Assert.Equal(1, observation.UnitOfWorkUses);
            }
            else
            {
                Assert.Equal(message.CorrelationId, terminalFault.Context.Message.Message.CorrelationId);
                Assert.True(snapshot.HasOutboxPayload);
                Assert.Equal(0, observation.UnitOfWorkUses);
            }

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            started = false;

            Assert.Same(snapshot.ConsumeContext, snapshot.PublishEndpoint);
            Assert.Same(snapshot.ConsumeContext, snapshot.SendEndpointProvider);
            Assert.Empty(harness.Published.Snapshot<DeferredSideEffect>());
            Assert.Equal(IsBatch(shape) ? 4 : 1,
                harness.Published.Snapshot<Fault<ContextCommand>>().Count());
            Assert.Equal(0, observation.DeliveredSideEffects);
        }
        finally
        {
            if (started)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildOutboxProvider(
        OutboxShape shape,
        ContextObservation observation,
        TimeSpan timeout)
    {
        var services = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopedContextCapture>()
            .AddScoped<OutboxPublishingDependency>()
            .AddScoped<OutboxUnitOfWork>();

        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            if (IsBatch(shape))
            {
                configuration.AddConsumer<FaultingBatchContextConsumer>(consumer =>
                    consumer.Options<BatchOptions>(options => options.SetMessageLimit(4)));
            }
            else if (IsDirect(shape))
                configuration.AddConsumer<FaultingDirectContextConsumer>();
            else
                configuration.AddConsumer<FaultingDependencyContextConsumer>();

            configuration.AddConsumer<SideEffectConsumer>();
            configuration.AddConfigureEndpointsCallback((context, _, endpoint) =>
            {
                if (UsesRetryFilters(shape))
                {
                    endpoint.UseDelayedRedelivery(redelivery => redelivery.None());
                    endpoint.UseMessageRetry(retry => retry.None());
                }

                if (UsesRegistrationContext(shape))
                    endpoint.UseVolatileOutbox(context);
                else
                    endpoint.UseVolatileOutbox();

                if (IsBatch(shape))
                    endpoint.ConnectConsumerConfigurationObserver(new OutboxUnitOfWorkObserver());
            });
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static bool IsBatch(OutboxShape shape) => shape is
        OutboxShape.BatchWithRetryAndRegistrationContext or
        OutboxShape.BatchWithRetryWithoutRegistrationContext or
        OutboxShape.BatchPlainWithRegistrationContext or
        OutboxShape.BatchPlainWithoutRegistrationContext;

    private static bool IsDirect(OutboxShape shape) => shape is
        OutboxShape.DirectWithRegistrationContext or
        OutboxShape.DirectWithoutRegistrationContext;

    private static bool UsesRetryFilters(OutboxShape shape) => shape is
        OutboxShape.BatchWithRetryAndRegistrationContext or
        OutboxShape.BatchWithRetryWithoutRegistrationContext;

    private static bool UsesRegistrationContext(OutboxShape shape) => shape is
        OutboxShape.DependencyWithRegistrationContext or
        OutboxShape.BatchWithRetryAndRegistrationContext or
        OutboxShape.BatchPlainWithRegistrationContext or
        OutboxShape.DirectWithRegistrationContext;


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum OutboxShape
    {
        DependencyWithRegistrationContext,
        DependencyWithoutRegistrationContext,
        BatchWithRetryAndRegistrationContext,
        BatchWithRetryWithoutRegistrationContext,
        BatchPlainWithRegistrationContext,
        BatchPlainWithoutRegistrationContext,
        DirectWithRegistrationContext,
        DirectWithoutRegistrationContext,
    }

    public sealed record ContextCommand(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record DeferredSideEffect(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ContextSnapshot(
        Guid CorrelationId,
        ConsumeContext ConsumeContext,
        IPublishEndpoint PublishEndpoint,
        ISendEndpointProvider SendEndpointProvider,
        bool HasMessageContextPayload,
        bool HasOutboxPayload,
        bool HasBatchOutboxPayload,
        int BatchLength);

    public sealed class ContextObservation
    {
        private int _deliveredSideEffects;
        private int _unitOfWorkUses;

        public TaskCompletionSource<ContextSnapshot> Captured { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DeliveredSideEffects => Volatile.Read(ref _deliveredSideEffects);

        public int UnitOfWorkUses => Volatile.Read(ref _unitOfWorkUses);

        public void SideEffectDelivered() => Interlocked.Increment(ref _deliveredSideEffects);

        public void UnitOfWorkUsed() => Interlocked.Increment(ref _unitOfWorkUses);
    }

    public sealed class ScopedContextCapture(
        ConsumeContext consumeContext,
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ContextObservation observation)
    {
        public void Record(Guid correlationId, int batchLength = 0)
        {
            observation.Captured.TrySetResult(new ContextSnapshot(
                correlationId,
                consumeContext,
                publishEndpoint,
                sendEndpointProvider,
                consumeContext.TryGetPayload(out MessageConsumeContext<ContextCommand>? _),
                consumeContext.TryGetPayload(out InMemoryOutboxConsumeContext<ContextCommand>? _),
                consumeContext.TryGetPayload(out InMemoryOutboxConsumeContext<ContextCommand>.BatchContext? _),
                batchLength));
        }
    }

    public sealed class PlainContextConsumer(
        ConsumeContext consumeContext,
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ContextObservation observation) : IConsumer<ContextCommand>
    {
        public Task ConsumeAsync(ConsumeContext<ContextCommand> context)
        {
            observation.Captured.TrySetResult(new ContextSnapshot(
                context.Message.CorrelationId,
                consumeContext,
                publishEndpoint,
                sendEndpointProvider,
                consumeContext.TryGetPayload(out MessageConsumeContext<ContextCommand>? _),
                false,
                false,
                0));
            return Task.CompletedTask;
        }
    }

    public sealed class OutboxPublishingDependency(
        ScopedContextCapture capture,
        IPublishEndpoint publishEndpoint)
    {
        public async Task ExecuteAsync(Guid correlationId, int batchLength = 0)
        {
            capture.Record(correlationId, batchLength);
            await publishEndpoint.PublishAsync(new DeferredSideEffect(correlationId));
        }
    }

    public sealed class FaultingDependencyContextConsumer(OutboxPublishingDependency dependency) :
        IConsumer<ContextCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ContextCommand> context)
        {
            await dependency.ExecuteAsync(context.Message.CorrelationId);
            throw new ExpectedContextFailure();
        }
    }

    public sealed class FaultingDirectContextConsumer(
        ConsumeContext consumeContext,
        IPublishEndpoint publishEndpoint,
        ISendEndpointProvider sendEndpointProvider,
        ContextObservation observation) : IConsumer<ContextCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ContextCommand> context)
        {
            observation.Captured.TrySetResult(new ContextSnapshot(
                context.Message.CorrelationId,
                consumeContext,
                publishEndpoint,
                sendEndpointProvider,
                false,
                consumeContext.TryGetPayload(out InMemoryOutboxConsumeContext<ContextCommand>? _),
                false,
                0));
            await publishEndpoint.PublishAsync(new DeferredSideEffect(context.Message.CorrelationId));
            throw new ExpectedContextFailure();
        }
    }

    public sealed class FaultingBatchContextConsumer(OutboxPublishingDependency dependency) :
        IConsumer<IMessageBatch<ContextCommand>>
    {
        public async Task ConsumeAsync(ConsumeContext<IMessageBatch<ContextCommand>> context)
        {
            Guid correlationId = context.Message[0].Message.CorrelationId;
            await dependency.ExecuteAsync(correlationId, context.Message.Count);
            throw new ExpectedContextFailure();
        }
    }

    public sealed class SideEffectConsumer(ContextObservation observation) : IConsumer<DeferredSideEffect>
    {
        public Task ConsumeAsync(ConsumeContext<DeferredSideEffect> context)
        {
            observation.SideEffectDelivered();
            return Task.CompletedTask;
        }
    }

    public sealed class OutboxUnitOfWork(ContextObservation observation)
    {
        public void Use() => observation.UnitOfWorkUsed();
    }

    public sealed class OutboxUnitOfWorkObserver : IConsumerConfigurationObserver
    {
        public void ConsumerConfigured<TConsumer>(IConsumerConfigurator<TConsumer> configurator)
            where TConsumer : class =>
            configurator.AddPipeSpecification(
                new FilterPipeSpecification<ConsumerConsumeContext<TConsumer>>(
                    new OutboxUnitOfWorkFilter<TConsumer>()));

        public void ConsumerMessageConfigured<TConsumer, TMessage>(
            IConsumerMessageConfigurator<TConsumer, TMessage> configurator)
            where TConsumer : class
            where TMessage : class
        {
        }
    }

    public sealed class OutboxUnitOfWorkFilter<TConsumer> : IFilter<ConsumerConsumeContext<TConsumer>>
        where TConsumer : class
    {
        public Task SendAsync(
            ConsumerConsumeContext<TConsumer> context,
            IPipe<ConsumerConsumeContext<TConsumer>> next)
        {
            context.GetPayload<IServiceProvider>().GetRequiredService<OutboxUnitOfWork>().Use();
            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("outboxUnitOfWork");
    }

    public sealed class ExpectedContextFailure : Exception;
}
