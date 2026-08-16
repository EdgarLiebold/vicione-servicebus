namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using ViciOne.ServiceBus.Testing;


    /// <summary>
    /// Single place where a spec reads the RabbitMQ fixture the canonical runner started.
    /// <para>
    /// The fixture publishes an ephemeral loopback port per run, so a spec that writes "localhost"
    /// without a port does not address it. It addresses whatever else happens to hold the default
    /// port 5672 instead, and then reports a result that says nothing about the pinned fixture.
    /// Measured on 2026-08-08: with a foreign broker on 5672 the required run reported 302 passed and
    /// 4 failed; with that broker stopped and nothing else changed, 289 passed and 17 failed. Fourteen
    /// tests had been green for the wrong reason.
    /// </para>
    /// </summary>
    public static class RunScopedBroker
    {
        /// <summary>Virtual host the fixture provisions for the test run.</summary>
        public const string VirtualHost = "test";

        static int Port(string variable, int fallback)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            return int.TryParse(value, out var port) && port > 0 ? port : fallback;
        }

        /// <summary>Host the fixture is reachable on.</summary>
        public static string Host =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.HostVariable) ?? "localhost";

        /// <summary>AMQP port resolved from Docker for this run.</summary>
        public static int AmqpPort => Port(RabbitMqTestHarness.PortVariable, 5672);

        /// <summary>Management API port resolved from Docker for this run.</summary>
        public static int ManagementPort => Port(RabbitMqTestHarness.ManagementPortVariable, 15672);

        /// <summary>User of the run-scoped account.</summary>
        public static string User =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.UsernameVariable) ?? "guest";

        /// <summary>Secret of the run-scoped account.</summary>
        public static string Pass =>
            Environment.GetEnvironmentVariable(RabbitMqTestHarness.PasswordVariable) ?? "guest";

        /// <summary>Address of the fixture including the virtual host the test run uses.</summary>
        public static Uri HostAddress => new Uri($"rabbitmq://{Host}:{AmqpPort}/{VirtualHost}");

        /// <summary>Address of a queue on the fixture.</summary>
        public static Uri QueueAddress(string queueName) =>
            new Uri($"rabbitmq://{Host}:{AmqpPort}/{VirtualHost}/{queueName}");

        /// <summary>Base address of the management API for this run.</summary>
        public static Uri ManagementAddress => new Uri($"http://{Host}:{ManagementPort}");

        /// <summary>
        /// Queue address carrying the run-scoped account, for the specs that derive their host
        /// settings from a URI instead of from a host callback.
        /// </summary>
        public static Uri QueueAddressWithCredentials(string queueName)
        {
            var user = Uri.EscapeDataString(User);
            var pass = Uri.EscapeDataString(Pass);
            return new Uri($"rabbitmq://{user}:{pass}@{Host}:{AmqpPort}/{VirtualHost}/{queueName}");
        }
    }
}
