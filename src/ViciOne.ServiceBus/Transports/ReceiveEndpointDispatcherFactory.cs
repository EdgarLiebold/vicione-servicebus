using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates receive endpoint dispatcher instances.</summary>
public class ReceiveEndpointDispatcherFactory :
    IReceiveEndpointDispatcherFactory
{
    readonly ConcurrentDictionary<string, Lazy<IReceiveEndpointDispatcher>> _dispatchers;
    readonly IHostConfiguration _hostConfiguration;
    readonly IBusRegistrationContext _registration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="registration">The registration.</param>
    /// <param name="busInstance">The bus instance.</param>
    public ReceiveEndpointDispatcherFactory(IBusRegistrationContext registration, IBusInstance busInstance)
    {
        _hostConfiguration = busInstance.HostConfiguration;
        _registration = registration;

        _dispatchers = new ConcurrentDictionary<string, Lazy<IReceiveEndpointDispatcher>>();
    }

    /// <summary>Creates receiver.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The created receiver.</returns>
    public IReceiveEndpointDispatcher CreateReceiver(string queueName)
    {
        return CreateMessageReceiver(queueName, _registration.ConfigureConsumerKinds);
    }

    /// <inheritdoc />
    public IReceiveEndpointDispatcher CreateRegistrationReceiver(Type registrationType, string fallbackQueueName,
        IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(registrationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackQueueName);
        ArgumentNullException.ThrowIfNull(formatter);

        IEnumerable<IConsumerKind> kinds =
            (IEnumerable<IConsumerKind>?)_registration.GetService(typeof(IEnumerable<IConsumerKind>))
            ?? Array.Empty<IConsumerKind>();

        foreach (IConsumerKind kind in kinds.OrderBy(candidate => candidate.IsFallback)
                     .ThenBy(candidate => candidate.Order)
                     .ThenBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            if (kind.TryCreateDispatcher(registrationType, this, formatter, out IReceiveEndpointDispatcher? dispatcher))
                return dispatcher ?? throw new ConfigurationException(
                    global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                        "Consumer kind",
                        "unknown",
                        $"'{kind.Name}' accepted '{registrationType}' without creating a dispatcher",
                        "Return a dispatcher when accepting a registration type"));
        }

        return CreateReceiver(fallbackQueueName);
    }

    /// <inheritdoc />
    public IReceiveEndpointDispatcher CreateReceiver(string queueName,
        Action<IReceiveEndpointConfigurator, IRegistrationContext> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        return CreateMessageReceiver(queueName, endpoint => configure(endpoint, _registration));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        IEnumerable<IReceiveEndpointDispatcher> dispatchers = _dispatchers.Values.Where(x => x.IsValueCreated).Select(x => x.Value);
        foreach (var dispatcher in dispatchers)
        {
            var metrics = dispatcher.GetMetrics();
            LogContext.Debug?.Log("Dispatcher completed {InputAddress}: {DeliveryCount} received, {ConcurrentDeliveryCount} concurrent",
                dispatcher.InputAddress, metrics.DeliveryCount, metrics.ConcurrentDeliveryCount);
        }

        return default;
    }

    IReceiveEndpointDispatcher CreateMessageReceiver(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        if (string.IsNullOrWhiteSpace(queueName))
            throw new ArgumentNullException(nameof(queueName));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        return _dispatchers.GetOrAdd(queueName, name => new Lazy<IReceiveEndpointDispatcher>(() =>
        {
            var endpointConfiguration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName);

            var configurator = endpointConfiguration as IReceiveEndpointConfigurator ??
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "The receive endpoint configuration was not valid", "Correct the named configuration before starting the host"));

            configurator.ThrowOnSkippedMessages();
            configurator.RethrowFaultedMessages();
            configurator.PublishFaults = false;
            configurator.ConfigureConsumeTopology = false;

            var configureReceiveEndpoint = _registration.GetConfigureReceiveEndpoints();

            configureReceiveEndpoint.Configure(queueName, configurator);

            configure(configurator);

            IReadOnlyList<ValidationResult> result = endpointConfiguration.Validate()
                .ThrowIfContainsFailure("The endpoint configuration is invalid:");

            try
            {
                var receiveEndpointContext = endpointConfiguration.CreateReceiveEndpointContext();

                return new ReceiveEndpointDispatcher(receiveEndpointContext);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "An exception occurred during dispatcher creation", "Correct the named configuration before starting the host"), ex);
            }
        })).Value;
    }
}
