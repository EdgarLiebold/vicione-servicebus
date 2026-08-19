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

            Assert.DoesNotThrowAsync(() => BrokerOutageClient.Ask(control.Path, "interrupt", Budget));
        }

        [Test]
        public void Should_fail_when_the_runner_refuses_to_interrupt_the_broker()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) =>
                Answer(id, action, "failed", "docker compose could not stop activemq: no such service"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
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
                () => BrokerOutageClient.Ask(control.Path, "restore", Budget),
                "a restore the runner could not confirm was read as a broker that is back");

            Assert.That(refused!.Message, Does.Contain("120 s after restore"),
                "a restore that did not happen leaves a broker every later case in the run blocks against");
        }

        [Test]
        public void Should_fail_when_the_runner_never_answers()
        {
            using var control = new ControlDirectory();

            var silent = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", TimeSpan.FromMilliseconds(600)),
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
                schemaVersion = BrokerOutageClient.SchemaVersion + 1, requestId = id, action, status = "ok",
                observed = "exited"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
                "an answer from a runner speaking another schema version was read as this one's answer");

            Assert.That(refused!.Message, Does.Contain("schema version"));
        }

        [Test]
        public void Should_refuse_an_answer_about_another_request()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => new
            {
                schemaVersion = BrokerOutageClient.SchemaVersion, requestId = "somebody-else", action,
                status = "ok", observed = "exited"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
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
                schemaVersion = BrokerOutageClient.SchemaVersion, requestId = id, action = "interrupt",
                status = "ok", observed = "exited"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "restore", Budget),
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
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
                "an answer that is not readable JSON was read as a confirmation");

            Assert.That(refused!.Message, Does.Contain("not readable JSON"));
        }

        [Test]
        public void Should_refuse_a_success_that_says_nothing_about_what_was_observed()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => new
            {
                schemaVersion = BrokerOutageClient.SchemaVersion, requestId = id, action, status = "ok"
            });

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
                "a success that observed nothing was read as an interrupt that happened");

            // The wording is not the assurance: an answer with no observed field is refused by the
            // explicit branch and by the accepted-state check behind it, and either sentence is a
            // correct one. What may not happen is that it is accepted.
            Assert.That(refused!.Message, Does.Contain("observ"));
        }

        [TestCase("exited")]
        [TestCase("stopped")]
        [TestCase("absent")]
        public void Should_accept_an_interrupt_the_runner_saw_take_effect(string observed)
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => Answer(id, action, "ok", observed: observed));

            Assert.DoesNotThrowAsync(() => BrokerOutageClient.Ask(control.Path, "interrupt", Budget));
        }

        [Test]
        public void Should_refuse_an_interrupt_that_observed_a_running_broker()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => Answer(id, action, "ok", observed: "running"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "interrupt", Budget),
                "the broker was still running and the interrupt was read as done");

            Assert.That(refused!.Message, Does.Contain("after observing 'running'"));
        }

        [Test]
        public void Should_refuse_a_restore_that_observed_only_a_started_container()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => Answer(id, action, "ok", observed: "running"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "restore", Budget),
                "a container that is running is not yet a broker that answers");

            Assert.That(refused!.Message, Does.Contain("after observing 'running'"));
        }

        [Test]
        public void Should_refuse_a_restore_that_observed_the_state_of_the_opposite_action()
        {
            using var control = new ControlDirectory();
            using var runner = control.Answering((id, action) => Answer(id, action, "ok", observed: "exited"));

            var refused = Assert.ThrowsAsync<AssertionException>(
                () => BrokerOutageClient.Ask(control.Path, "restore", Budget),
                "a broker the runner saw exit was read as a broker that came back");

            Assert.That(refused!.Message, Does.Contain("after observing 'exited'"));
        }

        [Test]
        public void Should_read_a_well_formed_answer_as_the_answer_to_this_request()
        {
            using var control = new ControlDirectory();
            var path = Path.Combine(control.Path, "interrupt-1.result");
            File.WriteAllText(path, JsonSerializer.Serialize(Answer("interrupt-1", "interrupt", "ok")));

            BrokerOutageClient.Answer answer = BrokerOutageClient.Read(path, "interrupt-1", "interrupt");

            Assert.That(answer.Status, Is.EqualTo("ok"));
            Assert.That(answer.Error, Is.Null);
        }

        static TimeSpan Budget => TimeSpan.FromSeconds(20);

        /// <summary>
        /// A runner answer, with the state a real runner reports for that action.
        /// <para>
        /// The observed state is not decoration here. A helper that left it out would let the positive
        /// cases pass against an answer the production protocol has to refuse, and the protocol would
        /// then be weaker than these cases suggest.
        /// </para>
        /// </summary>
        static object Answer(string requestId, string action, string status, string? error = null,
            string? observed = null)
        {
            return new
            {
                schemaVersion = BrokerOutageClient.SchemaVersion, requestId, action, status, error,
                observed = observed ?? (action == "interrupt" ? "exited" : "healthy")
            };
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
}
