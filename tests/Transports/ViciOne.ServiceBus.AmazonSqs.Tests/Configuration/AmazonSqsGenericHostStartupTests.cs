using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using GenericHost = Microsoft.Extensions.Hosting.IHost;

namespace ViciOne.ServiceBus.AmazonSqs.Tests.Configuration;

public sealed class AmazonSqsGenericHostStartupTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(InvalidNamedOption.Region, "Region must not be empty")]
    [InlineData(InvalidNamedOption.ScopeWithoutRegion, "Scope requires Region")]
    [InlineData(InvalidNamedOption.Scope, "Scope must not be empty")]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "selected-named-options-validated-by-generic-host-before-runtime")]
    public async Task SelectedNamedInvalidOptionsFailBeforeRuntime(InvalidNamedOption invalid, string reason)
    {
        Result result = await RunAsync(typed: true, selectedInvalid: invalid);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(IOrdersBus).Name, failure.OptionsName);
        Assert.Equal(typeof(AmazonSqsTransportOptions), failure.OptionsType);
        Assert.Contains(reason, string.Join(Environment.NewLine, failure.Failures), StringComparison.Ordinal);
        Assert.Contains(typeof(IOrdersBus).FullName!, failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.BarrierEntries);
        Assert.Equal(0, result.Materializations);
        Assert.Equal(0, result.ClientFactoryCalls);
        Assert.Empty(result.CleanupFailures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "unselected-default-options-do-not-reject-selected-named-sqs")]
    public async Task UnusedInvalidDefaultCannotRejectCoherentNamedBus()
    {
        Result result = await RunAsync(typed: true, poisonDefault: true);

        Assert.Same(result.BarrierFailure, result.StartFailure);
        Assert.Equal(1, result.BarrierEntries);
        Assert.Equal(1, result.Materializations);
        Assert.Equal(0, result.ClientFactoryCalls);
        Assert.Empty(result.CleanupFailures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "selected-default-options-remain-validated-before-runtime")]
    public async Task SelectedInvalidDefaultStillFailsBeforeRuntime()
    {
        Result result = await RunAsync(typed: false, selectedInvalid: InvalidNamedOption.Region);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(string.Empty, failure.OptionsName);
        Assert.Equal(typeof(AmazonSqsTransportOptions), failure.OptionsType);
        Assert.Contains("Region must not be empty", failure.Message, StringComparison.Ordinal);
        Assert.Contains("bus 'default'", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.BarrierEntries);
        Assert.Equal(0, result.Materializations);
        Assert.Equal(0, result.ClientFactoryCalls);
        Assert.Empty(result.CleanupFailures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HOST-CONFIGURATION", "coherent-named-sqs-reaches-selected-pre-runtime-boundary")]
    public async Task CoherentNamedBusReachesExactlyTheSameRuntimeBoundary()
    {
        Result result = await RunAsync(typed: true);

        Assert.Same(result.BarrierFailure, result.StartFailure);
        Assert.Equal(1, result.BarrierEntries);
        Assert.Equal(1, result.Materializations);
        Assert.Equal(0, result.ClientFactoryCalls);
        Assert.Empty(result.CleanupFailures);
    }

    async Task<Result> RunAsync(bool typed, InvalidNamedOption? selectedInvalid = null, bool poisonDefault = false)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });
        IServiceCollection services = builder.Services;
        string selectedName = typed ? typeof(IOrdersBus).Name : string.Empty;
        services.Configure<AmazonSqsTransportOptions>(selectedName, options =>
        {
            options.Region = selectedInvalid switch
            {
                InvalidNamedOption.Region => " ",
                InvalidNamedOption.ScopeWithoutRegion => null,
                _ => "eu-central-1",
            };
            options.Scope = selectedInvalid switch
            {
                InvalidNamedOption.Region => null,
                InvalidNamedOption.Scope => " ",
                _ => "orders",
            };
        });
        if (poisonDefault)
            services.Configure<AmazonSqsTransportOptions>(string.Empty, options => options.Region = " ");
        if (!typed)
            services.Configure<AmazonSqsTransportOptions>(typeof(IOrdersBus).Name, options => options.Region = "eu-central-1");

        int materializations = 0;
        int clientFactoryCalls = 0;
        void ConfigureBus(IBusRegistrationContext _, IAmazonSqsBusFactoryConfigurator configurator)
        {
            Interlocked.Increment(ref materializations);
            configurator.Host("eu-central-1", host => host.ClientFactories(
                () => { Interlocked.Increment(ref clientFactoryCalls); throw new InvalidOperationException("SQS client factory must not run before the runtime boundary"); },
                () => { Interlocked.Increment(ref clientFactoryCalls); throw new InvalidOperationException("SNS client factory must not run before the runtime boundary"); }));
        }
        if (typed)
            services.AddViciOneServiceBus<IOrdersBus>(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingAmazonSqs(ConfigureBus);
            });
        else
            services.AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingAmazonSqs(ConfigureBus);
            });

        ServiceDescriptor[] originalHosted = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
        ServiceDescriptor runtime = Assert.Single(originalHosted, descriptor =>
            descriptor.ImplementationType?.FullName == "ViciOne.ServiceBus.Hosting.ServiceBusHostedService");
        ServiceDescriptor composition = Assert.Single(originalHosted, descriptor =>
            descriptor.ImplementationType?.Name == "BusCompositionStartupValidator`1");
        int runtimeIndex = services.IndexOf(runtime);
        Assert.True(services.IndexOf(composition) < runtimeIndex);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStartupValidator));
        var barrier = new Barrier();
        ServiceDescriptor barrierDescriptor = ServiceDescriptor.Singleton<IHostedService>(barrier);
        services.Insert(runtimeIndex, barrierDescriptor);
        Assert.Equal(originalHosted, services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)
            && !ReferenceEquals(descriptor, barrierDescriptor)).ToArray());

        GenericHost host = builder.Build();
        var cleanupFailures = new List<Exception>();
        Exception? startupFailure = null;
        Task? starting = null;
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        bound.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            starting = host.StartAsync(bound.Token);
            try { await starting; }
            catch (Exception failure) { startupFailure = failure; }
        }
        finally
        {
            bound.Cancel();
            if (starting is not null)
            {
                try { await starting; }
                catch (Exception failure) when (ReferenceEquals(failure, startupFailure)) { }
                catch (Exception failure) { cleanupFailures.Add(failure); }
            }
            using var stopBound = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await host.StopAsync(stopBound.Token); }
            catch (Exception failure) { cleanupFailures.Add(failure); }
            try
            {
                if (host is IAsyncDisposable asyncDisposable) await asyncDisposable.DisposeAsync();
                else host.Dispose();
            }
            catch (Exception failure) { cleanupFailures.Add(failure); }
        }
        output.WriteLine("ACTUAL_STARTUP_FAILURE: " + startupFailure);
        output.WriteLine($"STATE barrier={barrier.Entries} materializations={materializations} clientFactories={clientFactoryCalls} cleanupFailures={cleanupFailures.Count} startTerminal={starting?.IsCompleted}");
        foreach (Exception failure in cleanupFailures) output.WriteLine("ACTUAL_CLEANUP_FAILURE: " + failure);
        return new Result(startupFailure, barrier.Failure, barrier.Entries, materializations, clientFactoryCalls, cleanupFailures);
    }

    sealed class Barrier : IHostedService
    {
        int _entries;
        public Exception Failure { get; } = new InvalidOperationException("chosen local pre-runtime startup barrier");
        public int Entries => Volatile.Read(ref _entries);
        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _entries);
            return Task.FromException(Failure);
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    sealed record Result(Exception? StartFailure, Exception BarrierFailure, int BarrierEntries, int Materializations,
        int ClientFactoryCalls, IReadOnlyList<Exception> CleanupFailures);

    public enum InvalidNamedOption { Region, ScopeWithoutRegion, Scope }
    public interface IOrdersBus : IBus { }
}
