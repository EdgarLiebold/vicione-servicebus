namespace ViciOne.ServiceBus.ActiveMqTransport.Tests
{
    using System;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Single place where the ActiveMQ specs read the fixture the canonical runner started.
    /// <para>
    /// The pinned fixture publishes ephemeral loopback ports and provisions an account that exists
    /// only for this run, so neither an endpoint nor a credential may be written into a spec. The
    /// values below come from the environment the runner hands to the test process.
    /// </para>
    /// </summary>
    public static class RunScopedBroker
    {
        static int Port(string variable, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return int.TryParse(value, out var port) && port > 0 ? port : fallback;
        }

        /// <summary>Host the fixture is reachable on.</summary>
        public static string Host =>
            Environment.GetEnvironmentVariable(ActiveMqTestHarness.HostVariable) ?? "localhost";

        /// <summary>OpenWire port resolved from Docker for this run.</summary>
        public static int OpenWirePort => Port(ActiveMqTestHarness.OpenWirePortVariable, 61616);

        /// <summary>AMQP port resolved from Docker for this run.</summary>
        public static int AmqpPort => Port(ActiveMqTestHarness.AmqpPortVariable, 5672);

        /// <summary>Jolokia port resolved from Docker for this run.</summary>
        public static int JolokiaPort => Port(ActiveMqTestHarness.JolokiaPortVariable, 8161);

        /// <summary>User of the run-scoped account.</summary>
        public static string User =>
            Environment.GetEnvironmentVariable(ActiveMqTestHarness.UsernameVariable) ?? "admin";

        /// <summary>Secret of the run-scoped account.</summary>
        public static string Pass =>
            Environment.GetEnvironmentVariable(ActiveMqTestHarness.PasswordVariable) ?? "admin";

        /// <summary>Address of the fixture for the given protocol.</summary>
        public static Uri AddressFor(string protocol)
        {
            return protocol == ActiveMqHostAddress.AmqpScheme
                ? new Uri($"amqp://{Host}:{AmqpPort}")
                : new Uri($"activemq://{Host}:{OpenWirePort}");
        }
    }
}
