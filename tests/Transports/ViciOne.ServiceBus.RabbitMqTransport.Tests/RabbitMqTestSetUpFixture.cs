using NUnit.Framework;

[assembly: LevelOfParallelism(1)]


namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus.Testing;


    [SetUpFixture]
    public class RabbitMqTestSetUpFixture
    {
        [OneTimeSetUp]
        public async Task Before_any()
        {
            RequireRunScopedCredentials();

            await CreateVirtualHost("test");
        }

        /// <summary>
        /// The pinned fixture provisions a run-scoped account and has no 'guest' user. Without the
        /// variables the whole suite would fail later with opaque authentication errors, so the
        /// missing configuration is named here instead.
        /// </summary>
        static void RequireRunScopedCredentials()
        {
            foreach (var variable in new[]
                     {
                         RabbitMqTestHarness.UsernameVariable, RabbitMqTestHarness.PasswordVariable,
                         RabbitMqTestHarness.HostVariable, RabbitMqTestHarness.PortVariable,
                         RabbitMqTestHarness.ManagementPortVariable
                     })
            {
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable)))
                {
                    Assert.Fail(
                        $"The run-scoped fixture configuration is incomplete: {variable} is not set. "
                        + "Start the pinned fixture first: "
                        + "python3 tools/ci/run_broker_category.py --broker rabbitmq ...");
                }
            }
        }

        /// <summary>
        /// Creates the virtual host the suite runs against, and fails the run when it cannot.
        /// A failure written to the console and swallowed turns a broken broker set up into a long
        /// cascade of unrelated test failures.
        /// </summary>
        static async Task CreateVirtualHost(string name)
        {
            var harness = new RabbitMqTestHarness();

            using var client = new HttpClient();
            var credentials = Encoding.ASCII.GetBytes($"{harness.Username}:{harness.Password}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            var requestUri = new UriBuilder("http", harness.HostAddress.Host, harness.ManagementPort, $"api/vhosts/{name}").Uri;

            HttpResponseMessage response;
            try
            {
                response = await client.PutAsync(requestUri, new StringContent("{}", Encoding.UTF8, "application/json"));
            }
            catch (Exception exception)
            {
                Assert.Fail(
                    $"The RabbitMQ management API at {requestUri} could not be reached, so the virtual host "
                    + $"'{name}' was not created. The broker fixture is not ready. {exception.Message}");
                return;
            }

            // PUT is idempotent here: 201 on creation, 204 when the virtual host already exists.
            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.Fail(
                    $"Creating the virtual host '{name}' failed with {(int)response.StatusCode} {response.ReasonPhrase}. "
                    + $"Management API: {requestUri}. Response: {body}");
            }
        }
    }
}
