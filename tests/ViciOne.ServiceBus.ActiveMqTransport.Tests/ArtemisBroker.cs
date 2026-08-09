// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.
namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;


    /// <summary>
    /// Run-scoped endpoint of the Artemis broker used by the 'artemis' flavor of the parameterized specs.
    /// <para>
    /// Artemis is a second, separate broker and is not the pinned ViciOne fixture. The imported baseline
    /// addressed it at a fixed port 61618 with a well known admin account, which made the branch neither
    /// a reproducible fixture nor visibly excluded. Every value now comes from the environment the
    /// canonical runner provides.
    /// </para>
    /// <para>
    /// When the environment is absent, reading an endpoint throws. That is deliberate: a silent fallback
    /// to a fixed port would let a spec appear to prove something against whatever happens to listen
    /// there, which is exactly the defect this type exists to remove.
    /// </para>
    /// </summary>
    public static class ArtemisBroker
    {
        public const string HostVariable = "VICIONE_SERVICEBUS_ARTEMIS_HOST";
        public const string PortVariable = "VICIONE_SERVICEBUS_ARTEMIS_OPENWIRE_PORT";
        public const string UsernameVariable = "VICIONE_SERVICEBUS_ARTEMIS_USER";
        public const string PasswordVariable = "VICIONE_SERVICEBUS_ARTEMIS_PASS";

        /// <summary>
        /// True when the runner started an Artemis fixture and published its endpoint.
        /// </summary>
        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(HostVariable))
            && int.TryParse(Environment.GetEnvironmentVariable(PortVariable), out var port) && port > 0;

        static string Required(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"{variable} is not set, so no Artemis fixture is running for this run. The artemis "
                    + "flavor belongs to the extended profile and must not fall back to a fixed port.");
            }

            return value;
        }

        /// <summary>Address of the run-scoped Artemis broker.</summary>
        public static Uri Address
        {
            get
            {
                var port = Required(PortVariable);
                if (!int.TryParse(port, out var value) || value <= 0)
                    throw new InvalidOperationException($"{PortVariable} is not a usable port: '{port}'.");

                return new Uri($"activemq://{Required(HostVariable)}:{value}");
            }
        }

        /// <summary>
        /// AMQP address of the run-scoped Artemis broker. The image multiplexes every protocol onto
        /// the same acceptor, so AMQP shares the port published for OpenWire.
        /// </summary>
        public static Uri AmqpAddress
        {
            get
            {
                var address = Address;
                return new Uri($"{ActiveMqHostAddress.AmqpScheme}://{address.Host}:{address.Port}");
            }
        }

        /// <summary>User of the run-scoped Artemis account.</summary>
        public static string User => Required(UsernameVariable);

        /// <summary>Secret of the run-scoped Artemis account.</summary>
        public static string Pass => Required(PasswordVariable);
    }
}
