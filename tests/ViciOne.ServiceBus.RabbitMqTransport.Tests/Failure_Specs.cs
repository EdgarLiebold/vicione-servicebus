// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Internals;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using RabbitMQ.Client.Events;
    using RabbitMQ.Client.Exceptions;
    using ViciOne.ServiceBus.Testing;


    [TestFixture]
    public class Failure_Specs
    {
        /// <summary>
        /// A second bus that claims a bus endpoint queue another connection already holds exclusively
        /// must fail, say why, and stop trying.
        /// <para>
        /// The spec used to assert that harness2.Start() throws. It cannot: the bus endpoint is
        /// materialised on demand, so Start() never declares the exclusive queue and never meets the
        /// conflict. Measured, the queue does not exist in the virtual host after either start, and the
        /// second start returns normally. Upstream MassTransit carries this spec as [Explicit], so the
        /// expectation never ran against a broker there either.
        /// </para>
        /// <para>
        /// Moving the assertion to where the endpoint really comes up was necessary but not sufficient.
        /// Measured again, it was green for the wrong reason: the caller waited 60,0097 s and received
        /// the generic readiness timeout, while the broker had answered
        /// <c>resource_locked</c> four times over and the transport quietly went on retrying a declare
        /// that could not succeed. The assertion below therefore pins the broker's own answer, its
        /// reply code, a budget far below the readiness limit, and the absence of any further attempt.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_properly_fail_on_exclusive_launch()
        {
            var harness1 = NewExclusiveHarness();
            var harness2 = NewExclusiveHarness();

            // A harness left over from an earlier run would still hold the exclusivity and make this
            // pass for the wrong reason.
            await harness1.RecreateVirtualHost();
            await harness1.Start();

            // Materialises the bus endpoint, so its exclusive queue is really held by this connection.
            harness1.SubscribeHandler<TestFramework.Messages.PingMessage>();

            await WaitUntilExclusivelyHeld(harness1);

            await harness2.Start();

            var faults = new FaultCounter();
            using var observer = harness2.Bus.ConnectReceiveEndpointObserver(faults);

            var stopwatch = Stopwatch.StartNew();

            RabbitMqConnectionException exception = null;
            Exception reported = null;

            try
            {
                harness2.SubscribeHandler<TestFramework.Messages.PingMessage>();
            }
            catch (RabbitMqConnectionException specific)
            {
                exception = specific;
            }
            catch (Exception other)
            {
                reported = other;
            }

            stopwatch.Stop();

            // Caught rather than asserted with Throws, so a failure can say what the endpoint reported
            // instead of only what the caller received. The two differ exactly when the conflict is
            // recognised but never delivered, which is the case this spec has to be able to name.
            Assert.That(exception, Is.Not.Null,
                "the second bus did not report the conflict as a RabbitMQ connection failure. What the caller got: "
                + $"{reported?.GetType().Name ?? "nothing"} after {stopwatch.Elapsed}. What the endpoint reported: {faults.Report()}");

            var interrupted = Cause(exception);

            Assert.Multiple(() =>
            {
                Assert.That(interrupted, Is.Not.Null,
                    $"the exception carried no broker cause at all: {exception}");
                Assert.That(interrupted.ShutdownReason?.ReplyCode, Is.EqualTo(405),
                    "the broker cause was not the exclusivity conflict");
                Assert.That(interrupted.ShutdownReason?.ReplyText, Does.Contain("exclusive"),
                    $"the reply text was replaced on the way out: {interrupted.ShutdownReason?.ReplyText}");

                // The readiness limit is sixty seconds. Anything near it would be that limit again
                // rather than the broker's answer, which is what this spec exists to tell apart.
                Assert.That(stopwatch.Elapsed, Is.LessThan(ReportingBudget),
                    $"the conflict took {stopwatch.Elapsed} to surface, so this is the readiness timeout and not the broker's answer");
            });

            // That nothing goes on declaring against a queue belonging to someone else is no longer
            // asserted by watching a fixed window here. A quiet window proves only that window, and it
            // cost every run eight seconds to do it. The rule is now counted exactly, from the broker's
            // own log, by the runner's one-refusal-per-vhost gate, and the classification that makes a
            // retry impossible is asserted directly in the brokerless specs.
        }

        /// <summary>
        /// The other half of the same rule: the attempt ends, the name does not. Once the exclusive
        /// holder is gone, a newly started bus must be able to take the queue — and the attempt that
        /// failed must not have quietly taken it in the meantime.
        /// </summary>
        [Test]
        public async Task Should_start_again_once_the_exclusive_queue_is_released()
        {
            var holder = NewExclusiveHarness();
            var refused = NewExclusiveHarness();

            await holder.RecreateVirtualHost();
            await holder.Start();

            holder.SubscribeHandler<TestFramework.Messages.PingMessage>();

            await WaitUntilExclusivelyHeld(holder);

            await refused.Start();

            var refusedFaults = new FaultCounter();
            using (refused.Bus.ConnectReceiveEndpointObserver(refusedFaults))
            {
                // Caught rather than asserted with Throws, for the same reason as in the spec above: a
                // failure has to be able to say what the endpoint reported, not only what arrived here.
                RabbitMqConnectionException refusal = null;
                Exception other = null;

                try
                {
                    refused.SubscribeHandler<TestFramework.Messages.PingMessage>();
                }
                catch (RabbitMqConnectionException specific)
                {
                    refusal = specific;
                }
                catch (Exception unexpected)
                {
                    other = unexpected;
                }

                Assert.That(refusal, Is.Not.Null,
                    "the precondition of this spec did not hold: the second bus was not refused. What the caller got: "
                    + $"{other?.GetType().Name ?? "nothing"}. What the endpoint reported: {refusedFaults.Report()}");
            }

            // The holder releases the queue.
            await holder.Stop();

            await WaitUntilReleased(successorProbe: refused);

            var successor = NewExclusiveHarness();

            await successor.Start();

            Assert.DoesNotThrow(() => successor.SubscribeHandler<TestFramework.Messages.PingMessage>(),
                "a bus started after the exclusive queue had been released was still refused");
        }

        /// <summary>
        /// Every caller waiting on the same refused endpoint gets the same answer.
        /// <para>
        /// One waiter woken and another left to the safety limit would mean the same failure had two
        /// meanings depending on who asked. Both connects are started before the conflict is reported,
        /// so both are genuinely waiting when it arrives.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_give_every_waiting_caller_the_same_answer()
        {
            var harness1 = NewExclusiveHarness();
            var harness2 = NewExclusiveHarness();

            await harness1.RecreateVirtualHost();
            await harness1.Start();

            harness1.SubscribeHandler<TestFramework.Messages.PingMessage>();

            await WaitUntilExclusivelyHeld(harness1);

            await harness2.Start();

            var stopwatch = Stopwatch.StartNew();

            // Two connects on the same bus, both blocking, both started before either can complete.
            // Dedicated threads rather than pool tasks: connecting blocks its thread by design, and two
            // blocked pool threads in a loaded suite made this spec wait on the scheduler instead of on
            // the bus — a timeout that said nothing about what is being tested.
            var first = Blocking(() => Catch(() => harness2.SubscribeHandler<TestFramework.Messages.PingMessage>()));
            // The second one is a request pipe, not another consumer: both kinds enter the same wait,
            // and a request pipe additionally binds a request id that must not be left behind.
            var second = Blocking(() => Catch(() => harness2.Bus.ConnectRequestPipe(NewId.NextGuid(), new IgnoringPipe())));

            var results = await Task.WhenAll(first, second).OrTimeout(TimeSpan.FromSeconds(45));

            stopwatch.Stop();

            Assert.Multiple(() =>
            {
                foreach (var result in results)
                {
                    Assert.That(result, Is.InstanceOf<RabbitMqConnectionException>(),
                        $"one of two waiting callers got a different answer: {result?.GetType().Name ?? "nothing"}");
                    Assert.That(Cause(result)?.ShutdownReason?.ReplyCode, Is.EqualTo(405),
                        "one of two waiting callers got an answer without the broker's cause");
                }

                Assert.That(stopwatch.Elapsed, Is.LessThan(ReportingBudget),
                    $"two waiting callers took {stopwatch.Elapsed}, so at least one of them waited out the readiness limit");
            });
        }

        /// <summary>A pipe that does nothing. The spec is about connecting, not about consuming.</summary>
        class IgnoringPipe :
            IPipe<ConsumeContext<TestFramework.Messages.PingMessage>>
        {
            public Task Send(ConsumeContext<TestFramework.Messages.PingMessage> context)
            {
                return Task.CompletedTask;
            }

            public void Probe(ProbeContext context)
            {
            }
        }


        /// <summary>
        /// A connect that fails gives the connection back.
        /// <para>
        /// The pipe is connected before the wait, so a wait that throws must undo it. A request pipe
        /// makes the leak observable: its request id stays bound, and connecting the same id again is
        /// refused with a registration error instead of the broker's answer. Without the disconnect the
        /// binding survives for the lifetime of the bus and the id can never be used again.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_give_the_connection_back_when_the_connect_fails()
        {
            var harness1 = NewExclusiveHarness();
            var harness2 = NewExclusiveHarness();

            await harness1.RecreateVirtualHost();
            await harness1.Start();

            harness1.SubscribeHandler<TestFramework.Messages.PingMessage>();

            await WaitUntilExclusivelyHeld(harness1);

            await harness2.Start();

            var requestId = NewId.NextGuid();

            var first = Catch(() => harness2.Bus.ConnectRequestPipe(requestId, new IgnoringPipe()));
            var second = Catch(() => harness2.Bus.ConnectRequestPipe(requestId, new IgnoringPipe()));

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.InstanceOf<RabbitMqConnectionException>(),
                    "the precondition did not hold: the first connect was not refused by the broker");
                Assert.That(second, Is.InstanceOf<RabbitMqConnectionException>(),
                    $"the request id was still bound after the first connect failed, so connecting it again was refused "
                    + $"by the pipe instead of by the broker: {second?.GetType().Name} {second?.Message}");
            });
        }

        /// <summary>Runs a blocking call on a thread of its own, so the scheduler is never the subject.</summary>
        static Task<Exception> Blocking(Func<Exception> call)
        {
            return Task.Factory.StartNew(call, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        static Exception Catch(Action action)
        {
            try
            {
                action();

                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        /// <summary>
        /// A bus that was refused once must not stay refused after it is restarted.
        /// <para>
        /// The terminal failure is remembered for as long as the bus runs, which is what lets a waiting
        /// caller be told why. It must not outlive that bus: the same harness, stopped and started again
        /// after the queue is free, has to take the queue. Record 0047 asks for this as behaviour rather
        /// than as an argument about construction, and it is the reason the observer is released even
        /// when stopping itself fails.
        /// </para>
        /// </summary>
        [Test]
        public async Task Should_not_inherit_the_old_failure_after_a_restart()
        {
            var holder = NewExclusiveHarness();
            var refused = NewExclusiveHarness();

            await holder.RecreateVirtualHost();
            await holder.Start();

            holder.SubscribeHandler<TestFramework.Messages.PingMessage>();

            await WaitUntilExclusivelyHeld(holder);

            await refused.Start();

            Assert.That(Catch(() => refused.SubscribeHandler<TestFramework.Messages.PingMessage>()),
                Is.InstanceOf<RabbitMqConnectionException>(),
                "the precondition did not hold: the second bus was not refused");

            // Same harness: stopped, the queue released, started again.
            await refused.Stop();
            await holder.Stop();

            await WaitUntilReleased(refused);

            await refused.Start();

            Assert.That(Catch(() => refused.SubscribeHandler<TestFramework.Messages.PingMessage>()), Is.Null,
                "a restarted bus inherited the terminal failure of the run before it");
        }

        /// <summary>How long the conflict may take to surface. Far below the sixty second readiness limit.</summary>
        static readonly TimeSpan ReportingBudget = TimeSpan.FromSeconds(20);

        /// <summary>
        /// A virtual host of this fixture's own.
        /// <para>
        /// An exclusively held queue is the one kind of leftover the shared virtual host cannot clean
        /// up: the harness tears a run down by deleting every queue, and a queue another connection
        /// still holds refuses to be deleted. Measured, a single bus left running here failed the
        /// clean up of eighteen later specs with queue.delete answered by RESOURCE_LOCKED. Keeping the
        /// exclusivity to a virtual host nobody else touches removes the interference at the source,
        /// and the teardown below removes the cause as well.
        /// </para>
        /// </summary>
        /// <summary>
        /// One per test method, not one per fixture. Both specs recreate their virtual host to be sure
        /// no leftover holds the queue, and a recreation is a demolition: whatever the other spec had
        /// running in there goes with it. Separate names make the two independent of each other and of
        /// whatever order the runner picks.
        /// </summary>
        string VirtualHost => $"test-exclusive-{TestContext.CurrentContext.Test.MethodName}".ToLowerInvariant().Replace("_", "-");

        readonly List<RabbitMqTestHarness> _harnesses = new();

        /// <summary>
        /// Stops everything this fixture started and removes its virtual host, whatever happened to it.
        /// <para>
        /// Each harness was stopped where it was no longer needed, which is correct until an assertion
        /// fails before that line is reached. Then the bus stays up, keeps its exclusive queue, and the
        /// failure of one spec becomes the failure of every spec after it.
        /// </para>
        /// <para>
        /// A clean up failure never replaces the failure that caused it: if the test has already failed,
        /// its own result stands and the clean up failure is recorded beside it. But on an otherwise
        /// green test the clean up failure is the only failure there is, and writing it to the output
        /// meant a bus that never stopped left a green run behind and took the next specs down instead.
        /// Every clean up runs even after an earlier one throws, so one stubborn harness cannot leave the
        /// rest of them standing.
        /// </para>
        /// </summary>
        [TearDown]
        public async Task StopEverythingStarted()
        {
            var failures = new List<Exception>();

            foreach (var harness in _harnesses)
            {
                try
                {
                    await harness.Stop();
                }
                catch (Exception exception)
                {
                    failures.Add(new InvalidOperationException($"{harness.InputQueueName} did not stop cleanly", exception));
                }
            }

            _harnesses.Clear();

            try
            {
                await DeleteVirtualHost();
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException($"the virtual host '{VirtualHost}' was not removed", exception));
            }

            if (failures.Count == 0)
                return;

            var report = string.Join("; ", failures.Select(x => $"{x.Message}: {x.InnerException?.Message ?? "no detail"}"));

            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                TestContext.Out.WriteLine($"tear down after a failed test, the result above stands: {report}");
                return;
            }

            Assert.Fail($"the test passed but its clean up did not, so the next specs would inherit what it left behind: {report}");
        }

        /// <summary>
        /// Removes this test method's virtual host, and with it every queue, exchange and holder inside
        /// it — including an exclusively held queue, which cannot be deleted on its own while its owner
        /// lives. Absent is a success: the point is that it is gone.
        /// </summary>
        async Task DeleteVirtualHost()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

            var credentials = Encoding.ASCII.GetBytes($"{RunScopedCredentials.User}:{RunScopedCredentials.Pass}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var managementPort = int.TryParse(Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_RMQ_MGMT_PORT"), out var port)
                ? port
                : 15672;

            var requestUri = new UriBuilder("http", RunScopedCredentials.Host, managementPort,
                $"api/vhosts/{Uri.EscapeDataString(VirtualHost)}").Uri;

            var response = await client.DeleteAsync(requestUri);

            if (response.StatusCode != HttpStatusCode.NoContent && response.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException($"the management API answered {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        static Task WaitUntilExclusivelyHeld(RabbitMqTestHarness harness)
        {
            return ExclusiveQueueProbe.WaitUntilHeld(harness, "exclusively-yours");
        }

        static Task WaitUntilReleased(RabbitMqTestHarness successorProbe)
        {
            return ExclusiveQueueProbe.WaitUntilReleased(successorProbe, "exclusively-yours");
        }

        RabbitMqTestHarness NewExclusiveHarness()
        {
            var harness = new RabbitMqTestHarness();

            var address = harness.HostAddress;
            harness.HostAddress = new UriBuilder(address) { Path = $"/{VirtualHost}/" }.Uri;

            harness.OnConfigureRabbitMqBus += configurator =>
            {
                configurator.OverrideDefaultBusEndpointQueueName("exclusively-yours");
                configurator.Exclusive = true;
            };

            _harnesses.Add(harness);

            return harness;
        }

        static OperationInterruptedException Cause(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is OperationInterruptedException interrupted)
                    return interrupted;
            }

            return null;
        }


        /// <summary>
        /// Counts how often the endpoint reports a fault. Each declare attempt that the broker refuses
        /// produces one, so the count is the number of attempts made.
        /// </summary>
        class FaultCounter :
            IReceiveEndpointObserver
        {
            readonly List<string> _seen = new();
            int _count;

            public int Count => Volatile.Read(ref _count);

            public Task Ready(ReceiveEndpointReady ready)
            {
                return Task.CompletedTask;
            }

            public Task Stopping(ReceiveEndpointStopping stopping)
            {
                return Task.CompletedTask;
            }

            public Task Completed(ReceiveEndpointCompleted completed)
            {
                return Task.CompletedTask;
            }

            public Task Faulted(ReceiveEndpointFaulted faulted)
            {
                Interlocked.Increment(ref _count);

                // The whole chain, not just the outermost type. Twice already the conflict arrived in a
                // shape the recognition did not cover — first wrapped, then as AlreadyClosedException,
                // which derives from OperationInterruptedException. A third shape is what this records.
                var chain = new List<string>();
                for (var current = faulted.Exception; current != null; current = current.InnerException)
                {
                    var code = current is RabbitMQ.Client.Exceptions.OperationInterruptedException interrupted
                        ? interrupted.ShutdownReason?.ReplyCode.ToString() ?? "no-reason"
                        : "-";

                    chain.Add($"{current.GetType().Name}(code={code})");
                }

                lock (_seen)
                {
                    _seen.Add(string.Join(" -> ", chain)
                        + $" [transient={(faulted.Exception as ConnectionException)?.IsTransient}]"
                        + $" msg={faulted.Exception.Message.Split('\n')[0]}");
                }

                return Task.CompletedTask;
            }

            /// <summary>What the endpoint actually reported, so a timeout can name its cause.</summary>
            public string Report()
            {
                lock (_seen)
                    return _seen.Count == 0 ? "no endpoint fault was reported at all" : string.Join(" || ", _seen);
            }
        }
    }
}
