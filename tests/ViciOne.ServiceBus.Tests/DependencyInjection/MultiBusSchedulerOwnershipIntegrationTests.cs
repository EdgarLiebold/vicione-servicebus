using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MultiBusSchedulerOwnershipIntegrationTests
{
    [Theory]
    [InlineData(SchedulerKind.Endpoint)]
    [InlineData(SchedulerKind.Publish)]
    [InlineData(SchedulerKind.Delayed)]
    [RequirementCoverage("REQ-VSB-SCHEDULER-SCOPE", "multibus-schedulers-preserve-scope-clock-and-command-owner")]
    public async Task ScopedSchedulers_DeliverCommandsThroughOnlyTheirOwningBusAsync(SchedulerKind kind)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2039, 5, 6, 7, 8, 9, TimeSpan.Zero));
        var observations = new ConcurrentQueue<Observation>();
        Channel<Observation> received = Channel.CreateUnbounded<Observation>();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddSingleton<TimeProvider>(clock);
        services.AddScoped<ScopeStamp>();
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            switch (kind)
            {
                case SchedulerKind.Endpoint:
                    bus.AddMessageScheduler(Address("primary", "scheduler"));
                    break;
                case SchedulerKind.Publish:
                    bus.AddPublishMessageScheduler();
                    break;
                case SchedulerKind.Delayed:
                    bus.AddDelayedMessageScheduler();
                    break;
            }
            bus.UsingInMemory((context, transport) => ConfigureTransport(context, transport, "primary"));
        });
        services.AddViciOneServiceBus<ISecondaryBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            switch (kind)
            {
                case SchedulerKind.Endpoint:
                    bus.AddMessageScheduler(Address("secondary", "scheduler"));
                    break;
                case SchedulerKind.Publish:
                    bus.AddPublishMessageScheduler();
                    break;
                case SchedulerKind.Delayed:
                    bus.AddDelayedMessageScheduler();
                    break;
            }
            bus.UsingInMemory((context, transport) => ConfigureTransport(context, transport, "secondary"));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope primaryScope = provider.CreateAsyncScope();
        await using AsyncServiceScope secondaryScope = provider.CreateAsyncScope();
        IMessageScheduler primaryScheduler = primaryScope.ServiceProvider.GetRequiredService<IMessageScheduler>();
        IMessageScheduler secondaryScheduler = secondaryScope.ServiceProvider.GetRequiredService<Bind<ISecondaryBus, IMessageScheduler>>().Value;
        Assert.Same(primaryScheduler, primaryScope.ServiceProvider.GetRequiredService<IMessageScheduler>());
        Assert.Same(secondaryScheduler, secondaryScope.ServiceProvider.GetRequiredService<Bind<ISecondaryBus, IMessageScheduler>>().Value);
        Assert.NotSame(primaryScheduler, secondaryScope.ServiceProvider.GetRequiredService<IMessageScheduler>());
        Assert.NotSame(secondaryScheduler, primaryScope.ServiceProvider.GetRequiredService<Bind<ISecondaryBus, IMessageScheduler>>().Value);
        Assert.NotSame(primaryScheduler, secondaryScheduler);
        Assert.Same(clock, primaryScheduler.TimeProvider);
        Assert.Same(clock, secondaryScheduler.TimeProvider);
        Guid primaryStamp = primaryScope.ServiceProvider.GetRequiredService<ScopeStamp>().Id;
        Guid secondaryStamp = secondaryScope.ServiceProvider.GetRequiredService<ScopeStamp>().Id;
        Assert.NotEqual(primaryStamp, secondaryStamp);

        IBusControl primary = provider.GetRequiredService<IBusControl>();
        IBusControl secondary = (IBusControl)provider.GetRequiredService<ISecondaryBus>();
        try
        {
            await primary.StartAsync(Token).WaitAsync(Timeout, Token);
            await secondary.StartAsync(Token).WaitAsync(Timeout, Token);
            await ExerciseAsync(primaryScheduler, primaryScope.ServiceProvider, "primary", primaryStamp);
            await ExerciseAsync(secondaryScheduler, secondaryScope.ServiceProvider, "secondary", secondaryStamp);
        }
        finally
        {
            await secondary.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            await primary.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }
        Assert.Equal(kind == SchedulerKind.Delayed ? 2 : 12, observations.Count);

        async Task ExerciseAsync(IMessageScheduler scheduler, IServiceProvider scope, string owner, Guid stamp)
        {
            using (var canceledAdmission = new CancellationTokenSource())
            {
                canceledAdmission.Cancel();
                OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => scheduler.ScheduleSendAsync(Address(owner, "destination"), TimeSpan.Zero,
                        new OwnerMessage("rejected"), canceledAdmission.Token));
                Assert.Equal(canceledAdmission.Token, cancellation.CancellationToken);
            }
            var payload = new OwnerMessage(owner + "-payload");
            Guid correlation = NewId.NextGuid();
            var options = new ScheduleOptions
            {
                CorrelationId = correlation,
                Headers = new Dictionary<string, object?> { ["Owner"] = owner },
            };
            TimeSpan delay = kind == SchedulerKind.Delayed ? TimeSpan.Zero : TimeSpan.FromMinutes(3);
            ScheduledMessage<OwnerMessage> scheduled = await scheduler.ScheduleSendAsync(
                Address(owner, "destination"), delay, payload, options, Token);
            Assert.Equal(clock.GetUtcNow() + delay, scheduled.DueAt);
            Assert.Equal(Address(owner, "destination"), scheduled.Destination);
            Assert.Same(payload, scheduled.Payload);
            Assert.NotEqual(Guid.Empty, scheduled.TokenId);

            Observation delivery = await NextAsync(owner, stamp);
            Assert.Equal(correlation, delivery.CorrelationId);
            Assert.Equal(owner, delivery.OwnerHeader);
            if (kind == SchedulerKind.Delayed)
            {
                Assert.Equal(payload, Assert.IsType<OwnerMessage>(delivery.Message));
                Assert.Equal(Address(owner, "destination"), delivery.InputAddress);
                await Assert.ThrowsAsync<NotSupportedException>(() => scheduler.CancelScheduledSendAsync(scheduled, Token));
                using var canceled = new CancellationTokenSource();
                canceled.Cancel();
                OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => scheduler.CancelScheduledSendAsync(scheduled, canceled.Token));
                Assert.Equal(canceled.Token, cancellation.CancellationToken);
                return;
            }

            ScheduleMessage command = Assert.IsAssignableFrom<ScheduleMessage>(delivery.Message);
            Assert.Equal(scheduled.TokenId, command.TokenId);
            Assert.Equal(scheduled.DueAt, command.DueAt);
            Assert.Equal(Address(owner, "destination"), command.Destination);
            Assert.Equal(payload.Marker, Assert.IsType<JsonElement>(command.Payload).GetProperty("marker").GetString());
            Assert.Contains(MessageUrn.ForTypeString<OwnerMessage>(), command.PayloadType);
            Assert.Equal(Address(owner, "scheduler"), delivery.InputAddress);
            await scheduler.CancelScheduledSendAsync(scheduled, Token);
            CancelScheduledMessage cancel = Assert.IsAssignableFrom<CancelScheduledMessage>((await NextAsync(owner, stamp)).Message);
            Assert.Equal(scheduled.TokenId, cancel.TokenId);
            Assert.Equal(clock.GetUtcNow(), cancel.Timestamp);

            IRecurringMessageScheduler recurring = owner == "primary"
                ? scope.GetRequiredService<IRecurringMessageScheduler>()
                : scope.GetRequiredService<Bind<ISecondaryBus, IRecurringMessageScheduler>>().Value;
            Assert.Same(clock, recurring.TimeProvider);
            Assert.Same(recurring, owner == "primary"
                ? scope.GetRequiredService<IRecurringMessageScheduler>()
                : scope.GetRequiredService<Bind<ISecondaryBus, IRecurringMessageScheduler>>().Value);
            IServiceProvider otherScope = owner == "primary" ? secondaryScope.ServiceProvider : primaryScope.ServiceProvider;
            Assert.NotSame(recurring, owner == "primary"
                ? otherScope.GetRequiredService<IRecurringMessageScheduler>()
                : otherScope.GetRequiredService<Bind<ISecondaryBus, IRecurringMessageScheduler>>().Value);
            var recurrence = new OwnerSchedule(owner, clock);
            ScheduledRecurringMessage<OwnerMessage> recurringHandle = await recurring.ScheduleRecurringSendAsync(
                Address(owner, "destination"), recurrence, payload, Token);
            Assert.Same(recurrence, recurringHandle.Schedule);
            Assert.Same(payload, recurringHandle.Payload);
            Assert.Equal(Address(owner, "destination"), recurringHandle.Destination);
            ScheduleRecurringMessage recurringCommand = Assert.IsAssignableFrom<ScheduleRecurringMessage>((await NextAsync(owner, stamp)).Message);
            Assert.Equal(owner + "-schedule", recurringCommand.Schedule.ScheduleId);
            Assert.Equal(owner + "-group", recurringCommand.Schedule.ScheduleGroup);
            Assert.Equal(recurrence.StartTime, recurringCommand.Schedule.StartTime);
            Assert.Equal(recurrence.CronExpression, recurringCommand.Schedule.CronExpression);
            Assert.Equal(Address(owner, "destination"), recurringCommand.Destination);
            Assert.Equal(payload.Marker, Assert.IsType<JsonElement>(recurringCommand.Payload).GetProperty("marker").GetString());

            await recurring.PauseScheduledRecurringSendAsync(recurrence.ScheduleId, recurrence.ScheduleGroup, Token);
            PauseScheduledRecurringMessage pause = Assert.IsAssignableFrom<PauseScheduledRecurringMessage>((await NextAsync(owner, stamp)).Message);
            Assert.Equal(recurrence.ScheduleId, pause.ScheduleId);
            Assert.Equal(recurrence.ScheduleGroup, pause.ScheduleGroup);
            Assert.Equal(clock.GetUtcNow(), pause.Timestamp);
            await recurring.ResumeScheduledRecurringSendAsync(recurrence.ScheduleId, recurrence.ScheduleGroup, Token);
            ResumeScheduledRecurringMessage resume = Assert.IsAssignableFrom<ResumeScheduledRecurringMessage>((await NextAsync(owner, stamp)).Message);
            Assert.Equal(recurrence.ScheduleId, resume.ScheduleId);
            Assert.Equal(recurrence.ScheduleGroup, resume.ScheduleGroup);
            Assert.Equal(clock.GetUtcNow(), resume.Timestamp);
            await recurring.CancelScheduledRecurringSendAsync(recurrence.ScheduleId, recurrence.ScheduleGroup, Token);
            CancelScheduledRecurringMessage recurringCancel = Assert.IsAssignableFrom<CancelScheduledRecurringMessage>((await NextAsync(owner, stamp)).Message);
            Assert.Equal(recurrence.ScheduleId, recurringCancel.ScheduleId);
            Assert.Equal(recurrence.ScheduleGroup, recurringCancel.ScheduleGroup);
            Assert.Equal(clock.GetUtcNow(), recurringCancel.Timestamp);
        }

        async Task<Observation> NextAsync(string owner, Guid stamp)
        {
            Observation observation = await received.Reader.ReadAsync(Token).AsTask().WaitAsync(Timeout, Token);
            Assert.Equal(owner, observation.Bus);
            Assert.Equal(stamp.ToString("D"), observation.ScopeHeader);
            Assert.Equal("t48-scheduler-" + owner, observation.SourceAddress?.Host);
            return observation;
        }

        void ConfigureTransport(IBusRegistrationContext context, IInMemoryBusFactoryConfigurator bus, string owner)
        {
            bus.Host(Address(owner, ""));
            bus.UseSendFilter(typeof(ScopeSendFilter<>), context);
            bus.UsePublishFilter(typeof(ScopePublishFilter<>), context);
            bus.ReceiveEndpoint("destination", endpoint => endpoint.Handler<OwnerMessage>(RecordAsync));
            bus.ReceiveEndpoint("scheduler", endpoint =>
            {
                endpoint.Handler<ScheduleMessage>(RecordAsync);
                endpoint.Handler<CancelScheduledMessage>(RecordAsync);
                endpoint.Handler<ScheduleRecurringMessage>(RecordAsync);
                endpoint.Handler<PauseScheduledRecurringMessage>(RecordAsync);
                endpoint.Handler<ResumeScheduledRecurringMessage>(RecordAsync);
                endpoint.Handler<CancelScheduledRecurringMessage>(RecordAsync);
            });

            Task RecordAsync<T>(ConsumeContext<T> context) where T : class
            {
                var observation = new Observation(owner, context.Message, context.SourceAddress,
                    context.Advanced().ReceiveContext.InputAddress, context.CorrelationId,
                    context.Headers.Get<string>("Owner"), context.Headers.Get<string>("Scope"));
                observations.Enqueue(observation);
                received.Writer.TryWrite(observation);
                return Task.CompletedTask;
            }
        }
    }

    private static Uri Address(string owner, string path) => new($"loopback://t48-scheduler-{owner}/{path}");
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    public enum SchedulerKind { Endpoint, Publish, Delayed }
    public interface ISecondaryBus : IBus;
    public sealed record OwnerMessage(string Marker);
    public sealed class ScopeStamp { public Guid Id { get; } = NewId.NextGuid(); }
    private sealed record Observation(string Bus, object Message, Uri? SourceAddress, Uri InputAddress,
        Guid? CorrelationId, string? OwnerHeader, string? ScopeHeader);

    private sealed class OwnerSchedule : DefaultRecurringSchedule
    {
        public OwnerSchedule(string owner, TimeProvider clock) : base("0 0 2 * * ?", timeProvider: clock)
        {
            ScheduleId = owner + "-schedule";
            ScheduleGroup = owner + "-group";
        }
    }

    public sealed class ScopeSendFilter<T>(ScopeStamp stamp) : IFilter<SendContext<T>> where T : class
    {
        public void Probe(ProbeContext context) => context.CreateFilterScope("scopeStamp");
        public Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            context.Headers.Set("Scope", stamp.Id.ToString("D"));
            return next.SendAsync(context);
        }
    }

    public sealed class ScopePublishFilter<T>(ScopeStamp stamp) : IFilter<PublishContext<T>> where T : class
    {
        public void Probe(ProbeContext context) => context.CreateFilterScope("scopeStamp");
        public Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            context.Headers.Set("Scope", stamp.Id.ToString("D"));
            return next.SendAsync(context);
        }
    }
}
