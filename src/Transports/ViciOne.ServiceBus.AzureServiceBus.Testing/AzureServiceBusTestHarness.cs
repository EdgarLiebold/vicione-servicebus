using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Testing;

namespace ViciOne.ServiceBus.AzureServiceBus.Testing;

/// <summary>Runs a bus test harness against an Azure Service Bus namespace.</summary>
public class AzureServiceBusTestHarness :
    BusTestHarness
{
    Uri? _inputQueueAddress;

    /// <summary>Initializes the harness for an Azure Service Bus namespace and input queue.</summary>
    /// <param name="serviceUri">The namespace URI.</param>
    /// <param name="namedKeyCredential">The credential used for transport and administration operations.</param>
    /// <param name="inputQueueName">The input queue name, or <see langword="null"/> to use <c>input_queue</c>.</param>
    public AzureServiceBusTestHarness(Uri serviceUri, AzureNamedKeyCredential namedKeyCredential, string? inputQueueName = null)
    {
        ArgumentNullException.ThrowIfNull(serviceUri);
        ArgumentNullException.ThrowIfNull(namedKeyCredential);
        ValidateServiceUri(serviceUri);

        string effectiveInputQueueName = inputQueueName ?? "input_queue";
        ArgumentException.ThrowIfNullOrWhiteSpace(effectiveInputQueueName, nameof(inputQueueName));

        HostAddress = serviceUri;
        NamedKeyCredential = namedKeyCredential;
        InputQueueName = effectiveInputQueueName;
        UseMessageScheduler = true;
    }

    /// <summary>Gets the credential used to access the Azure Service Bus namespace.</summary>
    public AzureNamedKeyCredential NamedKeyCredential { get; }
    /// <summary>Gets the queue on which the harness receives test messages.</summary>
    public override string InputQueueName { get; }
    /// <summary>Gets or sets whether the harness configures Azure Service Bus scheduled delivery.</summary>
    public bool UseMessageScheduler { get; set; }

    /// <summary>Gets the input queue address after the receive endpoint has been configured.</summary>
    public override Uri InputQueueAddress => _inputQueueAddress
        ?? throw new InvalidOperationException("The input queue address is not available before the bus has been created.");
    /// <summary>Gets the Azure Service Bus namespace address.</summary>
    public Uri HostAddress { get; }

    /// <summary>Occurs while the harness configures the Azure Service Bus factory.</summary>
    public event Action<IServiceBusBusFactoryConfigurator>? AzureServiceBusConfiguring;
    /// <summary>Occurs while the harness configures its provider-specific receive endpoint.</summary>
    public event Action<IServiceBusReceiveEndpointConfigurator>? AzureServiceBusReceiveEndpointConfiguring;

    /// <summary>Invokes registered callbacks for provider-specific bus configuration.</summary>
    /// <param name="configurator">The Azure Service Bus factory configurator.</param>
    protected virtual void ConfigureAzureServiceBus(IServiceBusBusFactoryConfigurator configurator)
    {
        AzureServiceBusConfiguring?.Invoke(configurator);
    }

    /// <summary>Invokes registered callbacks for provider-specific receive-endpoint configuration.</summary>
    /// <param name="configurator">The Azure Service Bus receive-endpoint configurator.</param>
    protected virtual void ConfigureAzureServiceBusReceiveEndpoint(IServiceBusReceiveEndpointConfigurator configurator)
    {
        AzureServiceBusReceiveEndpointConfiguring?.Invoke(configurator);
    }

    /// <summary>Deletes the topics and queues discovered by sequential enumeration of the configured Azure Service Bus namespace.</summary>
    /// <param name="cancellationToken">The token that cancels entity enumeration and deletion.</param>
    /// <returns>A task that completes after the discovered topic and queue names have been deleted or are already absent.</returns>
    public override async Task CleanAsync(CancellationToken cancellationToken = default)
    {
        await AzureServiceBusNamespaceCleaner
            .CleanAsync(CreateAdministrationClient(), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Creates the administration client used by <see cref="CleanAsync"/>.</summary>
    /// <returns>An administration client authenticated with <see cref="NamedKeyCredential"/>.</returns>
    protected virtual ServiceBusAdministrationClient CreateAdministrationClient()
    {
        var endpoint = new UriBuilder(HostAddress) { Path = "" }.Uri.ToString();

        return new ServiceBusAdministrationClient(endpoint, NamedKeyCredential);
    }

    /// <summary>Creates the bus and captures the configured input queue address.</summary>
    /// <param name="cancellationToken">Cancellation checked before synchronous bus construction begins.</param>
    /// <returns>A task that produces the configured bus control.</returns>
    protected override Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<IBusControl>(cancellationToken);

        IBusControl bus = ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingAzureServiceBus(x =>
        {
            x.Host(HostAddress, h =>
            {
                h.NamedKey(s =>
                {
                    s.NamedKeyCredential = NamedKeyCredential;
                });
            });

            ConfigureBus(x);

            ConfigureAzureServiceBus(x);

            if (UseMessageScheduler)
                x.ConfigureServiceBusMessageScheduler();

            x.ReceiveEndpoint(InputQueueName, e =>
            {
                ConfigureReceiveEndpoint(e);

                ConfigureAzureServiceBusReceiveEndpoint(e);

                _inputQueueAddress = e.InputAddress;
            });
        });

        return Task.FromResult(bus);
    }

    static void ValidateServiceUri(Uri serviceUri)
    {
        if (!serviceUri.IsAbsoluteUri)
            throw new ArgumentException("The Azure Service Bus namespace URI must be absolute.", nameof(serviceUri));
        if (!serviceUri.Scheme.Equals("sb", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The Azure Service Bus namespace URI must use the 'sb' scheme.", nameof(serviceUri));
        if (serviceUri.Host.Length == 0
            || serviceUri.AbsolutePath != "/"
            || serviceUri.UserInfo.Length > 0
            || serviceUri.Query.Length > 0
            || serviceUri.Fragment.Length > 0)
        {
            throw new ArgumentException(
                "The Azure Service Bus namespace URI cannot contain a path, user information, a query, or a fragment.",
                nameof(serviceUri));
        }
    }
}
