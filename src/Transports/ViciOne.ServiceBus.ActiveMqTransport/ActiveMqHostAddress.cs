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
        public readonly int Port;
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

        public ActiveMqHostAddress(ActiveMqTransportProtocol protocol, string host, int port, string virtualHost)
            : this(SchemeFor(protocol), host, port, virtualHost)
        {
        }

        internal ActiveMqHostAddress(string scheme, string host, int? port, string virtualHost)
        {
            var normalizedScheme = NormalizeScheme(scheme);
            var normalizedPort = port switch
            {
                null or 0 => throw new ArgumentOutOfRangeException(nameof(port), port, "The ActiveMQ port must be configured explicitly and must be between 1 and 65535."),
                < 0 or > 65535 => throw new ArgumentOutOfRangeException(nameof(port), port, "The ActiveMQ port must be between 1 and 65535."),
                _ => port.Value
            };
            var normalizedVirtualHost = string.IsNullOrWhiteSpace(virtualHost) ? "/" : virtualHost;

            Scheme = normalizedScheme;
            Port = normalizedPort;
            VirtualHost = normalizedVirtualHost;
            Host = NormalizeHost(normalizedScheme, host, normalizedPort, normalizedVirtualHost);
        }

        static void ParseLeft(Uri address, out string scheme, out string host, out int port, out string virtualHost)
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

        static string NormalizeHost(string scheme, string host, int port, string virtualHost)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("The ActiveMQ host must not be null, empty, or whitespace.", nameof(host));

            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
                throw new ActiveMqTransportConfigurationException($"The ActiveMQ host is invalid: {host}");

            try
            {
                return new UriBuilder
                {
                    Scheme = scheme,
                    Host = host,
                    Port = port,
                    Path = virtualHost == "/"
                        ? "/"
                        : $"/{Uri.EscapeDataString(virtualHost)}"
                }.Uri.Host;
            }
            catch (UriFormatException exception)
            {
                throw new ActiveMqTransportConfigurationException($"The ActiveMQ host is invalid: {host}", exception);
            }
        }

        static string SchemeFor(ActiveMqTransportProtocol protocol) => protocol switch
        {
            ActiveMqTransportProtocol.OpenWire => ActiveMqScheme,
            ActiveMqTransportProtocol.Amqp => AmqpScheme,
            _ => throw new ActiveMqTransportConfigurationException($"The ActiveMQ protocol is not supported: {protocol}")
        };

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
            ThrowIfInvalid(address);

            var builder = new UriBuilder
            {
                Scheme = address.Scheme,
                Host = address.Host,
                Port = address.Port,
                Path = address.VirtualHost == "/"
                    ? "/"
                    : $"/{Uri.EscapeDataString(address.VirtualHost)}"
            };

            return builder.Uri;
        }

        static void ThrowIfInvalid(in ActiveMqHostAddress address)
        {
            if (address.Scheme is not (ActiveMqScheme or AmqpScheme)
                || string.IsNullOrWhiteSpace(address.Host)
                || Uri.CheckHostName(address.Host) == UriHostNameType.Unknown
                || address.Port is <= 0 or > 65535
                || string.IsNullOrWhiteSpace(address.VirtualHost))
            {
                throw new ActiveMqTransportConfigurationException(
                    "The ActiveMQ host address is not initialized or contains an invalid component.");
            }
        }

        Uri DebuggerDisplay => this;
    }
}
