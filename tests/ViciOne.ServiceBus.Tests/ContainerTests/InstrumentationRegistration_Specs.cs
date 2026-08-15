// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Monitoring;
using NUnit.Framework;
using TestFramework;
using TestFramework.Messages;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;


/// <summary>
/// The provider isolated metric path resolves its meter factory from the container. The core therefore registers
/// the standard metric core itself instead of expecting the application bootstrap to have done it, and it does so
/// without replacing a factory the application registered.
/// </summary>
[TestFixture]
public class InstrumentationRegistration_Specs
{
    [Test]
    public async Task Should_instrument_a_bare_container_through_its_own_meter_factory()
    {
        // Deliberately bare: neither AddMetrics nor AddOpenTelemetry is called before the bus is registered.
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        var meterFactory = provider.GetService<IMeterFactory>();

        Assert.That(meterFactory, Is.Not.Null, "The core must register the standard metric core itself");

        var harness = provider.GetTestHarness();

        await harness.Start();

        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        using var collector = new MetricCollector<long>(meterFactory, InstrumentationOptions.MeterName, options.ConsumeTotal);

        await harness.Bus.Publish(new PingMessage());

        Assert.That(await harness.Consumed.Any<PingMessage>(), Is.True);

        await collector.WaitForMeasurementsAsync(1, harness.CancellationToken);

        Assert.That(collector.GetMeasurementSnapshot(), Has.Count.EqualTo(1),
            "The measurement must be observable through the meter factory of this container");
    }

