namespace ViciOneServiceBusBenchmark
{
    using System;
    using System.Globalization;
    using System.Net.Security;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using ViciOne.ServiceBus;
    using ViciOne.ServiceBus.RabbitMqTransport;
    using ViciOne.ServiceBus.RabbitMqTransport.Configuration;
    using ViciOne.ServiceBus.Transports;
    using NDesk.Options;
    using RabbitMQ.Client;


    public class RabbitMqOptionSet :
        OptionSet,
        RabbitMqHostSettings
    {
        bool _portWasGiven;
        string _sslServerName;

        public RabbitMqOptionSet()
        {
            Add<string>("h|host:", "The host name of the broker", SetHost);
            Add<string>("sni|ssl-server-name:", "The name presented for TLS, when it differs from the host",
                value => _sslServerName = value);
            // The address builder already had a Port, but nothing could set it, so the tool could only
            // ever reach a broker on the default port. The repository's own pinned fixture publishes an
            // ephemeral loopback port by design, and the policy gate requires exactly that, so the
            // benchmark could not be pointed at the fixture it is meant to measure. Putting the port in
            // the host instead breaks UriBuilder, which is what "the hostname could not be parsed" was.
            Add<int>("port:", "The port the broker listens on", SetPort);
            Add<string>("vhost:", "The virtual host to use", value => VirtualHost = value);
            Add<string>("u|username:", "Username (if using basic credentials)", value => Username = value);
            Add<string>("p|password:", "Password (if using basic credentials)", value => Password = value);
            Add<TimeSpan>("heartbeat:", "Heartbeat (for RabbitMQ)", value => Heartbeat = value);
            Add<bool>("confirm:", "Publisher Confirmation", value => PublisherConfirmation = value);
            Add<bool>("batch:", "Batch Publish", value => BatchEnabled = value);
            Add<int>("batch-limit:", "Batch message limit", value => BatchLimit = value);
            Add<int>("batch-timeout:", "Batch Publish", value => BatchTimeout = value);
            Add<bool>("ssl:", "Use SSL", EnableSsl);
            Add<bool>("split:", "Split into two bus instances to leverage separate connections", x => Split = x);

            Host = "localhost";
            Heartbeat = TimeSpan.Zero;
            VirtualHost = "/";
            Port = 5672;

            Ssl = false;
            SslProtocol = SslProtocols.None;
            AcceptablePolicyErrors = SslPolicyErrors.None;
            ClientCertificatePath = "";
            ClientCertificatePassphrase = "";

            RequestedConnectionTimeout = TimeSpan.FromSeconds(10);
            MessageNameFormatter = new RabbitMqMessageNameFormatter();

            PublisherConfirmation = false;
            BatchEnabled = true;
        }

        public IMessageNameFormatter MessageNameFormatter { get; }

        public string[] ClusterMembers => null;
        public bool Split { get; set; }

        public bool BatchEnabled { get; set; }
        public int BatchLimit { get; set; } = 100;
        public int BatchTimeout { get; set; } = 1;
        public int BatchSizeLimit { get; set; } = 200000;

        public string Host { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public TimeSpan Heartbeat { get; set; }

        public bool Ssl { get; private set; }

        public SslProtocols SslProtocol { get; }
        /// <summary>
        /// The name presented for TLS. It used to be captured in the constructor, where the host is
        /// still the default, so it stayed "localhost" no matter which broker was addressed and a
        /// certificate would have been validated against the wrong name. It follows the effective host
        /// unless one is named deliberately, which a host given as an IP address needs.
        /// </summary>
        public string SslServerName => string.IsNullOrWhiteSpace(_sslServerName) ? Host : _sslServerName;
        public SslPolicyErrors AcceptablePolicyErrors { get; }
        public string ClientCertificatePath { get; }
        public string ClientCertificatePassphrase { get; }

        public X509Certificate ClientCertificate => null;
        public bool UseClientCertificateAsAuthenticationIdentity => false;

        public LocalCertificateSelectionCallback CertificateSelectionCallback { get; set; }

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }
        public IRabbitMqEndpointResolver EndpointResolver => null;

        public string ClientProvidedName => "vicione-servicebus-benchmark";

        /// <summary>
        /// Built from the current values rather than captured once. Holding the first answer meant an
        /// option parsed afterwards no longer reached the address, and which options those were
        /// depended on when something first happened to read this.
        /// </summary>
        public Uri HostAddress => FormatHostAddress();

        public bool PublisherConfirmation { get; set; }

        public ushort RequestedChannelMax { get; }

        public TimeSpan RequestedConnectionTimeout { get; }

        /// <summary>
        /// Projected from the current values, so every option order ends in one effective
        /// configuration. This used to be materialized once in the constructor while --batch-limit and
        /// --batch-timeout wrote to two properties nobody read again: the tool reported the limit it
        /// had been given and published with 100 and one millisecond.
        /// </summary>
        public BatchSettings BatchSettings => new ConfigurationBatchSettings
        {
            Enabled = BatchEnabled,
            MessageLimit = BatchLimit,
            SizeLimit = BatchSizeLimit,
            Timeout = TimeSpan.FromMilliseconds(BatchTimeout)
        };

        public TimeSpan ContinuationTimeout => TimeSpan.FromSeconds(20);
        public uint? MaxMessageSize { get; set; }
        public ICredentialsProvider CredentialsProvider { get; set; }
        public uint? RequestedFrameMax { get; set; }

        public Task Refresh(ConnectionFactory connectionFactory)
        {
            return Task.CompletedTask;
        }

        Uri FormatHostAddress()
        {
            var builder = new UriBuilder
            {
                Scheme = "rabbitmq",
                Host = Host,
                Port = Port == 5672 ? -1 : Port,
                Path = string.IsNullOrWhiteSpace(VirtualHost) || VirtualHost == "/"
                    ? "/"
                    : $"/{VirtualHost.Trim('/')}"
            };

            return builder.Uri;
        }

        /// <summary>
        /// A port belongs in the port option. Written into the host it reached UriBuilder, which failed
        /// with a message about a hostname far from the line that caused it, so it is refused here with
        /// the option that should have carried it. A bracketed IPv6 literal is a host, not a host and a
        /// port, and stays accepted.
        /// </summary>
        void SetHost(string host)
        {
            if (!string.IsNullOrWhiteSpace(host) && !host.StartsWith("[", StringComparison.Ordinal)
                && host.Contains(':'))
            {
                throw new OptionException(
                    $"The host '{host}' carries a port. Give the host alone and use --port for the port.", "host");
            }

            Host = host;
        }

        void SetPort(int port)
        {
            if (port is < 1 or > 65535)
                throw new OptionException($"The port {port} is outside 1..65535.", "port");

            Port = port;
            _portWasGiven = true;
        }

        /// <summary>
        /// Switching TLS moves the default port, but only while nobody has named one. Overwriting a
        /// given port here made the tool depend on the order of two independent options: --port before
        /// --ssl was silently discarded, and the run then dialled 5672 while reporting the host it was
        /// told about.
        /// </summary>
        void EnableSsl(bool enabled)
        {
            Ssl = enabled;

            if (!_portWasGiven)
                Port = enabled ? 5671 : 5672;
        }

        public void ShowOptions()
        {
            Console.WriteLine("Host: {0}", Host);
            Console.WriteLine("Port: {0}", Port);
            Console.WriteLine("Virtual Host: {0}", VirtualHost);
            Console.WriteLine("Username: {0}", Username);
            // The secret itself never appears here; only whether one was given.
            Console.WriteLine("Password configured: {0}", !string.IsNullOrEmpty(Password));
            Console.WriteLine("TLS: enabled={0}, protocol={1}, server name={2}", Ssl, SslProtocol, SslServerName);
            Console.WriteLine("Heartbeat: {0}", Heartbeat);
            Console.WriteLine("Publisher Confirmation: {0}", PublisherConfirmation);
            Console.WriteLine("Split: {0}", Split);
            var batch = BatchSettings;
            Console.WriteLine("Batch: enabled={0}, limit={1}, timeout={2}", batch.Enabled, batch.MessageLimit,
                batch.Timeout.ToString("c", CultureInfo.InvariantCulture));
        }
    }


    class ConfigurationBatchSettings :
        BatchSettings
    {
        public bool Enabled { get; set; }

        public int MessageLimit { get; set; }

        public int SizeLimit { get; set; }

        public TimeSpan Timeout { get; set; }
    }
}
