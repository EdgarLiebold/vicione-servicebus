// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-12.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Asks the broker whether an exclusive queue is held, instead of assuming it after a sleep.
    /// <para>
    /// A fixed delay asserts nothing. It claimed "the queue is now held" and would go on claiming it on
    /// a machine where the declare took longer, leaving the spec to fail much later as a timeout that
    /// names no cause. Records 0041 and 0045 require an observable condition with an upper bound here,
    /// and this is one: the broker's own answer, polled until it agrees or the bound expires, with an
    /// explicit failure naming the precondition that never came about.
    /// </para>
    /// <para>
    /// It asks about exclusivity, which is the precondition the specs actually need. It used to ask
    /// whether anyone was consuming, and that is a different question with a different answer: an
    /// exclusive queue refuses a second declare from the moment it exists, whether or not a consumer has
    /// attached, and a non-exclusive queue with a consumer refuses nothing at all. The condition was
    /// therefore both too late and, for any other queue, simply wrong. What the protocol itself does is
    /// asserted directly in <c>The_broker_contract_for_an_exclusive_queue</c>; this probe only waits for
    /// a precondition, and no proof rests on it.
    /// </para>
    /// <para>
    /// Shared by both fixtures that need it, so the two cannot drift apart on what "held" means.
    /// </para>
    /// </summary>
    public static class ExclusiveQueueProbe
    {
        /// <summary>How long a precondition may take to come about.</summary>
        public static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

        /// <summary>
        /// One client for the whole run. A probe polls every 100 ms for up to 30 s, and a client per poll
        /// left a socket in TIME_WAIT for each — hundreds of them over a category run, on the very
        /// machine the timing-sensitive specs are being measured on.
        /// </summary>
        static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task WaitUntilHeld(RabbitMqTestHarness harness, string queueName)
        {
            if (await Poll(harness, queueName, held: true).ConfigureAwait(false))
                return;

            Assert.Fail($"the queue '{queueName}' was not held exclusively by anyone within {Budget.TotalSeconds:0} s, "
                + "so the precondition of this spec never came about");
        }

        public static async Task WaitUntilReleased(RabbitMqTestHarness harness, string queueName)
        {
            if (await Poll(harness, queueName, held: false).ConfigureAwait(false))
                return;

            Assert.Fail($"the queue '{queueName}' was still held exclusively {Budget.TotalSeconds:0} s after its holder stopped");
        }

        static async Task<bool> Poll(RabbitMqTestHarness harness, string queueName, bool held)
        {
            var deadline = DateTime.UtcNow + Budget;

            while (DateTime.UtcNow < deadline)
            {
                if (await IsHeldExclusively(harness, queueName).ConfigureAwait(false) == held)
                    return true;

                await Task.Delay(100).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Whether the broker reports the queue as existing and exclusive to its declaring connection —
        /// the state in which any other connection's declare is refused with reply code 405. An exclusive
        /// queue disappears with the connection that owns it, so its absence is the released state.
        /// </summary>
        static async Task<bool> IsHeldExclusively(RabbitMqTestHarness harness, string queueName)
        {
            var virtualHost = harness.HostAddress.AbsolutePath.Trim('/');
            var uri = new UriBuilder("http", harness.HostAddress.Host, ManagementPort,
                $"api/queues/{Uri.EscapeDataString(virtualHost)}/{Uri.EscapeDataString(queueName)}").Uri;

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var credentials = Encoding.ASCII.GetBytes($"{harness.Username}:{harness.Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            try
            {
                using var response = await Client.SendAsync(request).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return false;

                if (!response.IsSuccessStatusCode)
                {
                    Assert.Fail($"the management API answered {(int)response.StatusCode} {response.ReasonPhrase} for "
                        + $"'{queueName}', so this probe cannot tell whether the queue is held");
                }

                using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));

                return document.RootElement.TryGetProperty("exclusive", out var exclusive)
                    && exclusive.ValueKind == JsonValueKind.True;
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        static int ManagementPort =>
            int.TryParse(Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_RMQ_MGMT_PORT"), out var port) ? port : 15672;
    }
}
