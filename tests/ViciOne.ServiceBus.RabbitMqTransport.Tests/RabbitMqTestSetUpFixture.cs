// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
using NUnit.Framework;

[assembly: LevelOfParallelism(1)]


namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
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
            await CreateVirtualHost("test");
        }

        async Task CreateVirtualHost(string name)
        {
            try
            {
                var harness = new RabbitMqTestHarness();

                using var client = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes($"{harness.Username}:{harness.Password}");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var requestUri = new UriBuilder("http", harness.HostAddress.Host, 15672, $"api/vhosts/{name}").Uri;
                await client.PutAsync(requestUri, new StringContent("{}", Encoding.UTF8, "application/json"));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
