// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Options;
using Monitoring;
using NUnit.Framework;
using TestFramework;
using TestFramework.Messages;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Transports.Components;


/// <summary>
/// The kill switch restart runs on a timer thread. Whatever log context happens to be ambient there belongs to
/// something else, so the restart has to establish the context of its own kill switch as the operation boundary.
/// Only then do the instruments of that restart reach the meter scope of the provider the endpoint belongs to.
/// </summary>
[TestFixture]
public class KillSwitchInstrumentation_Specs
{
    [Test]
    public async Task Should_restart_through_the_meter_scope_of_its_own_kill_switch()
    {
        ILogContext consumeLogContext = null;

        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configurator => configurator.AddHandler(async (PingMessage _) =>
            {
                consumeLogContext = LogContext.Current;
            }))
            .BuildServiceProvider();

        var harness = provider.GetTestHarness();

        await harness.Start();

        // A context that demonstrably belongs to this provider: the one a consumer of this bus runs on.
        await harness.Bus.Publish(new PingMessage());

        Assert.That(await harness.Consumed.Any<PingMessage>(), Is.True);
        Assert.That(consumeLogContext, Is.Not.Null);

        // A foreign context is ambient on the flow that arms the timer, and it flows to the timer thread. The
        // restart must not adopt it.
        LogContext.ConfigureCurrentLogContext(BusTestFixture.LoggerFactory);

        var killSwitch = new RecordingKillSwitch(consumeLogContext);
        var stopped = new StoppedKillSwitchState(killSwitch, new Exception("intentional"));

        var options = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;

        using var ownScope = new MetricCollector<long>(provider.GetRequiredService<IMeterFactory>(), InstrumentationOptions.MeterName,
            options.HandlerTotal);
        using var outsideAnyScope = new MetricCollector<long>(null, InstrumentationOptions.MeterName, options.HandlerTotal);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        stopped.Activate();

        ILogContext restartLogContext = await killSwitch.Restarted.OrCanceled(cancellation.Token);

        Assert.Multiple(() =>
        {
            Assert.That(restartLogContext, Is.SameAs(consumeLogContext),
                "The restart must run on the context of its own kill switch, not on the ambient one");
            Assert.That(ownScope.GetMeasurementSnapshot(), Has.Count.EqualTo(1),
                "The restart must record through the meter scope of its own provider");
            Assert.That(outsideAnyScope.GetMeasurementSnapshot(), Is.Empty,
                "The restart must not record outside the scope of its own provider");
        });
    }


    /// <summary>
    /// Records what the restart entry established, and works through <see cref="LogContext.Current" /> exactly as the
    /// product does on this boundary.
    /// </summary>
    class RecordingKillSwitch :
        IKillSwitch
    {
        readonly TaskCompletionSource<ILogContext> _restarted =
            new TaskCompletionSource<ILogContext>(TaskCreationOptions.RunContinuationsAsynchronously);

        public RecordingKillSwitch(ILogContext logContext)
        {
            LogContext = logContext;
        }

        public Task<ILogContext> Restarted => _restarted.Task;

        public ILogContext LogContext { get; }

        public int ActivationThreshold => 1;
        public int TripThreshold => 1;
        public TimeSpan TrackingPeriod => TimeSpan.FromMinutes(1);
        public TimeSpan RestartTimeout => TimeSpan.FromMilliseconds(10);
        public IExceptionFilter ExceptionFilter => null;

        public void Stop(Exception exception, IKillSwitchState previousState)
        {
        }

        public void Restart(Exception exception, IKillSwitchState previousState)
        {
            ViciOne.ServiceBus.LogContext.Current?.StartHandlerInstrument(new TestConsumeContext<PingMessage>(new PingMessage()),
                Stopwatch.StartNew());

            _restarted.TrySetResult(ViciOne.ServiceBus.LogContext.Current);
        }

        public void Started(IKillSwitchState previousState)
        {
        }
    }
}
