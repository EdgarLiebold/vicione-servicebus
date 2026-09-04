using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.DependencyInjection.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.ProviderAbstractions;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Outbox;

public sealed class BusOutboxDeliveryTelemetryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-OBSERVABILITY", "background-delivery-uses-own-provider-meter-scope")]
    public async Task BackgroundDelivery_UsesItsOwnProviderMeterScopeAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource<ILogContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var measurements = new ConcurrentQueue<object?>();

        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.SetTestTimeouts(timeout, timeout))
            .AddScoped(_ => new RecordingDbContext(
                new DbContextOptionsBuilder<RecordingDbContext>().Options,
                entered))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        IMeterFactory meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == ServiceBusTelemetry.MeterName
                && ReferenceEquals(instrument.Meter.Scope, meterFactory)
                && instrument.Name == ServiceBusTelemetry.Metrics.OutboxMessages)
                current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) =>
            measurements.Enqueue(instrument.Meter.Scope));
        listener.Start();

        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var service = new BusOutboxDeliveryService<IBus, RecordingDbContext>(
            Options.Create(new OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, RecordingDbContext>>()),
            Options.Create(new EntityFrameworkOutboxOptions<RecordingDbContext>()),
            new NoNotification(),
            [],
            NullLogger<BusOutboxDeliveryService<IBus, RecordingDbContext>>.Instance,
            provider,
            TimeProvider.System,
            BusPersistenceIdentity<IBus>.Create("default"));

        try
        {
            await service.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            ILogContext deliveryLogContext = await entered.Task.WaitAsync(timeout, cancellationToken);

            OutboxTelemetryTestDriver.RecordDelivery(deliveryLogContext);

            Assert.Same(meterFactory, Assert.Single(measurements));
        }
        finally
        {
            await service.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private sealed class RecordingDbContext : DbContext
    {
        public RecordingDbContext(
            DbContextOptions<RecordingDbContext> options,
            TaskCompletionSource<ILogContext> entered)
            : base(options)
        {
            entered.TrySetResult(LogContext.Current
                ?? throw new Xunit.Sdk.XunitException("Expected the outbox delivery log context to be available."));
        }
    }

    private sealed class NoNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, RecordingDbContext>>
    {
        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

        public void Delivered()
        {
        }
    }
}
