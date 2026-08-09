// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
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
    using RabbitMQ.Client;
    using RabbitMqTransport;
    using RabbitMqTransport.Configuration;
    using Serialization;
    using Transports;


    public class RabbitMqTestHarness :
        BusTestHarness
    {
        Uri _hostAddress;
        Uri _inputQueueAddress;

        /// <summary>
        /// Environment variable carrying the user of the run-scoped broker account.
        /// </summary>
        public const string UsernameVariable = "VICIONE_SERVICEBUS_RMQ_USER";

        /// <summary>
        /// Environment variable carrying the secret of the run-scoped broker account.
        /// </summary>
        public const string PasswordVariable = "VICIONE_SERVICEBUS_RMQ_PASS";

        /// <summary>
        /// Environment variable carrying the host the fixture is reachable on.
        /// </summary>
        public const string HostVariable = "VICIONE_SERVICEBUS_RMQ_HOST";

        /// <summary>
        /// Environment variable carrying the AMQP port Docker bound for this run.
        /// </summary>
        public const string PortVariable = "VICIONE_SERVICEBUS_RMQ_PORT";

        /// <summary>
        /// Environment variable carrying the management API port Docker bound for this run.
        /// </summary>
        public const string ManagementPortVariable = "VICIONE_SERVICEBUS_RMQ_MGMT_PORT";

        const int DefaultManagementPort = 15672;

        /// <summary>
        /// Management API port of the fixture. The canonical runner publishes an ephemeral loopback
        /// port per run so a broker already listening on the developer machine cannot collide, and
        /// passes the port it actually bound through <see cref="ManagementPortVariable" />.
        /// </summary>
        public int ManagementPort { get; set; } = ReadPort(ManagementPortVariable, DefaultManagementPort);

        static int ReadPort(string variable, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return int.TryParse(value, out var port) && port > 0 ? port : fallback;
        }

        static Uri ReadHostAddress()
        {
            var host = Environment.GetEnvironmentVariable(HostVariable);
            if (string.IsNullOrWhiteSpace(host))
                host = "localhost";

            var port = ReadPort(PortVariable, 0);

            return port > 0
                ? new Uri($"rabbitmq://{host}:{port}/test/")
                : new Uri($"rabbitmq://{host}/test/");
        }

        public RabbitMqTestHarness(string inputQueueName = null)
        {
            // The pinned ViciOne fixture provisions a run-scoped account and knows no 'guest'.
            // The historic defaults remain as a fallback so an existing consumer pointing the
            // harness at its own broker keeps working; the ViciOne test run supplies the variables
            // and RabbitMqTestSetUpFixture refuses to start without them.
            Username = Environment.GetEnvironmentVariable(UsernameVariable) ?? "guest";
            Password = Environment.GetEnvironmentVariable(PasswordVariable) ?? "guest";

            InputQueueName = inputQueueName ?? "input_queue";

            NameFormatter = new RabbitMqMessageNameFormatter();

            HostAddress = ReadHostAddress();
        }

        public Uri HostAddress
        {
            get => _hostAddress;
            set
            {
                _hostAddress = value;
                _inputQueueAddress = new Uri($"queue:{InputQueueName}");
            }
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public bool CleanVirtualHost { get; set; } = true;
        public override string InputQueueName { get; }
        public string NodeHostName { get; set; }
        public IMessageNameFormatter NameFormatter { get; }

        public override Uri InputQueueAddress => _inputQueueAddress;

        public event Action<IRabbitMqBusFactoryConfigurator> OnConfigureRabbitMqBus;
        public event Action<IRabbitMqReceiveEndpointConfigurator> OnConfigureRabbitMqReceiveEndpoint;
        public event Action<IRabbitMqHostConfigurator> OnConfigureRabbitMqHost;
        public event Func<IChannel, Task> OnCleanupVirtualHost;

        protected virtual void ConfigureRabbitMqBus(IRabbitMqBusFactoryConfigurator configurator)
        {
            OnConfigureRabbitMqBus?.Invoke(configurator);
        }

        protected virtual void ConfigureRabbitMqReceiveEndpoint(IRabbitMqReceiveEndpointConfigurator configurator)
        {
            OnConfigureRabbitMqReceiveEndpoint?.Invoke(configurator);
        }

        protected virtual void ConfigureRabbitMqHost(IRabbitMqHostConfigurator configurator)
        {
            OnConfigureRabbitMqHost?.Invoke(configurator);
        }

        protected virtual Task CleanupVirtualHost(IChannel channel)
        {
            return OnCleanupVirtualHost != null ? OnCleanupVirtualHost(channel) : Task.CompletedTask;
        }

        protected virtual void ConfigureHost(IRabbitMqBusFactoryConfigurator configurator)
        {
            configurator.Host(HostAddress, h =>
            {
                ConfigureHostSettings(h);
            });
        }

        public RabbitMqHostSettings GetHostSettings()
        {
            var host = new RabbitMqHostConfigurator(HostAddress);

            ConfigureHostSettings(host);

            return host.Settings;
        }

        public override async Task Clean()
        {
            var settings = GetHostSettings();

            var connectionFactory = settings.GetConnectionFactory();

            await using var connection = settings.EndpointResolver != null
                ? await connectionFactory.CreateConnectionAsync(settings.EndpointResolver, settings.Host)
                : await connectionFactory.CreateConnectionAsync();

            await using var channel = await connection.CreateChannelAsync();

            IList<string> exchanges = await GetVirtualHostEntities("exchanges").ConfigureAwait(false);
            foreach (var exchange in exchanges)
                await channel.ExchangeDeleteAsync(exchange);

            IList<string> queues = await GetVirtualHostEntities("queues").ConfigureAwait(false);
            foreach (var queue in queues)
                await channel.QueueDeleteAsync(queue);

            await channel.CloseAsync();

            CleanVirtualHost = false;
        }

        /// <summary>
        /// Drops the virtual host and creates it again, which is the only reset that is guaranteed to
        /// be complete.
        /// <para>
        /// <see cref="Clean" /> enumerates exchanges and queues and deletes them one by one. That
        /// leaves behind anything a plugin keeps outside those two entity types — most notably the
        /// scheduled message store of the delayed message exchange. Recreating the virtual host
        /// removes that store with it, because the store belongs to the virtual host.
        /// </para>
        /// <para>
        /// The account that creates a virtual host over the management API receives full permissions
        /// on it, so no separate permission call is needed.
        /// </para>
        /// </summary>
        public async Task RecreateVirtualHost()
        {
            var virtualHost = HostAddress.AbsolutePath.Trim('/');
            if (string.IsNullOrWhiteSpace(virtualHost) || virtualHost == "/")
            {
                throw new InvalidOperationException(
                    "Refusing to recreate the root virtual host. The test fixture must run against a dedicated virtual host.");
            }

            using var client = new HttpClient();
            var credentials = Encoding.ASCII.GetBytes($"{Username}:{Password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

            // A cluster fixture addresses the broker through a logical host name that only the
            // cluster endpoint resolver understands. The management API has to be reached on the real
            // node instead, which NodeHostName carries in that case.
            var managementHost = HostAddress.Host;
            var managementPort = ManagementPort;
            if (!string.IsNullOrWhiteSpace(NodeHostName))
            {
                var separator = NodeHostName.LastIndexOf(':');
                managementHost = separator > 0 ? NodeHostName.Substring(0, separator) : NodeHostName;
            }

            var requestUri = new UriBuilder("http", managementHost, managementPort, $"api/vhosts/{virtualHost}").Uri;

            var delete = await client.DeleteAsync(requestUri).ConfigureAwait(false);
            if (delete.StatusCode != HttpStatusCode.NoContent && delete.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Deleting the virtual host '{virtualHost}' failed with {(int)delete.StatusCode} {delete.ReasonPhrase}.");
            }

            var create = await client.PutAsync(requestUri, new StringContent("{}", Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (create.StatusCode != HttpStatusCode.Created && create.StatusCode != HttpStatusCode.NoContent)
            {
                throw new InvalidOperationException(
                    $"Creating the virtual host '{virtualHost}' failed with {(int)create.StatusCode} {create.ReasonPhrase}.");
            }

            CleanVirtualHost = false;
        }

        async Task<IList<string>> GetVirtualHostEntities(string element)
        {
            using var client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes($"{Username}:{Password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var requestUri = new UriBuilder("http", HostAddress.Host, ManagementPort, $"api/{element}/{HostAddress.AbsolutePath.Trim('/')}").Uri;

            var bytes = await client.GetByteArrayAsync(requestUri);

            var rootElement = JsonSerializer.Deserialize<JsonElement>(bytes, SystemTextJsonMessageSerializer.Options);

            var entities = rootElement.EnumerateArray().Select(x => x.GetProperty("name").GetString()).ToArray();

            return entities.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("amq.")).ToList();
        }

        protected override async Task<IBusControl> CreateBus()
        {
            var busControl = ViciOne.ServiceBus.Bus.Factory.CreateUsingRabbitMq(x =>
            {
                ConfigureHost(x);

                ConfigureBus(x);

                ConfigureRabbitMqBus(x);

                x.ReceiveEndpoint(InputQueueName, e =>
                {
                    e.PrefetchCount = 16;
                    e.PurgeOnStartup = true;

                    ConfigureReceiveEndpoint(e);

                    ConfigureRabbitMqReceiveEndpoint(e);

                    _inputQueueAddress = e.InputAddress;
                });
            });

            if (CleanVirtualHost)
                await CleanUpVirtualHost();

            return busControl;
        }

        void ConfigureHostSettings(IRabbitMqHostConfigurator configurator)
        {
            configurator.Username(Username);
            configurator.Password(Password);

            if (!string.IsNullOrWhiteSpace(NodeHostName))
                configurator.UseCluster(c => c.Node(NodeHostName));

            ConfigureRabbitMqHost(configurator);
        }

        async Task CleanUpVirtualHost()
        {
            try
            {
                var settings = GetHostSettings();

                var connectionFactory = settings.GetConnectionFactory();

                await using var connection = settings.EndpointResolver != null
                    ? await connectionFactory.CreateConnectionAsync(settings.EndpointResolver, settings.Host)
                    : await connectionFactory.CreateConnectionAsync();

                await using var channel = await connection.CreateChannelAsync();

                await channel.ExchangeDeleteAsync("input_queue");
                await channel.QueueDeleteAsync("input_queue");

                await channel.ExchangeDeleteAsync("input_queue_skipped");
                await channel.QueueDeleteAsync("input_queue_skipped");

                await channel.ExchangeDeleteAsync("input_queue_error");
                await channel.QueueDeleteAsync("input_queue_error");

                await channel.ExchangeDeleteAsync("input_queue_delay");

                if (InputQueueName != "input_queue")
                {
                    await channel.ExchangeDeleteAsync(InputQueueName);
                    await channel.QueueDeleteAsync(InputQueueName);

                    await channel.ExchangeDeleteAsync(InputQueueName + "_skipped");
                    await channel.QueueDeleteAsync(InputQueueName + "_skipped");

                    await channel.ExchangeDeleteAsync(InputQueueName + "_error");
                    await channel.QueueDeleteAsync(InputQueueName + "_error");

                    await channel.ExchangeDeleteAsync(InputQueueName + "_delay");
                }

                await CleanupVirtualHost(channel);

                await channel.CloseAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}
