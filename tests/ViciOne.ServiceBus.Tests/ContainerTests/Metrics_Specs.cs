namespace ViciOne.ServiceBus.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Options;
using Monitoring;
using NUnit.Framework;
using OpenTelemetry.Metrics;
using TestFramework;
using TestFramework.Messages;


[TestFixture]
public class ConsumeMetrics_Specs
{
    [Test]
    public async Task Should_be_able_to_add_custom_tag_to_consume_metrics()
    {
        await using var provider = CreateServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator =>
            {
                configurator.AddHandler(async (PingMessage _) =>
                {
                });

                configurator.AddConfigureEndpointsCallback((_, _, e) => e.UseFilter(new MetricsFilter()));
            })
            .BuildServiceProvider();

        var testHarness = provider.GetTestHarness();

        await testHarness.Start();

        var instrumentationOptions = provider.GetRequiredService<IOptions<InstrumentationOptions>>();

        using MetricCollector<long> collector = GetMetricCollector<long>(provider, instrumentationOptions.Value.ConsumeTotal);

        await testHarness.Bus.Publish(new PingMessage());

        Assert.That(await testHarness.Consumed.Any<PingMessage>(), Is.True);

        // The meter records after the consumer returns, so the harness signal alone does not order the snapshot.
        // Waiting for the expected measurement is the barrier; the token comes from the harness, not from a clock.
        await collector.WaitForMeasurementsAsync(1, testHarness.CancellationToken);

        IReadOnlyList<CollectedMeasurement<long>> metrics = collector.GetMeasurementSnapshot();

        Assert.That(metrics, Has.Count.EqualTo(1));

