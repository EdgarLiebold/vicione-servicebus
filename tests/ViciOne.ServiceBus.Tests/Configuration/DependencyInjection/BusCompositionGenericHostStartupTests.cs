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

public sealed class BusCompositionGenericHostStartupTests(ITestOutputHelper output)
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "actual-host-missing-transport-and-limits")]
    public async Task MissingTransportAndLimitsFailTogetherBeforeRuntime()
    {
        Result result = await RunAsync(Scenario.MissingTransportAndLimits);

        ConfigurationException failure = Assert.IsType<ConfigurationException>(result.StartFailure);
        Assert.Equal(new[]
        {
            "Transport for bus 'default': no transport is selected. Select exactly one transport inside the bus block.",
            "Message limits for bus 'default': MaxBodyBytes is not declared. Call bus.Limits(...) with explicit byte limits."
        }, failure.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(0, result.DefaultCallbackEntries);
        Assert.Equal(0, result.TypedCallbackEntries);
        Assert.Equal(0, result.ProbeStartEntries);
        AssertCleanup(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PAYLOAD-ADMISSION-DI", "actual-host-selected-envelope-limit-not-declared")]
    public async Task SelectedTypedPayloadLimitFailsBeforeMaterialization()
    {
        Result result = await RunAsync(Scenario.MissingTypedEnvelopeLimit);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(string.Empty, failure.OptionsName);
        Assert.Equal(typeof(PayloadAdmissionOptions<IOrdersBus>), failure.OptionsType);
        string bus = typeof(IOrdersBus).Assembly.GetName().Name + ":" + typeof(IOrdersBus).FullName;
        Assert.Equal($"Payload admission for bus '{bus}': MaximumTransportEnvelopeBytes is not declared. Call bus.Limits(...) with an explicit MaxEnvelopeBytes value.",
            Assert.Single(failure.Failures));
        Assert.Equal(0, result.DefaultCallbackEntries);
        Assert.Equal(0, result.TypedCallbackEntries);
        Assert.Equal(0, result.TypedLifecycle.PreStart);
        Assert.Equal(0, result.TypedLifecycle.PostStart);
        Assert.Equal(0, result.ProbeStartEntries);
        AssertCleanup(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-COMPOSITION", "actual-host-default-and-typed-runtime-with-unused-payload-poison")]
    public async Task DefaultAndTypedBusesDeliverAndStopWithUnusedPayloadPoison()
    {
        Result result = await RunAsync(Scenario.HealthyTwoBuses);

        Assert.Null(result.StartFailure);
        Assert.Null(result.MessageFailure);
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
        Assert.Equal(1, result.ProbeStartEntries);
        AssertCleanup(result);
    }

    static void AssertCleanup(Result result)
    {
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Null(result.ObserverDisposalFailure);
        Assert.Equal(1, result.ProbeStopEntries);
        Assert.Equal(1, result.ProbeDisposalEntries);
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
        var defaultAddress = new Uri($"loopback://composition-default-{NewId.NextGuid():N}/");
        var typedAddress = new Uri($"loopback://composition-typed-{NewId.NextGuid():N}/");
        var primaryMessage = new StartupMessage("default", NewId.NextGuid().ToString("N"));
        var secondaryMessage = new StartupMessage("typed", NewId.NextGuid().ToString("N"));
        int defaultCallbacks = 0, typedCallbacks = 0, defaultConsumes = 0, typedConsumes = 0, unusedConfigure = 0;
        if (scenario == Scenario.MissingTransportAndLimits)
            services.AddViciOneServiceBus(_ => { });
        else
        {
            if (scenario == Scenario.HealthyTwoBuses)
            {
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
                });
            }
            services.AddViciOneServiceBus<IOrdersBus>(bus =>
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
            });
            if (scenario == Scenario.MissingTypedEnvelopeLimit)
                services.Configure<PayloadAdmissionOptions<IOrdersBus>>(options => options.MaximumTransportEnvelopeBytes = null);
            else
                services.Configure<PayloadAdmissionOptions<IUnusedBus>>(options =>
                {
                    Interlocked.Increment(ref unusedConfigure);
                    options.MaximumSerializedBodyBytes = null;
                    options.MaximumTransportEnvelopeBytes = null;
                });
        }
        var probe = new CleanupProbe();
        // Append after the real validators/runtime; the factory makes DI own disposal.
        services.AddSingleton<IHostedService>(_ => probe);
        ServiceDescriptor[] hosted = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
        ServiceDescriptor runtime = Assert.Single(hosted, descriptor =>
            descriptor.ImplementationType?.FullName == "ViciOne.ServiceBus.Hosting.ServiceBusHostedService");
        Type[] expectedOwners = scenario switch
        {
            Scenario.MissingTransportAndLimits => [typeof(IBus)],
            Scenario.MissingTypedEnvelopeLimit => [typeof(IOrdersBus)],
            _ => [typeof(IBus), typeof(IOrdersBus)]
        };
        ServiceDescriptor[] composition = hosted.Where(descriptor =>
            descriptor.ImplementationType?.Name == "BusCompositionStartupValidator`1").ToArray();
        Assert.Equal(expectedOwners, composition.Select(descriptor => descriptor.ImplementationType!.GenericTypeArguments[0]).ToArray());
        Assert.All(composition, descriptor => Assert.True(Array.IndexOf(hosted, descriptor) < Array.IndexOf(hosted, runtime)));
        Assert.True(Array.IndexOf(hosted, runtime) < hosted.Length - 1);
        GenericHost host = builder.Build();
        Task? starting = null;
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
        output.WriteLine("ACTUAL_STOP_FAILURE: " + stopFailure);
        output.WriteLine("ACTUAL_DISPOSAL_FAILURE: " + disposalFailure);
        output.WriteLine("ACTUAL_OBSERVER_DISPOSAL_FAILURE: " + observerDisposalFailure);
        output.WriteLine($"STATE scenario={scenario} defaultCallback={defaultCallbacks} typedCallback={typedCallbacks} unusedConfigure={unusedConfigure} defaultConsumed={defaultConsumes} typedConsumed={typedConsumes} probeStart={probe.StartEntries} probeStop={probe.StopEntries} probeDispose={probe.DisposalEntries}");
        output.WriteLine("DEFAULT_LIFECYCLE_AT_HOST_STOP: " + stoppedDefault);
        output.WriteLine("TYPED_LIFECYCLE_AT_HOST_STOP: " + stoppedTyped);
        return new Result(startFailure, startJoinFailure, messageFailure, stopFailure, disposalFailure, observerDisposalFailure, starting?.IsCompleted == true,
            defaultCallbacks, typedCallbacks, defaultConsumes, typedConsumes, unusedConfigure, probe.StartEntries, probe.StopEntries,
            probe.DisposalEntries, primaryMessage, secondaryMessage, actualPrimary, actualSecondary, stoppedDefault, stoppedTyped,
            defaultAddress.Host, typedAddress.Host);
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
        Exception? DisposalFailure, Exception? ObserverDisposalFailure, bool StartTerminal, int DefaultCallbackEntries, int TypedCallbackEntries,
        int DefaultConsumeEntries, int TypedConsumeEntries, int UnusedConfigureEntries, int ProbeStartEntries, int ProbeStopEntries,
        int ProbeDisposalEntries, StartupMessage DefaultExpectedMessage, StartupMessage TypedExpectedMessage,
        StartupMessage? DefaultDeliveredMessage, StartupMessage? TypedDeliveredMessage, Lifecycle DefaultLifecycle,
        Lifecycle TypedLifecycle, string DefaultHost, string TypedHost);
    enum Scenario { MissingTransportAndLimits, MissingTypedEnvelopeLimit, HealthyTwoBuses }
    public interface IOrdersBus : IBus;
    public interface IUnusedBus : IBus;
    public sealed record StartupMessage(string Owner, string Value);
}
