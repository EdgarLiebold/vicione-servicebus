// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-08.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Applies the run-scoped broker account to the dependency injection based specs.
    /// <para>
    /// The classic <see cref="RabbitMqTestHarness" /> reads the account itself, but the container
    /// specs go through <see cref="RabbitMqTransportOptions" />, whose defaults are product defaults
    /// and deliberately not changed by the test suite. This helper is the single place where the
    /// test side binds those options to the account the pinned fixture provisions.
    /// </para>
    /// </summary>
    public static class RunScopedCredentials
    {
        /// <summary>
        /// Binds the run-scoped account onto the transport options.
        /// Absent variables leave the product defaults untouched; the suite already refuses to start
        /// in that case through <c>RabbitMqTestSetUpFixture</c>, which reports the missing
        /// configuration by name instead of failing later with an opaque authentication error.
        /// </summary>
        /// <summary>Host the canonical runner published the fixture on.</summary>
        public static string Host =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.HostVariable) ?? "localhost";

        /// <summary>Ephemeral AMQP port the canonical runner resolved from Docker, 0 when unset.</summary>
        public static ushort Port =>
            ushort.TryParse(Environment.GetEnvironmentVariable(RabbitMqTestHarness.PortVariable), out var port) ? port : (ushort)0;

        /// <summary>User of the run-scoped account.</summary>
        public static string User =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.UsernameVariable) ?? "guest";

        /// <summary>Secret of the run-scoped account.</summary>
        public static string Pass =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.PasswordVariable) ?? "guest";

        public static RabbitMqTransportOptions ApplyRunScopedCredentials(this RabbitMqTransportOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var user = Environment.GetEnvironmentVariable(RabbitMqTestHarness.UsernameVariable);
            var pass = Environment.GetEnvironmentVariable(RabbitMqTestHarness.PasswordVariable);

            if (!string.IsNullOrWhiteSpace(user))
                options.User = user;

            if (!string.IsNullOrWhiteSpace(pass))
                options.Pass = pass;

            var host = Environment.GetEnvironmentVariable(RabbitMqTestHarness.HostVariable);
            if (!string.IsNullOrWhiteSpace(host))
                options.Host = host;

            // The canonical runner publishes ephemeral loopback ports, so the fixture endpoints are
            // only known at run time and must never be assumed.
            if (ushort.TryParse(Environment.GetEnvironmentVariable(RabbitMqTestHarness.PortVariable), out var port) && port > 0)
                options.Port = port;

            if (ushort.TryParse(Environment.GetEnvironmentVariable(RabbitMqTestHarness.ManagementPortVariable), out var management) && management > 0)
                options.ManagementPort = management;

            return options;
        }
    }
}
