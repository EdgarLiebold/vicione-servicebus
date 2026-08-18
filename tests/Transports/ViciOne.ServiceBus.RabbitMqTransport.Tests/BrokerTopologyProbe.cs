namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;


    /// <summary>
    /// Reads the exchanges the broker actually holds, so a spec about deploying a publish topology can
    /// assert what was created instead of only that the bus started. The management API is the broker's
    /// own view; asking the bus would only repeat what the bus intended.
    /// </summary>
    public static class BrokerTopologyProbe
    {
        static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

        public static async Task<IReadOnlyCollection<string>> Exchanges(RabbitMqTestHarness harness)
        {
            var virtualHost = harness.HostAddress.AbsolutePath.Trim('/');
            var uri = new UriBuilder("http", harness.HostAddress.Host, ManagementPort,
                $"api/exchanges/{Uri.EscapeDataString(virtualHost)}").Uri;

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var credentials = Encoding.ASCII.GetBytes($"{harness.Username}:{harness.Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            using var response = await Client.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Assert.Fail($"the management API answered {(int)response.StatusCode} {response.ReasonPhrase}, so this "
                    + "probe cannot tell which exchanges the broker holds");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));

            return document.RootElement.EnumerateArray()
                .Select(element => element.GetProperty("name").GetString())
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToArray();
        }

        /// <summary>
        /// How many channels the broker currently holds open for this virtual host. Publishing must not
        /// leak one: the transport keeps a channel per connection and reuses it.
        /// </summary>
        public static async Task<int> ChannelCount(RabbitMqTestHarness harness)
        {
            var virtualHost = harness.HostAddress.AbsolutePath.Trim('/');
            var uri = new UriBuilder("http", harness.HostAddress.Host, ManagementPort,
                $"api/vhosts/{Uri.EscapeDataString(virtualHost)}/channels").Uri;

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var credentials = Encoding.ASCII.GetBytes($"{harness.Username}:{harness.Password}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            using var response = await Client.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                Assert.Fail($"the management API answered {(int)response.StatusCode} {response.ReasonPhrase}, so this "
                    + "probe cannot tell how many channels the broker holds");
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false));

            return document.RootElement.GetArrayLength();
        }

        static int ManagementPort =>
            int.TryParse(Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_RMQ_MGMT_PORT"), out var port)
                ? port
                : throw new InvalidOperationException(
                    "VICIONE_SERVICEBUS_RMQ_MGMT_PORT is not set, so this probe would guess the management port. "
                    + "Start the pinned fixture with tools/ci/run_broker_category.py --broker rabbitmq.");
    }
}