        foreach (CollectedMeasurement<long> metric in metrics)
            Assert.That(metric.Tags, Contains.Key(TagName));
    }

    [Test]
    public async Task Should_be_able_to_produce_consume_exception_metrics()
    {
        await using var provider = CreateServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => throw new IntentionalTestException()))
            .BuildServiceProvider();

        var testHarness = provider.GetTestHarness();

        await testHarness.Start();

        var instrumentationOptions = provider.GetRequiredService<IOptions<InstrumentationOptions>>();

        using MetricCollector<long> totalCollector = GetMetricCollector<long>(provider, instrumentationOptions.Value.ConsumeTotal);
        using MetricCollector<long> faultCollector = GetMetricCollector<long>(provider, instrumentationOptions.Value.ConsumeFaultTotal);

        await testHarness.Bus.Publish(new PingMessage());

        Assert.That(await testHarness.Consumed.Any<PingMessage>(), Is.True);

        await totalCollector.WaitForMeasurementsAsync(1, testHarness.CancellationToken);
        await faultCollector.WaitForMeasurementsAsync(1, testHarness.CancellationToken);

        IReadOnlyList<CollectedMeasurement<long>> metrics = totalCollector.GetMeasurementSnapshot();
        IReadOnlyList<CollectedMeasurement<long>> faults = faultCollector.GetMeasurementSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(metrics, Has.Count.EqualTo(1));
            Assert.That(faults, Has.Count.EqualTo(1));
        });

        foreach (CollectedMeasurement<long> metric in metrics)
            Assert.That(metric.Value, Is.EqualTo(1));

        foreach (CollectedMeasurement<long> metric in faults)
        {
            Assert.Multiple(() =>
            {
                Assert.That(metric.Value, Is.EqualTo(1));
                Assert.That(metric.Tags, Does.ContainKey(instrumentationOptions.Value.ExceptionTypeLabel).WithValue(nameof(IntentionalTestException)));
            });
        }
    }

    [Test]
    public async Task Should_be_able_to_produce_consume_metrics()
    {
        await using var provider = CreateServiceCollection()
            .AddSingleton(provider => provider.GetTestHarness().GetTask<ConsumeContext<PingMessage>>())
            .AddSingleton(provider => provider.GetTestHarness().GetTask<bool>())
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddConsumer<PingConsumer>())
            .BuildServiceProvider();

        var testHarness = provider.GetTestHarness();
        await testHarness.Start();

        var instrumentationOptions = provider.GetRequiredService<IOptions<InstrumentationOptions>>();

        using MetricCollector<long> totalCollector = GetMetricCollector<long>(provider, instrumentationOptions.Value.ConsumeTotal);
        using MetricCollector<double> durationCollector = GetMetricCollector<double>(provider, instrumentationOptions.Value.ConsumeDuration);
        using MetricCollector<long> inProgressCollector = GetMetricCollector<long>(provider, instrumentationOptions.Value.ConsumerInProgress);

        await testHarness.Bus.Publish(new PingMessage());

        await provider.GetTask<ConsumeContext<PingMessage>>();

        IReadOnlyList<CollectedMeasurement<long>> inProgress = inProgressCollector.GetMeasurementSnapshot();

        foreach (CollectedMeasurement<long> metric in inProgress)
            Assert.That(metric.Value, Is.EqualTo(1));

        var completed = provider.GetRequiredService<TaskCompletionSource<bool>>();
        completed.TrySetResult(true);

        Assert.That(await testHarness.Consumed.Any<PingMessage>(), Is.True);

        // The consume total, the duration and the second in progress measurement are recorded after the consumer
        // returns. Each of them is awaited before the snapshot instead of hoping the harness signal ordered them.
        await totalCollector.WaitForMeasurementsAsync(1, testHarness.CancellationToken);
        await durationCollector.WaitForMeasurementsAsync(1, testHarness.CancellationToken);
        await inProgressCollector.WaitForMeasurementsAsync(2, testHarness.CancellationToken);

        IReadOnlyList<CollectedMeasurement<long>> metrics = totalCollector.GetMeasurementSnapshot();
        inProgress = inProgressCollector.GetMeasurementSnapshot();

        IReadOnlyList<CollectedMeasurement<double>> duration = durationCollector.GetMeasurementSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(metrics, Has.Count.EqualTo(1));
            Assert.That(duration, Has.Count.EqualTo(1));
            Assert.That(inProgress, Has.Count.EqualTo(2));
        });

        foreach (CollectedMeasurement<long> metric in metrics)
            Assert.That(metric.Value, Is.EqualTo(1));

        foreach (CollectedMeasurement<double> metric in duration)
            Assert.That(metric.Value, Is.GreaterThan(0));
    }

    [Test]
    public async Task Should_isolate_metrics_between_service_providers()
    {
        var providerA = CreateServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();
        await using var providerB = CreateServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        var providerADisposed = false;
        try
        {
            var harnessA = providerA.GetTestHarness();
            var harnessB = providerB.GetTestHarness();

            await harnessA.Start();
            await harnessB.Start();

            var optionsA = providerA.GetRequiredService<IOptions<InstrumentationOptions>>().Value;
            var optionsB = providerB.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

            using MetricCollector<long> collectorA = GetMetricCollector<long>(providerA, optionsA.ConsumeTotal);
            using MetricCollector<long> collectorB = GetMetricCollector<long>(providerB, optionsB.ConsumeTotal);
            using MetricCollector<long> sendCollectorA = GetMetricCollector<long>(providerA, optionsA.SendTotal);
            using MetricCollector<long> sendCollectorB = GetMetricCollector<long>(providerB, optionsB.SendTotal);

            await harnessA.Bus.Publish(new PingMessage());

            Assert.That(await harnessA.Consumed.Any<PingMessage>(), Is.True);
            await collectorA.WaitForMeasurementsAsync(1, harnessA.CancellationToken);
            await sendCollectorA.WaitForMeasurementsAsync(1, harnessA.CancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(collectorA.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
                Assert.That(collectorB.GetMeasurementSnapshot(), Is.Empty);
                Assert.That(sendCollectorA.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
                Assert.That(sendCollectorB.GetMeasurementSnapshot(), Is.Empty);
            });

            await providerA.DisposeAsync();
            providerADisposed = true;

            await harnessB.Bus.Publish(new PingMessage());

            Assert.That(await harnessB.Consumed.Any<PingMessage>(), Is.True);
            await collectorB.WaitForMeasurementsAsync(1, harnessB.CancellationToken);
            await sendCollectorB.WaitForMeasurementsAsync(1, harnessB.CancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(collectorB.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
                Assert.That(sendCollectorB.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
            });
        }
        finally
        {
            if (!providerADisposed)
                await providerA.DisposeAsync();
        }
    }

    [Test]
    public async Task Should_collect_metrics_from_multiple_buses_in_the_same_service_provider()
    {
        await using var provider = CreateServiceCollection()
            .AddViciOneServiceBusTestHarness()
            .AddViciOneServiceBus<IMetricsBus>(configurator => configurator.UsingInMemory((_, bus) =>
                bus.Host(new Uri("loopback://localhost/metrics-secondary"))))
            .BuildServiceProvider(true);

        var harness = provider.GetTestHarness();
        await harness.Start();

        var instrumentationOptions = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;
        using MetricCollector<long> collector = GetMetricCollector<long>(provider, instrumentationOptions.SendTotal);

        await harness.Bus.Publish(new PingMessage());
        await provider.GetRequiredService<IMetricsBus>().Publish(new PingMessage());

        await collector.WaitForMeasurementsAsync(2, harness.CancellationToken);

        Assert.That(collector.GetMeasurementSnapshot(), Has.Count.EqualTo(2));
    }

    const string TagName = "custom-metric-name";


    class PingConsumer :
        IConsumer<PingMessage>
    {
        readonly TaskCompletionSource<bool> _completed;
        readonly TaskCompletionSource<ConsumeContext<PingMessage>> _ready;

        public PingConsumer(TaskCompletionSource<ConsumeContext<PingMessage>> ready, TaskCompletionSource<bool> completed)
        {
            _ready = ready;
            _completed = completed;
        }

        public Task Consume(ConsumeContext<PingMessage> context)
        {
            _ready.TrySetResult(context);
            return _completed.Task;
        }
    }


    class MetricsFilter :
        IFilter<ConsumeContext>
    {
        public Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            context.AddMetricTags(TagName, "test");
            return next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
        }
    }


    protected static ServiceCollection CreateServiceCollection()
    {
        var collection = new ServiceCollection();
        collection.AddMetrics();
        collection.AddOpenTelemetry()
            .WithMetrics(builder => builder
                .AddMeter(InstrumentationOptions.MeterName)
                .AddConsoleExporter());
        return collection;
    }

    protected static MetricCollector<T> GetMetricCollector<T>(IServiceProvider provider, string instrumentation)
        where T : struct
    {
        var meterFactory = provider.GetRequiredService<IMeterFactory>();
        return new MetricCollector<T>(meterFactory, InstrumentationOptions.MeterName, instrumentation);
    }
}


/// <summary>
/// A second bus of the same service provider. Declared at namespace scope because the emitted bus instance type
/// derives from BusInstance&lt;TBus&gt; in another assembly, which cannot reference a bus interface nested in a fixture.
/// </summary>
public interface IMetricsBus :
    IBus
{
}
