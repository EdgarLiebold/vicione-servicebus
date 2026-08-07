// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests
{
    using System;
    using global::Azure.Messaging.ServiceBus.Administration;
    using ViciOne.ServiceBus.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    static class Configuration
    {
        public static string? KeyName =>
            TestContext.Parameters.Exists(nameof(KeyName))
                ? TestContext.Parameters.Get(nameof(KeyName))
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_ASB_KEYNAME") ?? "ViciOneServiceBusBuild";

        public static string? ServiceNamespace =>
            TestContext.Parameters.Exists(nameof(ServiceNamespace))
                ? TestContext.Parameters.Get(nameof(ServiceNamespace))
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_ASB_NAMESPACE") ?? "vicione-servicebus-build";

        public static string? SharedAccessKey =>
            TestContext.Parameters.Exists(nameof(SharedAccessKey))
                ? TestContext.Parameters.Get(nameof(SharedAccessKey))
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_ASB_KEYVALUE") ?? "YfN2b8jT84759bZy5sMhd0P+3K/qHqO81I5VrNrJYkI=";

        public static string? StorageAccount =>
            TestContext.Parameters.Exists(nameof(StorageAccount))
                ? TestContext.Parameters.Get(nameof(StorageAccount))
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_AZURE_STORAGE_ACCOUNT") ?? "";

        public static string? Compress =>
            TestContext.Parameters.Exists(nameof(Compress))
                ? TestContext.Parameters.Get(nameof(Compress))
                : Environment.GetEnvironmentVariable("VICIONE_SERVICEBUS_AZURE_STORAGE_COMPRESS") ?? "false";

        public static ServiceBusAdministrationClient GetManagementClient()
        {
            var hostAddress = AzureServiceBusEndpointUriCreator.Create(ServiceNamespace);
            var accountSettings = new TestAzureServiceBusAccountSettings();

            var hostConfigurator = new ServiceBusHostConfigurator(hostAddress);

            hostConfigurator.NamedKey(s =>
            {
                s.NamedKeyCredential = accountSettings.NamedKeyCredential;
            });

            var endpoint = new UriBuilder(hostAddress) { Path = "" }.Uri.Host;

            var managementClient = new ServiceBusAdministrationClient(endpoint, hostConfigurator.Settings.NamedKeyCredential);

            return managementClient;
        }

        public static string CreateConnectionString(string? ns, string? keyName, string? key)
        {
            return $"Endpoint=sb://{ns}.servicebus.windows.net/;SharedAccessKeyName={keyName};SharedAccessKey={key}";
        }

        public static void UsingTestAzureServiceBus(this IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, IServiceBusBusFactoryConfigurator>? configure = null, bool configureEndpoints = true)
        {
            configurator.AddOptions<AzureServiceBusTransportOptions>()
                .Configure(options => options.ConnectionString = CreateConnectionString(ServiceNamespace, KeyName, SharedAccessKey));

            configurator.UsingAzureServiceBus((context, cfg) =>
            {
                cfg.UseServiceBusMessageScheduler();

                configure?.Invoke(context, cfg);

                if (configureEndpoints)
                    cfg.ConfigureEndpoints(context);
            });

            configurator.AddOptions<TestHarnessOptions>()
                .Configure(options =>
                {
                    options.TestInactivityTimeout = TimeSpan.FromSeconds(10);
                });
        }
    }
}
