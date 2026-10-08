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

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class ReliableMessagingGenericHostStartupTests(ITestOutputHelper output)
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STARTUP", "actual-host-default-reliable-nan-jitter")]
    public async Task SelectedDefaultReliableNaNJitterFailsBeforeEndpointStart()
    {
        Result result = await RunAsync(Scenario.DefaultNaNJitter);
        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(ReliableMessagingOptions<IBus>), failure.OptionsType);
        Assert.Equal(string.Empty, failure.OptionsName);
        Assert.Equal(ExpectedDiagnostic("default", "RetryJitterFraction must be between 0 and 0.50."),
            Assert.Single(failure.Failures));
        AssertRejectedBeforeEndpointStart(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STARTUP", "actual-host-typed-reliable-zero-poll")]
    public async Task SelectedTypedReliableZeroPollFailsBeforeEitherEndpointStart()
    {
        Result result = await RunAsync(Scenario.TypedZeroPoll);
        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(ReliableMessagingOptions<IOrdersBus>), failure.OptionsType);
        Assert.Equal(string.Empty, failure.OptionsName);
        string bus = typeof(IOrdersBus).Assembly.GetName().Name + ":" + typeof(IOrdersBus).FullName;
        Assert.Equal(ExpectedDiagnostic(bus, "PollInterval must be positive."), Assert.Single(failure.Failures));
        AssertRejectedBeforeEndpointStart(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DURABLE-STARTUP", "actual-host-plain-default-typed-reliable-unused-options-poison")]
    public async Task PlainDefaultAndTypedReliableBusesDeliverAndStopWithUnusedReliablePoison()
    {
        Result result = await RunAsync(Scenario.HealthyTwoBuses);
        Assert.Null(result.StartFailure);
        Assert.Null(result.MessageFailure);
        Assert.True(result.DeliveryTerminal);
        Assert.Null(result.DeliveryJoinFailure);
        Assert.Equal(result.DefaultExpectedMessage, result.DefaultDeliveredMessage);
        Assert.Equal(result.TypedExpectedMessage, result.TypedDeliveredMessage);
        Assert.NotEqual(result.DefaultExpectedMessage, result.TypedExpectedMessage);
        Assert.Equal(1, result.DefaultConsumeEntries);
        Assert.Equal(1, result.TypedConsumeEntries);
        Assert.Equal(1, result.DefaultCallbackEntries);
        Assert.Equal(1, result.TypedCallbackEntries);
        Assert.Equal(0, result.UnusedConfigureEntries);
        AssertLifecycle(result.DefaultLifecycle, result.DefaultHost);
        AssertLifecycle(result.TypedLifecycle, result.TypedHost);
        Assert.Equal(1, result.ProbeCreatedEntries);
        Assert.Equal(1, result.ProbeStartEntries);
        Assert.Equal(1, result.ProbeStopEntries);
        Assert.Equal(1, result.ProbeDisposalEntries);
        AssertCleanup(result);
    }

    static string ExpectedDiagnostic(string bus, string problem) =>
        $"Reliable messaging for bus '{bus}': Reliable messaging for bus 'unknown': {problem} Correct the named configuration before starting the host. Correct the named value before starting the host.";

    static void AssertRejectedBeforeEndpointStart(Result result)
    {
        Assert.Equal(0, result.DefaultCallbackEntries);
        Assert.Equal(0, result.TypedCallbackEntries);
        Assert.Equal(0, result.DefaultLifecycle.Created);
        Assert.Equal(0, result.TypedLifecycle.Created);
        Assert.Equal(0, result.DefaultLifecycle.PreStart);
        Assert.Equal(0, result.DefaultLifecycle.PostStart);
        Assert.Equal(0, result.TypedLifecycle.PreStart);
        Assert.Equal(0, result.TypedLifecycle.PostStart);
        Assert.InRange(result.ProbeCreatedEntries, 0, 1);
        Assert.Equal(0, result.ProbeStartEntries);
        Assert.Equal(result.ProbeCreatedEntries, result.ProbeStopEntries);
        Assert.Equal(result.ProbeCreatedEntries, result.ProbeDisposalEntries);
        AssertCleanup(result);
    }

    static void AssertCleanup(Result result)
    {
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Null(result.ObserverDisposalFailure);
    }

    static void AssertStarted(Lifecycle result, string expectedHost)
    {
        Assert.Equal(expectedHost, result.Host);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.PreStart);
        Assert.Equal(1, result.PostStart);
        Assert.True(result.ReadyBusMatches);
        Assert.True(result.AllBusReferencesMatch);
    }

    static void AssertLifecycle(Lifecycle result, string expectedHost)
    {
        AssertStarted(result, expectedHost);
        Assert.Equal(1, result.PreStop);
        Assert.Equal(1, result.PostStop);
        Assert.Equal(0, result.Faults);
    }

    async Task<Result> RunAsync(Scenario scenario)
    {
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
        var defaultObserver = new LifecycleObserver();
        var typedObserver = new LifecycleObserver();
        var observerConnections = new List<ConnectHandle>();
        var primaryDelivered = new TaskCompletionSource<StartupMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondaryDelivered = new TaskCompletionSource<StartupMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var defaultAddress = new Uri($"loopback://reliable-startup-default-{NewId.NextGuid():N}/");
        var typedAddress = new Uri($"loopback://reliable-startup-typed-{NewId.NextGuid():N}/");
        var primaryMessage = new StartupMessage("default", NewId.NextGuid().ToString("N"));
        var secondaryMessage = new StartupMessage("typed", NewId.NextGuid().ToString("N"));
        int defaultCallbacks = 0, typedCallbacks = 0, defaultConsumes = 0, typedConsumes = 0, unusedConfigure = 0;
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory((_, configuration) =>
            {
                Interlocked.Increment(ref defaultCallbacks);
                configuration.Host(defaultAddress);
                observerConnections.Add(configuration.ConnectBusObserver(defaultObserver));
                configuration.ReceiveEndpoint("input", endpoint => endpoint.Handler<StartupMessage>(context =>
                {
                    Interlocked.Increment(ref defaultConsumes);
                    primaryDelivered.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            });
            if (scenario == Scenario.DefaultNaNJitter)
                bus.UseReliableMessaging(ConfigureReliable);
        });
        if (scenario != Scenario.DefaultNaNJitter)
        {
            services.AddViciOneServiceBus<IOrdersBus>("vicione.tests.reliable-generic-host.orders", bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingInMemory((_, configuration) =>
                {
                    Interlocked.Increment(ref typedCallbacks);
                    configuration.Host(typedAddress);
                    observerConnections.Add(configuration.ConnectBusObserver(typedObserver));
                    configuration.ReceiveEndpoint("input", endpoint => endpoint.Handler<StartupMessage>(context =>
                    {
                        Interlocked.Increment(ref typedConsumes);
                        secondaryDelivered.TrySetResult(context.Message);
                        return Task.CompletedTask;
                    }));
                });
                bus.UseReliableMessaging(ConfigureReliable);
            });
        }
        if (scenario == Scenario.DefaultNaNJitter)
            services.Configure<ReliableMessagingOptions<IBus>>(options => options.RetryJitterFraction = double.NaN);
        else if (scenario == Scenario.TypedZeroPoll)
            services.Configure<ReliableMessagingOptions<IOrdersBus>>(options => options.PollInterval = TimeSpan.Zero);
        else
            services.Configure<ReliableMessagingOptions<IUnusedBus>>(options =>
            {
                Interlocked.Increment(ref unusedConfigure);
                options.PollInterval = TimeSpan.Zero;
                options.RetryJitterFraction = double.NaN;
            });
        var probe = new CleanupProbe();
        int probeCreated = 0;
        // Last factory: distinguish actual DI materialization from hosted Start/Stop entries.
        services.AddSingleton<IHostedService>(_ =>
        {
            Interlocked.Increment(ref probeCreated);
            return probe;
        });
        GenericHost host = builder.Build();
        Task? starting = null, deliveryTask = null;
        Exception? deliveryJoinFailure = null;
        Exception? startFailure = null, startJoinFailure = null, messageFailure = null, stopFailure = null, disposalFailure = null;
        Exception? observerDisposalFailure = null;
        StartupMessage? actualPrimary = null, actualSecondary = null;
        Lifecycle stoppedDefault = defaultObserver.Snapshot(), stoppedTyped = typedObserver.Snapshot();
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            starting = host.StartAsync(bound.Token);
            try { await starting; }
            catch (Exception failure) { startFailure = failure; }
            if (startFailure is null && scenario == Scenario.HealthyTwoBuses)
            {
                deliveryTask = host.Services.GetServices<IHostedService>().OfType<BackgroundService>().Single().ExecuteTask;
                Assert.NotNull(deliveryTask);
                // Fail before a consume wait if either actual runtime never started; finally still stops/disposes.
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
            if (deliveryTask is not null)
            {
                try { await deliveryTask; }
                catch (OperationCanceledException) when (deliveryTask.IsCanceled) { }
                catch (Exception failure) { deliveryJoinFailure = failure; }
            }
            // Snapshot before disposal: DI disposal cannot manufacture successful hosted Stop observations.
            stoppedDefault = defaultObserver.Snapshot();
            stoppedTyped = typedObserver.Snapshot();
            try
            {
                if (host is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
                else host.Dispose();
            }
            catch (Exception failure) { disposalFailure = failure; }
            List<Exception> observerFailures = [];
            foreach (ConnectHandle connection in observerConnections)
            {
                try { await ((IAsyncDisposable)connection).DisposeAsync(); }
                catch (Exception failure) { observerFailures.Add(failure); }
            }
            if (observerFailures.Count > 0)
                observerDisposalFailure = new AggregateException("Observer connection disposal failed.", observerFailures);
        }
        output.WriteLine("ACTUAL_START_FAILURE: " + startFailure);
        output.WriteLine("ACTUAL_START_JOIN_FAILURE: " + startJoinFailure);
        output.WriteLine("ACTUAL_MESSAGE_FAILURE: " + messageFailure);
        output.WriteLine("ACTUAL_DELIVERY_JOIN_FAILURE: " + deliveryJoinFailure);
        output.WriteLine($"DELIVERY_TASK observed={deliveryTask is not null} terminal={deliveryTask?.IsCompleted == true} canceled={deliveryTask?.IsCanceled == true} faulted={deliveryTask?.IsFaulted == true}");
        output.WriteLine("ACTUAL_STOP_FAILURE: " + stopFailure);
        output.WriteLine("ACTUAL_DISPOSAL_FAILURE: " + disposalFailure);
        output.WriteLine("ACTUAL_OBSERVER_DISPOSAL_FAILURE: " + observerDisposalFailure);
        output.WriteLine($"STATE scenario={scenario} defaultCallback={defaultCallbacks} typedCallback={typedCallbacks} unusedConfigure={unusedConfigure} defaultConsumed={defaultConsumes} typedConsumed={typedConsumes} probeCreated={probeCreated} probeStart={probe.StartEntries} probeStop={probe.StopEntries} probeDispose={probe.DisposalEntries}");
        output.WriteLine("DEFAULT_LIFECYCLE_AT_HOST_STOP: " + stoppedDefault);
        output.WriteLine("TYPED_LIFECYCLE_AT_HOST_STOP: " + stoppedTyped);
        return new Result(startFailure, startJoinFailure, messageFailure, deliveryJoinFailure, deliveryTask?.IsCompleted == true, stopFailure, disposalFailure, observerDisposalFailure, starting?.IsCompleted == true,
            defaultCallbacks, typedCallbacks, defaultConsumes, typedConsumes, unusedConfigure, probeCreated, probe.StartEntries, probe.StopEntries,
            probe.DisposalEntries, primaryMessage, secondaryMessage, actualPrimary, actualSecondary, stoppedDefault, stoppedTyped,
            defaultAddress.Host, typedAddress.Host);
    }

    static void ConfigureReliable(IReliableMessagingConfigurator reliable)
    {
        reliable.UseInMemoryStore();
        reliable.AddMessageContract<StartupMessage>("vicione.tests.reliable-generic-host.startup", 1);
        reliable.Store(new ReliableStoreLimits { MaximumStoredCount = 16, MaximumStoredBytes = 64 * 1024 });
        reliable.Delivery(_ => { });
        reliable.Retention(TimeSpan.FromDays(1));
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
    sealed record Result(Exception? StartFailure, Exception? StartJoinFailure, Exception? MessageFailure, Exception? DeliveryJoinFailure, bool DeliveryTerminal, Exception? StopFailure,
        Exception? DisposalFailure, Exception? ObserverDisposalFailure, bool StartTerminal, int DefaultCallbackEntries, int TypedCallbackEntries,
        int DefaultConsumeEntries, int TypedConsumeEntries, int UnusedConfigureEntries, int ProbeCreatedEntries, int ProbeStartEntries, int ProbeStopEntries,
        int ProbeDisposalEntries, StartupMessage DefaultExpectedMessage, StartupMessage TypedExpectedMessage,
        StartupMessage? DefaultDeliveredMessage, StartupMessage? TypedDeliveredMessage, Lifecycle DefaultLifecycle,
        Lifecycle TypedLifecycle, string DefaultHost, string TypedHost);
    enum Scenario { DefaultNaNJitter, TypedZeroPoll, HealthyTwoBuses }
    public interface IOrdersBus : IBus;
    public interface IUnusedBus : IBus;
    public sealed record StartupMessage(string Owner, string Value);
}
