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
    /// A fixed delay asserts nothing: it claims "the queue is now held" and goes on claiming it on a
    /// machine where the declare takes longer, leaving the spec to fail much later as a timeout that
    /// names no cause. Records 0041 and 0045 require an observable condition with an upper bound, and
    /// this is one - the broker's own answer, polled until it agrees or the bound expires, with an
    /// explicit failure naming the precondition that never came about.
    /// </para>
    /// <para>
    /// The question is exclusivity, which is the precondition the specs need. "Is anyone consuming" is a
    /// different question with a different answer: an exclusive queue refuses a second declare from the
    /// moment it exists, whether or not a consumer has attached, and a non-exclusive queue with a
    /// consumer refuses nothing. What the protocol itself does is asserted directly in
    /// <c>The_broker_contract_for_an_exclusive_queue</c>; this probe only waits for a precondition, and
    /// no proof rests on it.
    /// </para>
    /// <para>
    /// Every answer that is not a statement about the queue is <see cref="QueueState.Unknown"/>, and an
    /// unknown never satisfies a wait. The management plugin answers 500 while a queue is being deleted,
    /// which is exactly the window <see cref="WaitUntilReleased"/> polls through, and a request that
    /// fails outright says nothing about the queue at all. Counting either as "not held" would let the
    /// wait return successfully because the broker was unreachable.
    /// </para>
    /// <para>
    /// Shared by both fixtures that need it, so the two cannot drift apart on what "held" means.
    /// </para>
    /// </summary>
    public static class ExclusiveQueueProbe
    {
        /// <summary>What the broker said about the queue, including that it said nothing usable.</summary>
        public enum QueueState
        {
            Held,
            Released,
            Unknown
        }


        /// <summary>How long a precondition may take to come about.</summary>
        public static readonly TimeSpan Budget = TimeSpan.FromSeconds(30);

        /// <summary>
        /// One client for the whole run. A probe polls every 100 ms for up to 30 s, and a client per poll
        /// leaves a socket in TIME_WAIT for each — hundreds of them over a category run, on the very
        /// machine the timing-sensitive specs are being measured on.
        /// </summary>
        static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task WaitUntilHeld(RabbitMqTestHarness harness, string queueName)
        {
            var last = await Poll(harness, queueName, QueueState.Held).ConfigureAwait(false);
            if (last == QueueState.Held)
                return;

            Assert.Fail(Explain(queueName, last,
                $"the queue was not held exclusively by anyone within {Budget.TotalSeconds:0} s, "
                + "so the precondition of this spec never came about"));
        }

        public static async Task WaitUntilReleased(RabbitMqTestHarness harness, string queueName)
        {
            var last = await Poll(harness, queueName, QueueState.Released).ConfigureAwait(false);
            if (last == QueueState.Released)
                return;

            Assert.Fail(Explain(queueName, last,
                $"the queue was still held exclusively {Budget.TotalSeconds:0} s after its holder stopped"));
        }

        static string Explain(string queueName, QueueState last, string expected)
        {
            return last == QueueState.Unknown
                ? $"the management API never gave a usable answer about '{queueName}' within "
                + $"{Budget.TotalSeconds:0} s, so this probe cannot tell whether the queue is held"
                : $"'{queueName}': {expected}";
        }

        /// <summary>
        /// Polls until the wanted state is reported or the budget expires, and returns the last state the
        /// broker reported. An <see cref="QueueState.Unknown"/> is never the wanted state, so it can only
        /// ever cost time, never turn a wait into a success.
        /// </summary>
        static async Task<QueueState> Poll(RabbitMqTestHarness harness, string queueName, QueueState wanted)
        {
            var deadline = DateTime.UtcNow + Budget;
            var last = QueueState.Unknown;

            while (DateTime.UtcNow < deadline)
            {
                last = await StateOf(harness, queueName).ConfigureAwait(false);
                if (last == wanted)
                    return last;

                await Task.Delay(100).ConfigureAwait(false);
            }

            return last;
        }

        /// <summary>
        /// Whether the broker reports the queue as existing and exclusive to its declaring connection —
        /// the state in which any other connection's declare is refused with reply code 405. An exclusive
        /// queue disappears with the connection that owns it, so its absence is the released state.
        /// </summary>
        static async Task<QueueState> StateOf(RabbitMqTestHarness harness, string queueName)
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
                var body = response.IsSuccessStatusCode
                    ? await response.Content.ReadAsStringAsync().ConfigureAwait(false)
                    : string.Empty;

                return StateFrom(response.StatusCode, body);
            }
            catch (HttpRequestException)
            {
                return QueueState.Unknown;
            }
            catch (TaskCanceledException)
            {
                return QueueState.Unknown;
            }
        }

        /// <summary>
        /// The whole decision, as a function of what the management API answered. Held only on an answer
        /// that says the queue exists and is exclusive; released on the queue being gone or reported
        /// without exclusivity; unknown on anything else, including the 500 the management plugin
        /// answers while a queue is being deleted.
        /// </summary>
        internal static QueueState StateFrom(HttpStatusCode status, string body)
        {
            if (status == HttpStatusCode.NotFound)
                return QueueState.Released;

            if ((int)status < 200 || (int)status > 299)
                return QueueState.Unknown;

            try
            {
                using var document = JsonDocument.Parse(body);

                return document.RootElement.TryGetProperty("exclusive", out var exclusive)
                    && exclusive.ValueKind == JsonValueKind.True
                        ? QueueState.Held
                        : QueueState.Released;
            }
            catch (JsonException)
            {
                return QueueState.Unknown;
            }
        }

        /// <summary>
        /// The management port of the fixture this run started. There is deliberately no default: the
        /// well known 15672 belongs to whatever broker happens to run on this machine, and a probe that
        /// silently addressed it would report about a queue in a different broker.
        /// </summary>
        static int ManagementPort =>
            int.TryParse(Environment.GetEnvironmentVariable(ManagementPortVariable), out var port) && port > 0
                ? port
                : throw new InvalidOperationException(
                    $"{ManagementPortVariable} is not set, so this probe does not know which broker to ask. "
                    + "Start the pinned fixture with tools/ci/run_broker_category.py, which publishes it.");

        internal const string ManagementPortVariable = "VICIONE_SERVICEBUS_RMQ_MGMT_PORT";
    }
}
