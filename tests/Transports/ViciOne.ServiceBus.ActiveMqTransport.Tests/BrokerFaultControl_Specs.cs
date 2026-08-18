#nullable enable
namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;


    /// <summary>
    /// The decisions of the outage boundary, driven directly.
    /// <para>
    /// None of this needs a broker, a container or a fixture, and that is the point. Every case here
    /// was previously only "covered" by the two real recovery cases passing, which proves that the
    /// happy path works and says nothing about what happens when the runner refuses, answers about a
    /// different request, speaks another schema version or does not answer at all. Those are the paths
    /// a run takes when something is already wrong, so they are the ones that must not be inferred.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Asking_the_runner_to_take_the_broker_away
    {
        [Test]
        public void Should_return_when_the_runner_confirms_the_interrupt()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => Answer(id, action, "ok"));

            Assert.DoesNotThrowAsync(() => BrokerFaultController.Ask(control.Path, "interrupt", Budget));
        }

        [Test]
        public void Should_fail_when_the_runner_refuses_to_interrupt_the_broker()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) =>
                Answer(id, action, "failed", "docker compose could not stop activemq: no such service"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "interrupt", Budget),
                "a refusal was read as a confirmed interrupt");

            Assert.That(refused!.Message, Does.Contain("no such service"),
                "a refused interrupt has to carry the runner's own reason, or the case reports that "
                + "the broker was stopped when it never was");
        }

        [Test]
        public void Should_fail_when_the_runner_could_not_start_the_broker_again()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) =>
                Answer(id, action, "failed", "the broker was still 'starting' 120 s after restore"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "restore", Budget),
                "a restore the runner could not confirm was read as a broker that is back");

            Assert.That(refused!.Message, Does.Contain("120 s after restore"),
                "a restore that did not happen leaves a broker every later case in the run blocks against");
        }

        [Test]
        public void Should_fail_when_the_runner_never_answers()
        {
            using var control = new ControlDirectory();

            var silent = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "interrupt", TimeSpan.FromMilliseconds(600)),
                "a controller that never answered was read as a controller that agreed");

            Assert.That(silent!.Message, Does.Contain("did not answer"),
                "a controller that has stopped answering must end the case rather than let it wait");
        }

        [Test]
        public void Should_refuse_an_answer_of_another_schema_version()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => new
            {
                schemaVersion = BrokerFaultController.SchemaVersion + 1, requestId = id, action, status = "ok"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "interrupt", Budget),
                "an answer from a runner speaking another schema version was read as this one's answer");

            Assert.That(refused!.Message, Does.Contain("schema version"));
        }

        [Test]
        public void Should_refuse_an_answer_about_another_request()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => new
            {
                schemaVersion = BrokerFaultController.SchemaVersion, requestId = "somebody-else", action, status = "ok"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "interrupt", Budget),
                "an answer to another request was read as the answer to this one");

            Assert.That(refused!.Message, Does.Contain("request id"),
                "a control directory is a shared surface, so an answer to an earlier request may not "
                + "be read as the answer to this one");
        }

        [Test]
        public void Should_refuse_an_answer_about_the_opposite_action()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => new
            {
                schemaVersion = BrokerFaultController.SchemaVersion, requestId = id, action = "interrupt", status = "ok"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "restore", Budget),
                "a confirmed interrupt was read as a confirmed restore");

            Assert.That(refused!.Message, Does.Contain("is about 'interrupt'"),
                "a confirmed interrupt read as a confirmed restore leaves the broker down while the "
                + "case believes it is back");
        }

        [Test]
        public void Should_refuse_an_answer_that_is_not_readable()
        {
            using var control = new ControlDirectory();
            using var runner = control.AnsweringWith("{ this is not json");

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerFaultController.Ask(control.Path, "interrupt", Budget),
                "an answer that is not readable JSON was read as a confirmation");

            Assert.That(refused!.Message, Does.Contain("not readable JSON"));
        }

        [Test]
        public void Should_read_a_well_formed_answer_as_the_answer_to_this_request()
        {
            using var control = new ControlDirectory();
            var path = Path.Combine(control.Path, "interrupt-1.result");
            File.WriteAllText(path, JsonSerializer.Serialize(Answer("interrupt-1", "interrupt", "ok")));

            BrokerFaultController.Answer answer = BrokerFaultController.Read(path, "interrupt-1", "interrupt");

            Assert.That(answer.Status, Is.EqualTo("ok"));
            Assert.That(answer.Error, Is.Null);
        }

        static TimeSpan Budget => TimeSpan.FromSeconds(20);

        static object Answer(string requestId, string action, string status, string? error = null)
        {
            return new { schemaVersion = BrokerFaultController.SchemaVersion, requestId, action, status, error };
        }


        /// <summary>
        /// A control directory of this case alone, and a runner that answers into it by script.
        /// <para>
        /// The directory is created per case, so two cases can never read each other's answers, and it
        /// is removed afterwards. The scripted runner publishes exactly the way the real one does -
        /// write beside the target, then rename - so a reader can never meet a half written answer.
        /// </para>
        /// </summary>
        sealed class ControlDirectory :
            IDisposable
        {
            public ControlDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    $"vicione-control-{Guid.NewGuid():N}");
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public IDisposable Answering(Func<string, string, object> answer)
            {
                return new ScriptedRunner(Path, (id, action) => JsonSerializer.Serialize(answer(id, action)));
            }

            public IDisposable AnsweringWith(string literal)
            {
                return new ScriptedRunner(Path, (_, _) => literal);
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Path, true);
                }
                catch (DirectoryNotFoundException)
                {
                }
            }
        }


        sealed class ScriptedRunner :
            IDisposable
        {
            readonly CancellationTokenSource _stopping = new();
            readonly Task _serving;

            public ScriptedRunner(string control, Func<string, string, string> answer)
            {
                _serving = Task.Run(() => Serve(control, answer, _stopping.Token));
            }

            static async Task Serve(string control, Func<string, string, string> answer, CancellationToken stopping)
            {
                while (!stopping.IsCancellationRequested)
                {
                    foreach (var request in Directory.GetFiles(control, "*.request"))
                    {
                        var requestId = Path.GetFileNameWithoutExtension(request);
                        var result = Path.Combine(control, $"{requestId}.result");
                        if (File.Exists(result))
                            continue;

                        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(request, stopping));
                        var action = document.RootElement.GetProperty("action").GetString() ?? "";

                        var partial = result + ".partial";
                        await File.WriteAllTextAsync(partial, answer(requestId, action), stopping);
                        File.Move(partial, result);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(20), stopping);
                }
            }

            public void Dispose()
            {
                _stopping.Cancel();
                try
                {
                    _serving.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // The serving loop ends by cancellation; that is how it is stopped, not a failure.
                }

                _stopping.Dispose();
            }
        }
    }


    /// <summary>
    /// When the receive endpoint has recovered, and when it only looks as though it had.
    /// <para>
    /// Recovery is a sequence. The two passing broker cases cannot tell these apart, because in a real
    /// outage the Fault always precedes the Ready: the order that has to be refused never occurs there,
    /// so it was never exercised. It is driven directly here.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Deciding_when_an_endpoint_has_recovered
    {
        [Test]
        public async Task Should_not_accept_a_ready_that_arrived_before_the_fault()
        {
            var observer = new EndpointStateObserver();
            observer.Watch();

            await observer.Ready(new EndpointReady());

            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the endpoint that never went away reported that it was ready, and that was read as a "
                + "recovery of an outage which had not even started");
        }

        [Test]
        public async Task Should_accept_a_ready_that_arrived_after_the_fault()
        {
            var observer = new EndpointStateObserver();
            observer.Watch();

            await observer.Faulted(new EndpointFaulted());
            await observer.Ready(new EndpointReady());

            Assert.That(await observer.Faulted(Budget), Is.True);
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.True);
        }

        [Test]
        public async Task Should_report_nothing_until_it_is_asked_to_watch()
        {
            var observer = new EndpointStateObserver();

            await observer.Faulted(new EndpointFaulted());

            Assert.That(await observer.Faulted(Budget), Is.False,
                "an observer that was never asked to watch an outage reported a fault, so a case could "
                + "read the endpoint's start as the outage it has not caused yet");
        }

        [Test]
        public async Task Should_ignore_everything_that_happened_before_the_watch_began()
        {
            var observer = new EndpointStateObserver();

            await observer.Faulted(new EndpointFaulted());
            await observer.Ready(new EndpointReady());

            observer.Watch();

            Assert.That(await observer.Faulted(Budget), Is.False,
                "a fault from before this outage would let the next Ready complete a recovery that "
                + "nothing caused");
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False);
        }

        [Test]
        public async Task Should_not_carry_a_fault_from_an_earlier_watch_into_the_next_one()
        {
            var observer = new EndpointStateObserver();

            observer.Watch();
            await observer.Faulted(new EndpointFaulted());

            observer.Watch();
            await observer.Ready(new EndpointReady());

            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the fault of the previous outage was still remembered, so the first Ready of the next "
                + "one completed a recovery from a fault that had already been recovered");
        }

        [Test]
        public async Task Should_not_report_a_recovery_when_only_the_fault_was_seen()
        {
            var observer = new EndpointStateObserver();
            observer.Watch();

            await observer.Faulted(new EndpointFaulted());

            Assert.That(await observer.Faulted(Budget), Is.True);
            Assert.That(await observer.ReadyAfterTheFault(Budget), Is.False,
                "the broker never came back, so nothing may report that it did");
        }

        /// <summary>Short on purpose: every case here decides on a signal that is already present.</summary>
        static TimeSpan Budget => TimeSpan.FromMilliseconds(250);


        sealed class EndpointReady :
            ReceiveEndpointReady
        {
            public Uri InputAddress => Address;
            public IReceiveEndpoint ReceiveEndpoint => null!;
            public bool IsStarted => true;
        }


        sealed class EndpointFaulted :
            ReceiveEndpointFaulted
        {
            public Uri InputAddress => Address;
            public IReceiveEndpoint ReceiveEndpoint => null!;
            public Exception? Exception => new InvalidOperationException("the broker was stopped");
        }


        static readonly Uri Address = new("activemq://127.0.0.1/recovery-input");
    }
}
