using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using GenericHost = Microsoft.Extensions.Hosting.IHost;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class CoreGenericHostStartupTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(InvalidPolicy.ZeroStartTimeout)]
    [InlineData(InvalidPolicy.ConsumerExceedsStop)]
    [RequirementCoverage("REQ-VSB-HOST-OPTIONS-STARTUP", "actual-host-invalid-policy-rejects-start-and-releases-owned-services")]
    public async Task InvalidLifecyclePolicyRejectsStartupAndReleasesOwnedServices(InvalidPolicy policy)
    {
        Result result = await RunAsync(policy);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(string.Empty, failure.OptionsName);
        Assert.Equal(typeof(ViciOneServiceBusHostOptions), failure.OptionsType);
        string reason = policy == InvalidPolicy.ZeroStartTimeout
            ? "StartTimeout (00:00:00) must be greater than 00:00:00"
            : "must be less than or equal to StopTimeout";
        Assert.Single(failure.Failures);
        Assert.Contains(reason, failure.Message, StringComparison.Ordinal);
        Assert.StartsWith("Host lifecycle for bus 'all':", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.BusCallbackEntries);
        Assert.Equal(0, result.ProbeStartEntries);
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Equal(1, result.ProbeStopEntries);
        Assert.Equal(1, result.ProbeDisposalEntries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-OPTIONS-STARTUP", "actual-host-coherent-policy-delivers-and-releases-owned-services")]
    public async Task CoherentPolicyStartsDeliversAndStopsWithUnusedInvalidNamedPolicy()
    {
        Result result = await RunAsync();

        Assert.Null(result.StartFailure);
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.MessageFailure);
        Assert.Equal(result.ExpectedMessage, result.DeliveredMessage);
        Assert.Equal(1, result.BusCallbackEntries);
        Assert.Equal(1, result.ProbeStartEntries);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Equal(1, result.ProbeStopEntries);
        Assert.Equal(1, result.ProbeDisposalEntries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOSTED-LIFECYCLE", "canceled-unstarted-stop-allows-later-real-host-startup")]
    public async Task CanceledStopBeforeStartupRemainsRetryable()
    {
        Result result = await RunAsync(cancelStopBeforeStartup: true);

        OperationCanceledException failure = Assert.IsAssignableFrom<OperationCanceledException>(result.CanceledStopFailure);
        Assert.Equal(result.CanceledStopToken, failure.CancellationToken);
        Assert.Null(result.StartFailure);
        Assert.True(result.StartTerminal);
        Assert.Null(result.StartJoinFailure);
        Assert.Null(result.MessageFailure);
        Assert.Equal(result.ExpectedMessage, result.DeliveredMessage);
        Assert.Equal(1, result.BusCallbackEntries);
        Assert.Equal(1, result.ProbeStartEntries);
        Assert.Null(result.StopFailure);
        Assert.Null(result.DisposalFailure);
        Assert.Equal(1, result.ProbeStopEntries);
        Assert.Equal(1, result.ProbeDisposalEntries);
    }

    async Task<Result> RunAsync(InvalidPolicy? policy = null, bool cancelStopBeforeStartup = false)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });
        IServiceCollection services = builder.Services;
        services.Configure<HostOptions>(options => options.ServicesStartConcurrently = false);
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.Configure<ViciOneServiceBusHostOptions>(options =>
        {
            options.WaitUntilStarted = true;
            options.StartTimeout = policy == InvalidPolicy.ZeroStartTimeout ? TimeSpan.Zero : TimeSpan.FromSeconds(5);
            options.StopTimeout = TimeSpan.FromSeconds(5);
            options.ConsumerStopTimeout = policy == InvalidPolicy.ConsumerExceedsStop
                ? TimeSpan.FromSeconds(6) : TimeSpan.FromSeconds(5);
        });
        services.Configure<ViciOneServiceBusHostOptions>("unselected-policy", options => options.StartTimeout = TimeSpan.Zero);
        var probe = new CleanupProbe();
        // A factory registration gives DI ownership; an instance registration would not prove disposal.
        services.AddSingleton<IHostedService>(_ => probe);
        var delivered = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var address = new Uri($"loopback://core-startup-{NewId.NextGuid():N}/");
        string expectedMessage = "core-host-startup-" + NewId.NextGuid().ToString("N");
        int busCallbackEntries = 0;
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingInMemory((_, configuration) =>
            {
                Interlocked.Increment(ref busCallbackEntries);
                configuration.Host(address);
                configuration.ReceiveEndpoint("input", endpoint => endpoint.Handler<StartupMessage>(context =>
                {
                    delivered.TrySetResult(context.Message.Value);
                    return Task.CompletedTask;
                }));
            });
        });
        ServiceDescriptor[] hosted = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
        ServiceDescriptor runtime = Assert.Single(hosted, descriptor =>
            descriptor.ImplementationType?.FullName == "ViciOne.ServiceBus.Hosting.ServiceBusHostedService");
        ServiceDescriptor composition = Assert.Single(hosted, descriptor =>
            descriptor.ImplementationType?.Name == "BusCompositionStartupValidator`1");
        Assert.Equal(typeof(IBus), Assert.Single(composition.ImplementationType!.GenericTypeArguments));
        Assert.True(Array.IndexOf(hosted, composition) < Array.IndexOf(hosted, runtime));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
        GenericHost host = builder.Build();
        Task? starting = null;
        Exception? canceledStopFailure = null;
        CancellationToken canceledStopToken = default;
        Exception? startFailure = null;
        Exception? startJoinFailure = null;
        Exception? messageFailure = null;
        Exception? stopFailure = null;
        Exception? disposalFailure = null;
        string? deliveredMessage = null;
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            if (cancelStopBeforeStartup)
            {
                IHostedService runtimeService = host.Services.GetServices<IHostedService>().Single(service =>
                    service.GetType().FullName == "ViciOne.ServiceBus.Hosting.ServiceBusHostedService");
                using var canceledStop = new CancellationTokenSource();
                canceledStop.Cancel();
                canceledStopToken = canceledStop.Token;
                try { await runtimeService.StopAsync(canceledStopToken); }
                catch (Exception failure) { canceledStopFailure = failure; }
            }
            starting = host.StartAsync(bound.Token);
            try { await starting; }
            catch (Exception failure) { startFailure = failure; }
            if (startFailure is null)
            {
                try
                {
                    ISendEndpoint endpoint = await host.Services.GetRequiredService<IBus>()
                        .GetSendEndpointAsync(new Uri(address, "input"), bound.Token);
                    await endpoint.SendAsync(new StartupMessage(expectedMessage), bound.Token);
                    deliveredMessage = await delivered.Task.WaitAsync(bound.Token);
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
            try
            {
                if (host is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
                else host.Dispose();
            }
            catch (Exception failure) { disposalFailure = failure; }
        }
        output.WriteLine("ACTUAL_CANCELED_UNSTARTED_STOP_FAILURE: " + canceledStopFailure);
        output.WriteLine("ACTUAL_START_FAILURE: " + startFailure);
        output.WriteLine("ACTUAL_START_JOIN_FAILURE: " + startJoinFailure);
        output.WriteLine("ACTUAL_MESSAGE_FAILURE: " + messageFailure);
        output.WriteLine("ACTUAL_STOP_FAILURE: " + stopFailure);
        output.WriteLine("ACTUAL_DISPOSAL_FAILURE: " + disposalFailure);
        output.WriteLine($"STATE callback={busCallbackEntries} probeStart={probe.StartEntries} probeStop={probe.StopEntries} probeDispose={probe.DisposalEntries} startTerminal={starting?.IsCompleted} delivered={deliveredMessage}");
        return new Result(canceledStopFailure, canceledStopToken, startFailure, startJoinFailure, messageFailure, stopFailure, disposalFailure, starting?.IsCompleted == true,
            busCallbackEntries, probe.StartEntries, probe.StopEntries, probe.DisposalEntries, expectedMessage, deliveredMessage);
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

    sealed record Result(Exception? CanceledStopFailure, CancellationToken CanceledStopToken, Exception? StartFailure, Exception? StartJoinFailure, Exception? MessageFailure, Exception? StopFailure,
        Exception? DisposalFailure, bool StartTerminal, int BusCallbackEntries, int ProbeStartEntries,
        int ProbeStopEntries, int ProbeDisposalEntries, string ExpectedMessage, string? DeliveredMessage);

    public enum InvalidPolicy { ZeroStartTimeout, ConsumerExceedsStop }
    public sealed record StartupMessage(string Value);
}
