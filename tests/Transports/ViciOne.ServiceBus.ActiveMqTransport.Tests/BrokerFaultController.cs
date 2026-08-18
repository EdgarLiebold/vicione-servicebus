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
    /// Asks the canonical runner to take the broker away and bring it back.
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
    static class BrokerFaultController
    {
        public const string ControlVariable = "VICIONE_SERVICEBUS_FIXTURE_CONTROL";

        /// <summary>The schema both sides write and read. A mismatch is a mismatch, not a guess.</summary>
        public const int SchemaVersion = 1;

        /// <summary>How long the runner is given to establish or undo an outage, plus its own slack.</summary>
        public static readonly TimeSpan Budget = TimeSpan.FromMinutes(3);

        public static bool Available => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ControlVariable));

        /// <summary>Stops the broker, and returns only once the runner has seen it stopped.</summary>
        public static Task Interrupt()
        {
            return Ask("interrupt");
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
            return Ask("restore");
        }

        static async Task Ask(string action)
        {
            var control = Environment.GetEnvironmentVariable(ControlVariable);
            if (string.IsNullOrWhiteSpace(control))
            {
                Assert.Fail($"{ControlVariable} is not set, so this run cannot reach the broker controller. "
                    + "Start the fixture with tools/ci/run_broker_category.py --allow-broker-outage activemq.");
                return;
            }

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
            while (elapsed.Elapsed < Budget)
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
                + $"{Budget.TotalSeconds:0} s");
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
        static Answer Read(string path, string requestId, string action)
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

            return new Answer(status ?? "failed", error);
        }


        readonly record struct Answer(string Status, string? Error);
    }
}
