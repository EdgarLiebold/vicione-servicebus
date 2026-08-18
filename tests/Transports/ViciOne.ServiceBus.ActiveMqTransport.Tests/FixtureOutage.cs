namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using NUnit.Framework;


    /// <summary>
    /// Asks the canonical runner to take its own broker away and bring it back.
    /// <para>
    /// The test process never touches Docker. It writes a request into the control directory the runner
    /// handed it and waits for the runner's answer, and that answer reports what the runner observed:
    /// that the port really stopped accepting connections, that it accepts them again, and that it is
    /// still the same port. A request whose effect the runner could not confirm comes back as a failure,
    /// so a case can never continue believing an outage that did not happen.
    /// </para>
    /// <para>
    /// The runner pauses the container rather than stopping it, and that is a measured choice rather
    /// than a convenience: stopping and starting rebinds the published port on this fixture, so the
    /// address the bus holds would no longer be the address the broker returns on and nothing could
    /// reconnect to it.
    /// </para>
    /// </summary>
    public static class FixtureOutage
    {
        public const string ControlVariable = "VICIONE_SERVICEBUS_FIXTURE_CONTROL";

        /// <summary>How long the runner is given to establish or undo an outage.</summary>
        public static readonly TimeSpan Budget = TimeSpan.FromSeconds(90);

        public static bool Available => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ControlVariable));

        /// <summary>Takes the broker away, and returns only once the runner has seen it become unreachable.</summary>
        public static Task Interrupt()
        {
            return Ask("interrupt");
        }

        /// <summary>
        /// Brings the broker back, and returns only once the runner has seen it accept again.
        /// <para>
        /// This takes no cancellation token, and that is deliberate. Restoring is the one step that must
        /// happen even when the case has already failed or its own timeout has expired: a broker left
        /// paused is a broker every later fixture in the category blocks against, which turns one red
        /// case into a run that never ends.
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
                Assert.Fail($"{ControlVariable} is not set, so this run cannot ask for an outage. Start the "
                    + "fixture with tools/ci/run_broker_category.py --allow-broker-outage activemq.");
            }

            var id = $"{action}-{Guid.NewGuid():N}";
            var request = Path.Combine(control!, $"{id}.request");
            var result = Path.Combine(control!, $"{id}.result");

            await File.WriteAllTextAsync(request, JsonSerializer.Serialize(new { action }));

            var deadline = DateTime.UtcNow + Budget;
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(result))
                {
                    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(result));

                    if (document.RootElement.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
                        return;

                    var reason = document.RootElement.TryGetProperty("error", out var error)
                        ? error.GetString()
                        : "the runner gave no reason";

                    Assert.Fail($"the runner refused to {action} the broker: {reason}");
                    return;
                }

                await Task.Delay(100);
            }

            Assert.Fail($"the runner did not answer the request to {action} the broker within "
                + $"{Budget.TotalSeconds:0} s");
        }
    }
}
