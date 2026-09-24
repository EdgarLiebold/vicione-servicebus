using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates Azure Service Bus messaging and administration clients for a configured namespace.</summary>
public class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Initializes the factory from the resolved namespace configuration.</summary>
    /// <param name="hostConfiguration">The host configuration containing client, credential, and retry settings.</param>
    public ConnectionContextFactory(IServiceBusHostConfiguration hostConfiguration)
    {
        _hostConfiguration = hostConfiguration;
    }

    IPipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateContext(ISupervisor supervisor)
    {
        Task<ConnectionContext> context = Task.Run(() => CreateConnection(supervisor), supervisor.Stopped);

        IPipeContextAgent<ConnectionContext> contextHandle = supervisor.AddContext(context);

        return contextHandle;
    }

    IActivePipeContextAgent<ConnectionContext> IPipeContextFactory<ConnectionContext>.CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    async Task<ConnectionContext> CreateSharedConnectionAsync(Task<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully
            ? new SharedConnectionContext(context.Result, cancellationToken)
            : new SharedConnectionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    ConnectionContext CreateConnection(ISupervisor supervisor)
    {
        var endpoint = new UriBuilder(_hostConfiguration.HostAddress) { Path = "" }.Uri.Host;

        if (supervisor.Stopping.IsCancellationRequested)
            throw new ServiceBusConnectionException($"The connection is stopping and cannot be used: {endpoint}");

        var settings = _hostConfiguration.Settings;

        var client = settings.ServiceBusClient;
        var managementClient = settings.ServiceBusAdministrationClient;

        ValidateCustomPort(settings, client, managementClient);

        (client, managementClient) = CreateMissingClients(settings, endpoint, client, managementClient);

        var namespaceAddress = new UriBuilder(_hostConfiguration.HostAddress) { Path = "", Query = "", Fragment = "" }.Uri;
        return new ServiceBusConnectionContext(client, managementClient, supervisor.Stopped, namespaceAddress);
    }

    static void ValidateCustomPort(ServiceBusHostSettings settings, ServiceBusClient? client,
        ServiceBusAdministrationClient? managementClient)
    {
        if (settings.ServiceUri.IsDefaultPort || (client != null && managementClient != null))
            return;

        if (settings.ConnectionString != null && HasSharedAccess(settings.ConnectionString)
            && ViciOne.ServiceBus.Configuration.ServiceBusHostConfigurator.IsDevelopmentEmulator(settings.ConnectionString))
            return;

        throw new ServiceBusConnectionException(
            "A custom port requires a credential-bearing emulator connection string or both preconfigured Service Bus clients");
    }

    static (ServiceBusClient Client, ServiceBusAdministrationClient ManagementClient) CreateMissingClients(
        ServiceBusHostSettings settings, string endpoint, ServiceBusClient? client,
        ServiceBusAdministrationClient? managementClient)
    {
        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = settings.TransportType,
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = settings.RetryLimit,
                Mode = ServiceBusRetryMode.Exponential,
                MaxDelay = settings.RetryMaxBackoff,
            },
            EnableCrossEntityTransactions = false,
        };

        var managementOptions = new ServiceBusAdministrationClientOptions
        {
            Retry =
            {
                MaxRetries = settings.RetryLimit,
                Mode = Azure.Core.RetryMode.Exponential,
                MaxDelay = settings.RetryMaxBackoff
            }
        };

        TokenCredential? tokenCredential = settings.TokenCredential;
        if (tokenCredential == null && settings.NamedKeyCredential == null && settings.SasCredential == null
            && (settings.ConnectionString == null || !HasSharedAccess(settings.ConnectionString)))
            tokenCredential = new DefaultAzureCredential();

        client ??= CreateMessagingClient(settings, endpoint, tokenCredential, clientOptions);
        managementClient ??= CreateAdministrationClient(settings, endpoint, tokenCredential, managementOptions);
        return (client, managementClient);
    }

    static ServiceBusClient CreateMessagingClient(ServiceBusHostSettings settings, string endpoint,
        TokenCredential? tokenCredential, ServiceBusClientOptions options)
    {
        if (tokenCredential != null)
            return new ServiceBusClient(endpoint, tokenCredential, options);
        if (settings.NamedKeyCredential != null)
            return new ServiceBusClient(endpoint, settings.NamedKeyCredential, options);
        if (settings.SasCredential != null)
            return new ServiceBusClient(endpoint, settings.SasCredential, options);
        return new ServiceBusClient(settings.ConnectionString!, options);
    }

    static ServiceBusAdministrationClient CreateAdministrationClient(ServiceBusHostSettings settings, string endpoint,
        TokenCredential? tokenCredential, ServiceBusAdministrationClientOptions options)
    {
        if (tokenCredential != null)
            return new ServiceBusAdministrationClient(endpoint, tokenCredential, options);
        if (settings.NamedKeyCredential != null)
            return new ServiceBusAdministrationClient(endpoint, settings.NamedKeyCredential, options);
        if (settings.SasCredential != null)
            return new ServiceBusAdministrationClient(endpoint, settings.SasCredential, options);
        return new ServiceBusAdministrationClient(settings.ConnectionString!, options);
    }

    static bool HasSharedAccess(string connectionString)
    {
        var connectionStringProperties = ServiceBusConnectionStringProperties.Parse(connectionString);

        return !string.IsNullOrEmpty(connectionStringProperties.SharedAccessKeyName)
            && !string.IsNullOrEmpty(connectionStringProperties.SharedAccessKey) || !string.IsNullOrEmpty(connectionStringProperties.SharedAccessSignature);
    }
}