    [Test]
    public async Task Should_keep_a_mixed_process_isolated()
    {
        // The explicit non dependency injection path: a bus built without a container, which is the only way this
        // state may be reached at all.
        // A consumer, not a handler: the handler pipeline feeds HandlerTotal, so only a consumer records into the
        // same instrument the dependency injection side records into and makes the comparison discriminating.
        IBusControl nonDependencyInjectionBus = Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseInstrumentation();

            cfg.ReceiveEndpoint("mixed-process", e => e.Consumer<PingConsumer>());
        });

        // A meter without a scope is the one that path creates, so this collector observes it and nothing else.
        using var nonDependencyInjection = new MetricCollector<long>(null, InstrumentationOptions.MeterName, ConsumeTotalName);

        var providerA = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        // Configured on the same asynchronous flow as the first provider. Each provider owns its root log context,
        // so the second configuration cannot rebind the root instance the first one uses.
        await using var providerB = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        var disposedA = false;
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            await nonDependencyInjectionBus.StartAsync(cancellation.Token);

            var harnessA = providerA.GetTestHarness();
            var harnessB = providerB.GetTestHarness();

            await harnessA.Start();
            await harnessB.Start();

            using MetricCollector<long> collectorA = Collector(providerA);
            using MetricCollector<long> collectorB = Collector(providerB);

            await harnessA.Bus.Publish(new PingMessage());

            Assert.That(await harnessA.Consumed.Any<PingMessage>(), Is.True);
            await collectorA.WaitForMeasurementsAsync(1, harnessA.CancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(collectorA.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
                Assert.That(collectorB.GetMeasurementSnapshot(), Is.Empty, "No provider may observe the other");
                Assert.That(nonDependencyInjection.GetMeasurementSnapshot(), Is.Empty,
                    "A dependency injection provider must never record into the non dependency injection state");
            });

            await providerA.DisposeAsync();
            disposedA = true;

            await harnessB.Bus.Publish(new PingMessage());

            Assert.That(await harnessB.Consumed.Any<PingMessage>(), Is.True);
            await collectorB.WaitForMeasurementsAsync(1, harnessB.CancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(collectorB.GetMeasurementSnapshot(), Has.Count.EqualTo(1),
                    "Disposing one provider must not disable another");
                Assert.That(nonDependencyInjection.GetMeasurementSnapshot(), Is.Empty);
            });

            // The non dependency injection path itself stays functional and records into its own state.
            await nonDependencyInjectionBus.Publish(new PingMessage(), cancellation.Token);

            await nonDependencyInjection.WaitForMeasurementsAsync(1, cancellation.Token);

            Assert.That(nonDependencyInjection.GetMeasurementSnapshot(), Has.Count.EqualTo(1));
        }
        finally
        {
            if (!disposedA)
                await providerA.DisposeAsync();

            await nonDependencyInjectionBus.StopAsync(TestContext.CurrentContext.CancellationToken);
        }
    }

    [Test]
    public async Task Should_not_rebind_a_root_context_that_another_scope_already_uses()
    {
        // A root context the application established itself, with a real logger so that no later configuration
        // silently replaces it. Without that shared starting point two providers could never collide on one
        // instance and the case would prove nothing.
        LogContext.ConfigureCurrentLogContext(BusTestFixture.LoggerFactory);

        ILogContext applicationRoot = LogContext.Current;

        await using var providerA = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        await providerA.GetTestHarness().Start();

        await using var providerB = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) => { }))
            .BuildServiceProvider();

        await providerB.GetTestHarness().Start();

        // Neither provider state is reachable through the instance the application established, so nothing was
        // rebound onto it. The activation itself is not observable from here: LogContext.Current is an AsyncLocal
        // and each provider activates its root inside its own resolution flow.
        LogContext.Current = applicationRoot;

        var handled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        IBusControl busOnApplicationRoot = Bus.Factory.CreateUsingInMemory(cfg =>
            cfg.ReceiveEndpoint("application-root", e => e.Handler<PingMessage>(_ =>
            {
                handled.TrySetResult(true);

                return Task.CompletedTask;
            })));

        using MetricCollector<long> handlerA = HandlerCollector(providerA);
        using MetricCollector<long> handlerB = HandlerCollector(providerB);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await busOnApplicationRoot.StartAsync(cancellation.Token);
        try
        {
            await busOnApplicationRoot.Publish(new PingMessage(), cancellation.Token);

            await handled.Task.OrCanceled(cancellation.Token);

            Assert.Multiple(() =>
            {
                Assert.That(handlerA.GetMeasurementSnapshot(), Is.Empty,
                    "No provider state may be reachable through the instance the application established");
                Assert.That(handlerB.GetMeasurementSnapshot(), Is.Empty,
                    "No provider state may be reachable through the instance the application established");
            });
        }
        finally
        {
            await busOnApplicationRoot.StopAsync(TestContext.CurrentContext.CancellationToken);
        }
    }

    [Test]
    public async Task Should_not_attach_an_unbound_context_to_the_non_dependency_injection_state()
    {
        // The explicit non dependency injection state has to exist for this case, otherwise there would be nothing
        // to reach implicitly and the case would pass for the wrong reason. Configuring the bus creates that state;
        // it is never started, so it cannot record anything of its own.
        Bus.Factory.CreateUsingInMemory(cfg =>
        {
            cfg.UseInstrumentation();

            cfg.ReceiveEndpoint("explicit-state", e => e.Consumer<PingConsumer>());
        });

        // A fresh root context that no configuration ever bound to any instrumentation state.
        LogContext.ConfigureCurrentLogContext(NullLoggerFactory.Instance);

        var handled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        IBusControl unboundBus = Bus.Factory.CreateUsingInMemory(cfg =>
            cfg.ReceiveEndpoint("unbound-context", e => e.Handler<PingMessage>(_ =>
            {
                handled.TrySetResult(true);

                return Task.CompletedTask;
            })));

        // The instrument is recorded before the handler runs, so completion of the handler proves the decision was
        // already taken.
        using var handlerTotal = new MetricCollector<long>(null, InstrumentationOptions.MeterName, HandlerTotalName);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await unboundBus.StartAsync(cancellation.Token);
        try
        {
            await unboundBus.Publish(new PingMessage(), cancellation.Token);

            await handled.Task.OrCanceled(cancellation.Token);

            Assert.That(handlerTotal.GetMeasurementSnapshot(), Is.Empty,
                "An unbound context must not reach the explicit non dependency injection state");
        }
        finally
        {
            await unboundBus.StopAsync(TestContext.CurrentContext.CancellationToken);
        }
    }

    static MetricCollector<long> Collector(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        return new MetricCollector<long>(provider.GetRequiredService<IMeterFactory>(), InstrumentationOptions.MeterName,
            options.ConsumeTotal);
    }

    static MetricCollector<long> HandlerCollector(IServiceProvider provider)
    {
        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        return new MetricCollector<long>(provider.GetRequiredService<IMeterFactory>(), InstrumentationOptions.MeterName,
            options.HandlerTotal);
    }

    /// <summary>
    /// The same default the non dependency injection path applies to its own options.
    /// </summary>
    static readonly string ConsumeTotalName = DefaultInstrumentationOptions().ConsumeTotal;

    static readonly string HandlerTotalName = DefaultInstrumentationOptions().HandlerTotal;

    static InstrumentationOptions DefaultInstrumentationOptions()
    {
        var options = new InstrumentationOptions();

        new ConfigureDefaultInstrumentationOptions().Configure(options);

        return options;
    }

    [Test]
    public void Should_register_exactly_one_meter_factory_for_several_entry_points()
    {
        // Both entry points run through the same central registration, which must stay idempotent.
        IServiceCollection collection = new ServiceCollection()
            .AddViciOneServiceBusTestHarness()
            .AddViciOneServiceBus<IMetricsBus>(configurator => configurator.UsingInMemory((_, bus) =>
                bus.Host(new Uri("loopback://localhost/registration-secondary"))));

        Assert.That(collection.Count(x => x.ServiceType == typeof(IMeterFactory)), Is.EqualTo(1),
            "The standard registration must not add a second meter factory");
    }

    [Test]
    public void Should_not_replace_a_meter_factory_that_the_application_registered()
    {
        using var controlled = new ControlledMeterFactory();

        var provider = new ServiceCollection()
            .AddSingleton<IMeterFactory>(controlled)
            .AddViciOneServiceBusTestHarness()
            .BuildServiceProvider();

        using (provider as IDisposable)
        {
            Assert.That(provider.GetRequiredService<IMeterFactory>(), Is.SameAs(controlled),
                "A meter factory the application registered must survive the bus registration");
        }
    }


    class PingConsumer :
        IConsumer<PingMessage>
    {
        public Task Consume(ConsumeContext<PingMessage> context)
        {
            return Task.CompletedTask;
        }
    }


    class ControlledMeterFactory :
        IMeterFactory
    {
        readonly List<Meter> _meters = new List<Meter>();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version, options.Tags, this);

            _meters.Add(meter);

            return meter;
        }

        public void Dispose()
        {
            foreach (Meter meter in _meters)
                meter.Dispose();

            _meters.Clear();
        }
    }
}
