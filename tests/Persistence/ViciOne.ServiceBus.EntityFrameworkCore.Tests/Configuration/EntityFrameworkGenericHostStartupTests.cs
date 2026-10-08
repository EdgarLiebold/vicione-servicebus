using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using GenericHost = Microsoft.Extensions.Hosting.IHost;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkGenericHostStartupTests(ITestOutputHelper output)
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "actual-host-selected-null-lock-provider")]
    public async Task SelectedEfLockProviderNullFailsBeforeEndpointStart()
    {
        Result result = await RunAsync(Scenario.NullLockProvider);
        AssertOptionsFailure<EntityFrameworkOutboxOptions<OwnedDbContext>>(result,
            $"Entity Framework outbox for bus '{typeof(IOrdersBus).FullName}': LockStatementProvider is not declared. Select exactly one relational provider.");
        AssertRejected(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "actual-host-selected-inbox-zero-query-delay")]
    public async Task SelectedInboxZeroQueryDelayFailsBeforeEndpointStart()
    {
        Result result = await RunAsync(Scenario.ZeroInboxDelay);
        AssertOptionsFailure<InboxCleanupServiceOptions<OwnedDbContext>>(result,
            $"Inbox cleanup for bus '{typeof(IOrdersBus).FullName}': QueryDelay must be greater than zero. Set a positive delay.");
        AssertRejected(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-CONFIGURATION", "actual-host-selected-scoped-reversed-delivery-retry")]
    public async Task SelectedScopedDeliveryReversedRetryFailsBeforeEndpointStart()
    {
        Result result = await RunAsync(Scenario.ReversedRetry);
        AssertOptionsFailure<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IOrdersBus, OwnedDbContext>>>(result,
            $"Outbox delivery for bus '{typeof(IOrdersBus).FullName}': MaximumDeliveryRetryDelay must not be less than InitialDeliveryRetryDelay. Raise the maximum or lower the initial delay.");
        AssertRejected(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-IDENTITY", "actual-host-two-buses-sqlite-workers-unused-persistence-poison")]
    public async Task PlainAndTypedEfBusesDeliverAndStopWithUnusedPersistencePoison()
    {
        Result result = await RunAsync(Scenario.Healthy);
        Assert.Null(result.StartFailure);
        Assert.Null(result.MessageFailure);
        Assert.Equal(2, result.WorkersObserved);
        Assert.True(result.WorkersTerminal);
        Assert.Empty(result.WorkerFailures);
        Assert.True(result.SchemaCreated);
        Assert.Equal(result.DefaultExpected, result.DefaultDelivered);
        Assert.Equal(result.TypedExpected, result.TypedDelivered);
        Assert.NotEqual(result.DefaultExpected, result.TypedExpected);
        Assert.Equal(1, result.DefaultConsumes);
        Assert.Equal(1, result.TypedConsumes);
        Assert.Equal(1, result.DefaultCallbacks);
        Assert.Equal(1, result.TypedCallbacks);
        Assert.Equal(0, result.UnusedEf);
        Assert.Equal(0, result.UnusedInbox);
        Assert.Equal(0, result.UnusedDelivery);
        AssertLifecycle(result.DefaultLifecycle, result.DefaultHost);
        AssertLifecycle(result.TypedLifecycle, result.TypedHost);
        Assert.Equal(1, result.ProbeCreated);
        Assert.Equal(1, result.ProbeStart);
        Assert.Equal(1, result.ProbeStop);
        Assert.Equal(1, result.ProbeDisposed);
        AssertCleanup(result);
    }

    static void AssertOptionsFailure<T>(Result result, string diagnostic)
    {
        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(T), failure.OptionsType);
        Assert.Equal(Options.DefaultName, failure.OptionsName);
        Assert.Equal(diagnostic, Assert.Single(failure.Failures));
    }

    static void AssertRejected(Result result)
    {
        // EF source construction can materialize a bus before its options read; actual endpoint starts must stay zero.
        Assert.InRange(result.DefaultCallbacks, 0, 1);
        Assert.InRange(result.TypedCallbacks, 0, 1);
        Assert.Equal(result.DefaultCallbacks, result.DefaultLifecycle.Created);
        Assert.Equal(result.TypedCallbacks, result.TypedLifecycle.Created);
        Assert.Equal(0, result.DefaultLifecycle.PreStart);
        Assert.Equal(0, result.DefaultLifecycle.PostStart);
        Assert.Equal(0, result.TypedLifecycle.PreStart);
        Assert.Equal(0, result.TypedLifecycle.PostStart);
        Assert.Equal(0, result.DefaultConsumes);
        Assert.Equal(0, result.TypedConsumes);
        Assert.InRange(result.ProbeCreated, 0, 1);
        Assert.Equal(0, result.ProbeStart);
        Assert.Equal(result.ProbeCreated, result.ProbeStop);
        Assert.Equal(result.ProbeCreated, result.ProbeDisposed);
        AssertCleanup(result);
    }

    static void AssertCleanup(Result result)
    {
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Null(result.ObserverFailure);
    }

    static void AssertStarted(Lifecycle result, string host)
    {
        Assert.Equal(host, result.Host);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.PreStart);
        Assert.Equal(1, result.PostStart);
        Assert.True(result.ReadyBusMatches);
        Assert.True(result.AllBusReferencesMatch);
    }

    static void AssertLifecycle(Lifecycle result, string host)
    {
        AssertStarted(result, host);
        Assert.Equal(1, result.PreStop);
        Assert.Equal(1, result.PostStop);
        Assert.Equal(0, result.Faults);
    }

    async Task<Result> RunAsync(Scenario scenario)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        DbContextOptions<OwnedDbContext> database = new DbContextOptionsBuilder<OwnedDbContext>().UseSqlite(connection).Options;
        bool schemaCreated = false;
        if (scenario == Scenario.Healthy)
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var setup = new OwnedDbContext(database);
            schemaCreated = await setup.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });
        IServiceCollection services = builder.Services;
        services.Configure<HostOptions>(options => options.ServicesStartConcurrently = false);
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.Configure<ViciOneServiceBusHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = TimeSpan.FromSeconds(5);
            options.StopTimeout = TimeSpan.FromSeconds(5);
            options.ConsumerStopTimeout = TimeSpan.FromSeconds(5);
        });
        services.AddScoped(_ => new OwnedDbContext(database));
        var defaultObserver = new LifecycleObserver();
        var typedObserver = new LifecycleObserver();
        var connections = new List<ConnectHandle>();
        var primaryDelivered = new TaskCompletionSource<StartupMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondaryDelivered = new TaskCompletionSource<StartupMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var defaultAddress = new Uri($"loopback://ef-startup-default-{NewId.NextGuid():N}/");
        var typedAddress = new Uri($"loopback://ef-startup-typed-{NewId.NextGuid():N}/");
        var primaryMessage = new StartupMessage("default", NewId.NextGuid().ToString("N"));
        var secondaryMessage = new StartupMessage("typed", NewId.NextGuid().ToString("N"));
        int defaultCallbacks = 0, typedCallbacks = 0, defaultConsumes = 0, typedConsumes = 0;
        int unusedEf = 0, unusedInbox = 0, unusedDelivery = 0;
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory((_, configuration) =>
            {
                Interlocked.Increment(ref defaultCallbacks);
                configuration.Host(defaultAddress);
                connections.Add(configuration.ConnectBusObserver(defaultObserver));
                configuration.ReceiveEndpoint("input", endpoint => endpoint.Handler<StartupMessage>(context =>
                {
                    Interlocked.Increment(ref defaultConsumes);
                    primaryDelivered.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            });
        });
        services.AddViciOneServiceBus<IOrdersBus>("vicione.tests.ef-generic-host.orders", bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureEntityFrameworkTransactionalStore<IOrdersBus, OwnedDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.EnableTransactionalOutbox();
            });
            bus.UsingInMemory((_, configuration) =>
            {
                Interlocked.Increment(ref typedCallbacks);
                configuration.Host(typedAddress);
                connections.Add(configuration.ConnectBusObserver(typedObserver));
                configuration.ReceiveEndpoint("input", endpoint => endpoint.Handler<StartupMessage>(context =>
                {
                    Interlocked.Increment(ref typedConsumes);
                    secondaryDelivered.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            });
        });
        if (scenario == Scenario.NullLockProvider)
            services.Configure<EntityFrameworkOutboxOptions<OwnedDbContext>>(options => options.LockStatementProvider = null!);
        else if (scenario == Scenario.ZeroInboxDelay)
            services.Configure<InboxCleanupServiceOptions<OwnedDbContext>>(options => options.QueryDelay = TimeSpan.Zero);
        else if (scenario == Scenario.ReversedRetry)
            services.Configure<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IOrdersBus, OwnedDbContext>>>(options =>
            {
                options.InitialDeliveryRetryDelay = TimeSpan.FromSeconds(2);
                options.MaximumDeliveryRetryDelay = TimeSpan.FromSeconds(1);
            });
        else
        {
            services.Configure<EntityFrameworkOutboxOptions<UnusedDbContext>>(_ =>
            {
                Interlocked.Increment(ref unusedEf);
                throw new InvalidOperationException("Unselected EF owner was resolved.");
            });
            services.Configure<InboxCleanupServiceOptions<UnusedDbContext>>(_ =>
            {
                Interlocked.Increment(ref unusedInbox);
                throw new InvalidOperationException("Unselected inbox owner was resolved.");
            });
            services.Configure<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IUnusedBus, UnusedDbContext>>>(_ =>
            {
                Interlocked.Increment(ref unusedDelivery);
                throw new InvalidOperationException("Unselected scoped delivery owner was resolved.");
            });
        }
        var probe = new CleanupProbe();
        int probeCreated = 0;
        services.AddSingleton<IHostedService>(_ => { Interlocked.Increment(ref probeCreated); return probe; });
        GenericHost host = builder.Build();
        Task? starting = null;
        Task[] workers = [];
        List<Exception> workerFailures = [];
        Exception? startFailure = null, startJoinFailure = null, messageFailure = null, stopFailure = null;
        Exception? disposalFailure = null, observerFailure = null;
        StartupMessage? actualPrimary = null, actualSecondary = null;
        Lifecycle stoppedDefault = defaultObserver.Snapshot(), stoppedTyped = typedObserver.Snapshot();
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            starting = host.StartAsync(bound.Token);
            try { await starting; }
            catch (Exception failure) { startFailure = failure; }
            if (startFailure is null && scenario == Scenario.Healthy)
            {
                BackgroundService[] selected = host.Services.GetServices<IHostedService>().OfType<BackgroundService>().ToArray();
                // Retain all present real tasks before assertions or message waits; cleanup joins each.
                var observed = new List<Task>();
                foreach (BackgroundService service in selected)
                {
                    if (service.ExecuteTask is { } task) observed.Add(task);
                }
                workers = observed.ToArray();
                Assert.Equal(2, selected.Length);
                Assert.Equal(2, workers.Length);
                Type deliveryType = typeof(ReliableMessagingOptions<IOrdersBus>).Assembly
                    .GetType("ViciOne.ServiceBus.Providers.Persistence.ReliableMessagingDeliveryService`1", throwOnError: true)!
                    .MakeGenericType(typeof(IOrdersBus));
                Assert.Single(selected, service => service.GetType() == typeof(InboxCleanupService<OwnedDbContext>));
                Assert.Single(selected, service => service.GetType() == deliveryType);
                Assert.All(workers, task => Assert.False(task.IsCompleted));
                AssertStarted(defaultObserver.Snapshot(), defaultAddress.Host);
                AssertStarted(typedObserver.Snapshot(), typedAddress.Host);
                try
                {
                    ISendEndpoint primary = await host.Services.GetRequiredService<IBus>()
                        .GetSendEndpointAsync(new Uri(defaultAddress, "input"), bound.Token);
                    ISendEndpoint secondary = await host.Services.GetRequiredService<IOrdersBus>()
                        .GetSendEndpointAsync(new Uri(typedAddress, "input"), bound.Token);
                    await primary.SendAsync(primaryMessage, bound.Token);
                    await secondary.SendAsync(secondaryMessage, bound.Token);
                    actualPrimary = await primaryDelivered.Task.WaitAsync(bound.Token);
                    actualSecondary = await secondaryDelivered.Task.WaitAsync(bound.Token);
                }
                catch (Exception failure) { messageFailure = failure; }
            }
        }
        finally
        {
            bound.Cancel();
            if (starting is not null)
            {
                try { await starting; }
                catch (Exception failure) when (ReferenceEquals(failure, startFailure)) { }
                catch (Exception failure) { startJoinFailure = failure; }
            }
            using var stopBound = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await host.StopAsync(stopBound.Token); }
            catch (Exception failure) { stopFailure = failure; }
            foreach (Task worker in workers)
            {
                try { await worker; }
                catch (OperationCanceledException) when (worker.IsCanceled) { }
                catch (Exception failure) { workerFailures.Add(failure); }
            }
            stoppedDefault = defaultObserver.Snapshot();
            stoppedTyped = typedObserver.Snapshot();
            try
            {
                if (host is IAsyncDisposable disposable) await disposable.DisposeAsync();
                else host.Dispose();
            }
            catch (Exception failure) { disposalFailure = failure; }
            List<Exception> observerFailures = [];
            foreach (ConnectHandle connectionHandle in connections)
            {
                try { await ((IAsyncDisposable)connectionHandle).DisposeAsync(); }
                catch (Exception failure) { observerFailures.Add(failure); }
            }
            if (observerFailures.Count > 0)
                observerFailure = new AggregateException("Observer connection disposal failed.", observerFailures);
        }
        output.WriteLine("ACTUAL_START_FAILURE: " + startFailure);
        output.WriteLine("ACTUAL_START_JOIN_FAILURE: " + startJoinFailure);
        output.WriteLine("ACTUAL_MESSAGE_FAILURE: " + messageFailure);
        output.WriteLine("ACTUAL_STOP_FAILURE: " + stopFailure);
        output.WriteLine("ACTUAL_DISPOSAL_FAILURE: " + disposalFailure);
        output.WriteLine("ACTUAL_OBSERVER_FAILURE: " + observerFailure);
        foreach (Task worker in workers) output.WriteLine("ACTUAL_WORKER_TERMINAL: " + worker.Status);
        foreach (Exception failure in workerFailures) output.WriteLine("ACTUAL_WORKER_FAILURE: " + failure);
        output.WriteLine($"STATE scenario={scenario} defaultCallback={defaultCallbacks} typedCallback={typedCallbacks} unusedEf={unusedEf} unusedInbox={unusedInbox} unusedDelivery={unusedDelivery} defaultConsumed={defaultConsumes} typedConsumed={typedConsumes} probeCreated={probeCreated} probeStart={probe.StartEntries} probeStop={probe.StopEntries} probeDispose={probe.DisposalEntries}");
        output.WriteLine("DEFAULT_LIFECYCLE_AT_HOST_STOP: " + stoppedDefault);
        output.WriteLine("TYPED_LIFECYCLE_AT_HOST_STOP: " + stoppedTyped);
        return new Result(startFailure, startJoinFailure, messageFailure, stopFailure, disposalFailure, observerFailure,
            starting?.IsCompleted == true, workers.Length, workers.All(task => task.IsCompleted), workerFailures.ToArray(), schemaCreated,
            defaultCallbacks, typedCallbacks, defaultConsumes, typedConsumes, unusedEf, unusedInbox, unusedDelivery,
            probeCreated, probe.StartEntries, probe.StopEntries, probe.DisposalEntries, primaryMessage, secondaryMessage,
            actualPrimary, actualSecondary, stoppedDefault, stoppedTyped, defaultAddress.Host, typedAddress.Host);
    }

    sealed class LifecycleObserver : IBusObserver
    {
        IBus? _bus;
        int _created, _preStart, _postStart, _preStop, _postStop, _faults;
        int _readyMatches, _referenceMismatches;
        public void PostCreate(IBus bus) { _bus = bus; Interlocked.Increment(ref _created); }
        public void CreateFaulted(Exception exception) { Interlocked.Increment(ref _faults); }
        public Task PreStartAsync(IBus bus) { Observe(bus); Interlocked.Increment(ref _preStart); return Task.CompletedTask; }
        public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
        {
            Observe(bus);
            BusReady ready = await busReady.ConfigureAwait(false);
            Volatile.Write(ref _readyMatches, ReferenceEquals(bus, ready.Bus) ? 1 : 0);
            Interlocked.Increment(ref _postStart);
        }
        public Task StartFaultedAsync(IBus bus, Exception exception) { Observe(bus); Interlocked.Increment(ref _faults); return Task.CompletedTask; }
        public Task PreStopAsync(IBus bus) { Observe(bus); Interlocked.Increment(ref _preStop); return Task.CompletedTask; }
        public Task PostStopAsync(IBus bus) { Observe(bus); Interlocked.Increment(ref _postStop); return Task.CompletedTask; }
        public Task StopFaultedAsync(IBus bus, Exception exception) { Observe(bus); Interlocked.Increment(ref _faults); return Task.CompletedTask; }
        void Observe(IBus bus)
        {
            if (!ReferenceEquals(_bus, bus)) Interlocked.Increment(ref _referenceMismatches);
        }
        public Lifecycle Snapshot() => new(_bus?.Address.Host, Volatile.Read(ref _created), Volatile.Read(ref _preStart),
            Volatile.Read(ref _postStart), Volatile.Read(ref _preStop), Volatile.Read(ref _postStop), Volatile.Read(ref _faults),
            Volatile.Read(ref _readyMatches) == 1, Volatile.Read(ref _referenceMismatches) == 0);
    }

    sealed class CleanupProbe : IHostedService, IAsyncDisposable
    {
        public int StartEntries { get; private set; }
        public int StopEntries { get; private set; }
        public int DisposalEntries { get; private set; }
        public Task StartAsync(CancellationToken cancellationToken) { StartEntries++; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken) { StopEntries++; return Task.CompletedTask; }
        public ValueTask DisposeAsync() { DisposalEntries++; return ValueTask.CompletedTask; }
    }

    sealed record Lifecycle(string? Host, int Created, int PreStart, int PostStart, int PreStop, int PostStop,
        int Faults, bool ReadyBusMatches, bool AllBusReferencesMatch);
    sealed record Result(Exception? StartFailure, Exception? StartJoinFailure, Exception? MessageFailure, Exception? StopFailure,
        Exception? DisposalFailure, Exception? ObserverFailure, bool StartTerminal, int WorkersObserved, bool WorkersTerminal,
        IReadOnlyList<Exception> WorkerFailures, bool SchemaCreated, int DefaultCallbacks, int TypedCallbacks, int DefaultConsumes,
        int TypedConsumes, int UnusedEf, int UnusedInbox, int UnusedDelivery, int ProbeCreated, int ProbeStart, int ProbeStop, int ProbeDisposed,
        StartupMessage DefaultExpected, StartupMessage TypedExpected, StartupMessage? DefaultDelivered, StartupMessage? TypedDelivered,
        Lifecycle DefaultLifecycle, Lifecycle TypedLifecycle, string DefaultHost, string TypedHost);
    enum Scenario { NullLockProvider, ZeroInboxDelay, ReversedRetry, Healthy }
    public interface IOrdersBus : IBus;
    public interface IUnusedBus : IBus;
    public sealed record StartupMessage(string Owner, string Value);
    public sealed class OwnedDbContext(DbContextOptions<OwnedDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddTransactionalOutboxEntities();
    }
    public sealed class UnusedDbContext : DbContext { }
}
