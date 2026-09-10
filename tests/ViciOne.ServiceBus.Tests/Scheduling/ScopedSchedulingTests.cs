using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class ScopedSchedulingTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-SCOPE", "scheduled-send-uses-current-consume-scope")]
    public async Task ScheduledSend_UsesTheSameScopedServiceAsTheInitiatingConsumerAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ScheduleScopeObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddScoped<ScheduleScope>()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddDelayedMessageScheduler();
                configuration.AddConsumer<ScheduleCommandConsumer>();
                configuration.AddConsumer<ScheduledMessageConsumer>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.UseSendFilter(typeof(ScheduleScopeSendFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.Bus.PublishAsync(new ScheduleCommand(correlationId), cancellationToken);
            IConsumedMessage<ScheduleCommand> command = await harness.Consumed
                .SelectAsync<ScheduleCommand>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(correlationId, command.Context.Message.CorrelationId);
            Assert.Null(command.Exception);

            ScheduleScopeResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(correlationId, result.CorrelationId);
            Assert.Equal(result.InitiatingScopeId, result.FilterScopeId);
            Assert.Equal(result.InitiatingScopeId.ToString("N"), result.HeaderValue);
            Assert.Equal(1, observation.FilterInvocations);
            Assert.Single(harness.Sent.Snapshot<ScheduledMessage>());
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ScheduleCommand(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ScheduledMessage(Guid CorrelationId, Guid InitiatingScopeId) : CorrelatedBy<Guid>;

    public sealed class ScheduleScope
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    public sealed record ScheduleScopeResult(
        Guid CorrelationId,
        Guid InitiatingScopeId,
        Guid FilterScopeId,
        string? HeaderValue);

    public sealed class ScheduleScopeObservation
    {
        private int _filterInvocations;

        public int FilterInvocations => Volatile.Read(ref _filterInvocations);

        public TaskCompletionSource<ScheduleScopeResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void RecordFilter() => Interlocked.Increment(ref _filterInvocations);
    }

    public sealed class ScheduleCommandConsumer(ScheduleScope scope) : IConsumer<ScheduleCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ScheduleCommand> context)
        {
            await context.Advanced().ScheduleSendAsync(
                new Uri($"queue:{DefaultEndpointNameFormatter.Instance.Consumer<ScheduledMessageConsumer>()}"),
                TimeSpan.Zero,
                new ScheduledMessage(context.Message.CorrelationId, scope.Id),
                context.CancellationToken);
        }
    }

    public sealed class ScheduleScopeSendFilter<T>(
        ScheduleScope scope,
        ScheduleScopeObservation observation) : IFilter<SendContext<T>>
        where T : class
    {
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            if (context.Message is ScheduledMessage)
            {
                observation.RecordFilter();
                context.Headers.Set("Schedule-Scope", scope.Id.ToString("N"));
                context.Headers.Set("Schedule-Scope-Id", scope.Id);
            }

            return next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("scheduleScope");
    }

    public sealed class ScheduledMessageConsumer(
        ScheduleScopeObservation observation) : IConsumer<ScheduledMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ScheduledMessage> context)
        {
            if (!context.Advanced().TryGetHeader("Schedule-Scope-Id", out Guid? filterScopeId)
                || !filterScopeId.HasValue)
                throw new InvalidDataException("The schedule scope identifier is missing.");
            observation.Completed.TrySetResult(new ScheduleScopeResult(
                context.Message.CorrelationId,
                context.Message.InitiatingScopeId,
                filterScopeId.Value,
                context.Headers.Get<string>("Schedule-Scope")));
            return Task.CompletedTask;
        }
    }
}
