using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using GenericHost = Microsoft.Extensions.Hosting.IHost;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqGenericHostStartupTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(InvalidNamedOption.Host, "Host must not be empty")]
    [InlineData(InvalidNamedOption.CertificatePassphrase, "CertPassphrase is set without CertPath")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "selected-named-rabbit-options-validated-by-generic-host-before-runtime")]
    public async Task SelectedNamedInvalidOptionsFailBeforeRuntime(InvalidNamedOption invalid, string reason)
    {
        Result result = await RunAsync(invalid);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(IOrdersBus).Name, failure.OptionsName);
        Assert.Equal(invalid == InvalidNamedOption.Host ? typeof(RabbitMqTransportOptions) : typeof(RabbitMqSslOptions), failure.OptionsType);
        Assert.Contains(reason, string.Join(Environment.NewLine, failure.Failures), StringComparison.Ordinal);
        Assert.Contains(typeof(IOrdersBus).FullName!, failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.BarrierEntries);
        Assert.Equal(0, result.OrdersMaterializations);
        Assert.Equal(0, result.BillingMaterializations);
        Assert.True(result.StartTerminal);
        Assert.Empty(result.CleanupFailures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "unselected-default-and-other-named-rabbit-options-cannot-reject-selected-bus")]
    public async Task UnusedInvalidDefaultAndOtherNameCannotRejectSelectedBus()
    {
        Result result = await RunAsync(poisonUnused: true);

        Assert.Same(result.BarrierFailure, result.StartFailure);
        Assert.Equal(1, result.BarrierEntries);
        Assert.Equal(1, result.OrdersMaterializations);
        Assert.Equal(0, result.BillingMaterializations);
        Assert.True(result.StartTerminal);
        Assert.Empty(result.CleanupFailures);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HOST-CONFIGURATION", "invalid-second-selected-rabbit-bus-rejected-before-either-runtime")]
    public async Task InvalidSecondSelectedBusFailsBeforeEitherRuntime()
    {
        Result result = await RunAsync(selectInvalidBilling: true);

        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(result.StartFailure);
        Assert.Equal(typeof(IBillingBus).Name, failure.OptionsName);
        Assert.Equal(typeof(RabbitMqTransportOptions), failure.OptionsType);
        Assert.Contains("Host must not be empty", failure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(IBillingBus).FullName!, failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, result.BarrierEntries);
        Assert.Equal(0, result.OrdersMaterializations);
        Assert.Equal(0, result.BillingMaterializations);
        Assert.True(result.StartTerminal);
        Assert.Empty(result.CleanupFailures);
    }

    async Task<Result> RunAsync(InvalidNamedOption? invalid = null, bool poisonUnused = false, bool selectInvalidBilling = false)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });
        IServiceCollection services = builder.Services;
        services.Configure<RabbitMqTransportOptions>(typeof(IOrdersBus).Name, options =>
        {
            options.Host = invalid == InvalidNamedOption.Host ? " " : "localhost";
            options.User = string.Empty;
            options.Pass = string.Empty;
        });
        services.Configure<RabbitMqSslOptions>(typeof(IOrdersBus).Name, options =>
        {
            if (invalid == InvalidNamedOption.CertificatePassphrase)
                options.CertPassphrase = "local-test-passphrase-without-path";
        });
        if (poisonUnused)
        {
            foreach (string name in new[] { string.Empty, "unselected-other-bus" })
            {
                services.Configure<RabbitMqTransportOptions>(name, options => options.Host = " ");
                services.Configure<RabbitMqSslOptions>(name, options => options.CertPassphrase = "unused-local-test-passphrase");
            }
        }

        int ordersMaterializations = 0;
        int billingMaterializations = 0;
        services.AddViciOneServiceBus<IOrdersBus>(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingRabbitMq((_, _) => Interlocked.Increment(ref ordersMaterializations));
        });
        if (selectInvalidBilling)
        {
            services.Configure<RabbitMqTransportOptions>(typeof(IBillingBus).Name, options => options.Host = " ");
            services.AddViciOneServiceBus<IBillingBus>(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.UsingRabbitMq((_, _) => Interlocked.Increment(ref billingMaterializations));
            });
        }

        ServiceDescriptor[] originalHosted = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
        ServiceDescriptor runtime = Assert.Single(originalHosted, descriptor =>
            descriptor.ImplementationType?.FullName == "ViciOne.ServiceBus.Hosting.ServiceBusHostedService");
        ServiceDescriptor[] composition = originalHosted.Where(descriptor =>
            descriptor.ImplementationType?.Name == "BusCompositionStartupValidator`1").ToArray();
        Assert.Equal(selectInvalidBilling ? 2 : 1, composition.Length);
        int runtimeIndex = services.IndexOf(runtime);
        foreach (ServiceDescriptor descriptor in composition)
            Assert.True(services.IndexOf(descriptor) < runtimeIndex);
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
        output.WriteLine($"STATE barrier={barrier.Entries} orders={ordersMaterializations} billing={billingMaterializations} cleanupFailures={cleanupFailures.Count} startTerminal={starting?.IsCompleted}");
        foreach (Exception failure in cleanupFailures) output.WriteLine("ACTUAL_CLEANUP_FAILURE: " + failure);
        return new Result(startupFailure, barrier.Failure, barrier.Entries, ordersMaterializations,
            billingMaterializations, starting?.IsCompleted == true, cleanupFailures);
    }

    sealed class Barrier : IHostedService
    {
        int _entries;
        public Exception Failure { get; } = new InvalidOperationException("chosen local RabbitMQ pre-runtime startup barrier");
        public int Entries => Volatile.Read(ref _entries);
        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _entries);
            return Task.FromException(Failure);
        }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    sealed record Result(Exception? StartFailure, Exception BarrierFailure, int BarrierEntries,
        int OrdersMaterializations, int BillingMaterializations, bool StartTerminal, IReadOnlyList<Exception> CleanupFailures);

    public enum InvalidNamedOption { Host, CertificatePassphrase }
    public interface IOrdersBus : IBus { }
    public interface IBillingBus : IBus { }
}
