#nullable enable
namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using NUnit.Framework;


    /// <summary>
    /// The test process's client for the outage service of the canonical runner.
    /// <para>
    /// Named for what it does. It does not control the broker: it publishes a request into the control
    /// directory the runner handed it and waits for the runner's answer. The runner owns Docker.
    /// </para>
    /// <para>
    /// The test process never touches Docker. It publishes a request into the control directory the
    /// runner handed it and waits for the runner's answer, and that answer reports what the runner
    /// observed about the broker itself: stopped for an interrupt, and healthy rather than merely
    /// running for a restore. A request whose effect the runner could not confirm comes back failed, so
    /// a case can never continue believing an outage that did not happen.
    /// </para>
    /// <para>
    /// What makes this possible at all is the relay in front of the broker. Compose publishes an
    /// ephemeral loopback port per container start, so a restarted broker returns on a different port
    /// every time; the relay is never restarted, so the addresses this process was given stay valid
    /// while the broker behind them genuinely goes away and comes back.
    /// </para>
    /// </summary>
    static class BrokerOutageClient
    {
        public const string ControlVariable = "VICIONE_SERVICEBUS_FIXTURE_CONTROL";

        /// <summary>The schema both sides write and read. A mismatch is a mismatch, not a guess.</summary>
        public const int SchemaVersion = 1;

        /// <summary>
        /// What the runner has to have observed before an action counts as done.
        /// <para>
        /// An interrupt is done when the container is really gone from the running set; a restore is
        /// done when the fixture's own health check says so, because a container that is running is
        /// not yet a broker that answers. Anything else - a missing field, an unknown state, or the
        /// state of the opposite action - is a failed answer.
        /// </para>
        /// </summary>
        static string[] ObservedFor(string action)
        {
            return action == "interrupt" ? new[] { "exited", "stopped", "absent" } : new[] { "healthy" };
        }

        /// <summary>How long the runner is given to establish or undo an outage, plus its own slack.</summary>
        public static readonly TimeSpan Budget = TimeSpan.FromMinutes(3);

        public static bool Available => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ControlVariable));

        /// <summary>Stops the broker, and returns only once the runner has seen it stopped.</summary>
        public static Task Interrupt()
        {
            return Ask(RequiredControlDirectory(), "interrupt", Budget);
        }

        /// <summary>
        /// Starts the broker again, and returns only once the runner has seen it healthy.
        /// <para>
        /// This takes no cancellation token on purpose. Restoring is the one step that has to happen
        /// even after the case has already failed or run out of its own time: a broker left stopped is
        /// a broker every later fixture in the category blocks against, which turns one red case into a
        /// run that never ends.
        /// </para>
        /// </summary>
        public static Task Restore()
        {
            return Ask(RequiredControlDirectory(), "restore", Budget);
        }

        static string RequiredControlDirectory()
        {
            var control = Environment.GetEnvironmentVariable(ControlVariable);
            if (string.IsNullOrWhiteSpace(control))
            {
                Assert.Fail($"{ControlVariable} is not set, so this run cannot reach the broker controller. "
                    + "Start the fixture with tools/ci/run_broker_category.py --allow-broker-outage activemq.");
            }

            return control!;
        }

        /// <summary>
        /// The exchange itself, with the control directory and the budget handed in.
        /// <para>
        /// Both are parameters so that the decisions in here - a refusal is a failure, an answer about
        /// another request is not an answer, a runner that says nothing runs out of time - are provable
        /// without a broker and without a Docker fixture. The public entry points above bind them to
        /// the run this process was started in.
        /// </para>
        /// </summary>
        internal static async Task Ask(string control, string action, TimeSpan budget)
        {
            var requestId = $"{action}-{Guid.NewGuid():N}";
            var request = Path.Combine(control, $"{requestId}.request");
            var result = Path.Combine(control, $"{requestId}.result");

            // Published by an atomic rename on the same directory, so the runner can never read a
            // request that is still being written.
            var partial = request + ".partial";
            await File.WriteAllTextAsync(partial, JsonSerializer.Serialize(
                new { schemaVersion = SchemaVersion, requestId, action }));
            File.Move(partial, request);

            // Monotonic: this is a duration, and a wall clock that steps would either cut the wait
            // short or extend it past the runner's own budget.
            var elapsed = Stopwatch.StartNew();
            while (elapsed.Elapsed < budget)
            {
                if (File.Exists(result))
                {
                    Answer answer = Read(result, requestId, action);

                    if (answer.Status == "ok")
                        return;

                    Assert.Fail($"the runner refused to {action} the broker: {answer.Error ?? "no reason given"}");
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            Assert.Fail($"the runner did not answer the request to {action} the broker within "
                + $"{budget.TotalSeconds:0} s");
        }

        /// <summary>
        /// Reads the runner's answer, and refuses anything that is not an answer to this request.
        /// <para>
        /// The schema version, the echoed request id and the echoed action are all checked. A control
        /// directory is a shared surface: a stale file from an earlier request, a runner speaking a
        /// different version, or an answer about the opposite action would otherwise be read as
        /// success for this one.
        /// </para>
        /// </summary>
        internal static Answer Read(string path, string requestId, string action)
        {
            JsonElement root;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                root = document.RootElement.Clone();
            }
            catch (JsonException unreadable)
            {
                return new Answer("failed", $"the runner's answer is not readable JSON: {unreadable.Message}");
            }

            var version = root.TryGetProperty("schemaVersion", out var value) && value.TryGetInt32(out var number)
                ? number
                : 0;

            if (version != SchemaVersion)
            {
                return new Answer("failed",
                    $"the runner answered with schema version {version}, this side speaks {SchemaVersion}");
            }

            var echoedId = root.TryGetProperty("requestId", out var identity) ? identity.GetString() : null;
            if (echoedId != requestId)
                return new Answer("failed", $"the answer carries request id '{echoedId}', this request is '{requestId}'");

            var echoedAction = root.TryGetProperty("action", out var performed) ? performed.GetString() : null;
            if (echoedAction != action)
                return new Answer("failed", $"the answer is about '{echoedAction}', this request asked for '{action}'");

            var status = root.TryGetProperty("status", out var reported) ? reported.GetString() : null;
            var error = root.TryGetProperty("error", out var reason) ? reason.GetString() : null;

            if (status != "ok")
                return new Answer(status ?? "failed", error);

            // A success has to say what the runner observed, and it has to be a state that means the
            // action really happened. Reading status alone accepted a syntactically fine answer about a
            // broker that never went away, and the case then went on to call whatever followed a
            // recovery.
            var observed = root.TryGetProperty("observed", out var seen) ? seen.GetString() : null;
            if (string.IsNullOrEmpty(observed))
            {
                return new Answer("failed",
                    $"the runner reported success for '{action}' without saying what it observed");
            }

            string[] accepted = ObservedFor(action);
            if (Array.IndexOf(accepted, observed) < 0)
            {
                return new Answer("failed",
                    $"the runner reported success for '{action}' after observing '{observed}', and that "
                    + $"action is only done when it observes one of: {string.Join(", ", accepted)}");
            }

            return new Answer("ok", null);
        }


        internal readonly record struct Answer(string Status, string? Error);
    }
}
