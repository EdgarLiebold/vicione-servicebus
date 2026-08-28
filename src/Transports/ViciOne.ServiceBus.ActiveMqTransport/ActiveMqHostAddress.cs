namespace ViciOne.ServiceBus
{
    using System;
    using System.Diagnostics;
    using Internals;


    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public readonly struct ActiveMqHostAddress
    {
        public const string ActiveMqScheme = "activemq";
        public const string AmqpScheme = "amqp";

        public readonly string Scheme;
        public readonly string Host;
        public readonly int? Port;
        public readonly string VirtualHost;

        public ActiveMqHostAddress(Uri address)
        {
            ArgumentNullException.ThrowIfNull(address);
            RejectCredentialsAndUnsupportedComponents(address);

            Scheme = default;
            Host = default;
            Port = default;
            VirtualHost = default;

            var scheme = address.Scheme.ToLowerInvariant();
            switch (scheme)
            {
                case ActiveMqScheme:
                case AmqpScheme:
                    ParseLeft(address, out Scheme, out Host, out Port, out VirtualHost);
                    break;

                default:
                    throw new ActiveMqTransportConfigurationException($"The address scheme is not supported: {address.Scheme}");
            }
        }

        public ActiveMqHostAddress(string host, int? port, string virtualHost)
            : this(ActiveMqScheme, host, port, virtualHost)
        {
        }

        public ActiveMqHostAddress(string scheme, string host, int? port, string virtualHost)
        {
            Scheme = NormalizeScheme(scheme);
            Host = string.IsNullOrWhiteSpace(host)
                ? throw new ArgumentException("The ActiveMQ host must not be null, empty, or whitespace.", nameof(host))
                : host;
            Port = port switch
            {
                null or 0 => throw new ArgumentOutOfRangeException(nameof(port), port, "The ActiveMQ port must be configured explicitly and must be between 1 and 65535."),
                < 0 or > 65535 => throw new ArgumentOutOfRangeException(nameof(port), port, "The ActiveMQ port must be between 1 and 65535."),
                _ => port
            };
            VirtualHost = string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost;
        }

        static void ParseLeft(Uri address, out string scheme, out string host, out int? port, out string virtualHost)
        {
            scheme = address.Scheme;
            host = address.Host;

            if (address.IsDefaultPort || address.Port <= 0)
                throw new ActiveMqTransportConfigurationException("The ActiveMQ port must be present explicitly in the host address.");

            port = address.Port;

            virtualHost = address.ParseHostPath();
        }

        static string NormalizeScheme(string scheme)
        {
            var normalized = scheme?.ToLowerInvariant();
            return normalized switch
            {
                ActiveMqScheme or AmqpScheme => normalized,
                _ => throw new ActiveMqTransportConfigurationException($"The address scheme is not supported: {scheme}")
            };
        }

        static void RejectCredentialsAndUnsupportedComponents(Uri address)
        {
            if (!string.IsNullOrEmpty(address.UserInfo))
            {
                throw new ActiveMqTransportConfigurationException(
                    "Credentials must be configured through the ActiveMQ host configurator, never embedded in a URI.");
            }

            if (!string.IsNullOrEmpty(address.Query) || !string.IsNullOrEmpty(address.Fragment))
            {
                throw new ActiveMqTransportConfigurationException(
                    "ActiveMQ host transport options must be configured through the typed host configurator.");
            }
        }

        public static implicit operator Uri(in ActiveMqHostAddress address)
        {
            var builder = new UriBuilder
            {
                Scheme = address.Scheme,
                Host = address.Host,
                Port = address.Port ?? throw new ActiveMqTransportConfigurationException("The ActiveMQ port is unavailable."),
                Path = address.VirtualHost == "/"
                    ? "/"
                    : $"/{Uri.EscapeDataString(address.VirtualHost)}"
            };

            return builder.Uri;
        }

        Uri DebuggerDisplay => this;
    }
}
