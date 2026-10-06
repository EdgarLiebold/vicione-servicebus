using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates and caches receive dispatchers by endpoint name.</summary>
public sealed class ReceiveEndpointDispatcherFactory :
    IReceiveEndpointDispatcherFactory
{
    readonly ConcurrentDictionary<string, Lazy<IReceiveEndpointDispatcher>> _dispatchers;
    readonly IHostConfiguration _hostConfiguration;
    readonly IBusRegistrationContext _registration;
    int _disposed;

    /// <summary>Initializes a dispatcher factory for a registered bus instance.</summary>
    /// <param name="registration">The registration context used to configure handlers.</param>
    /// <param name="busInstance">The bus instance that owns receive endpoint configuration.</param>
    public ReceiveEndpointDispatcherFactory(IBusRegistrationContext registration, IBusInstance busInstance)
    {
        _registration = registration ?? throw new ArgumentNullException(nameof(registration));
        ArgumentNullException.ThrowIfNull(busInstance);
        _hostConfiguration = busInstance.HostConfiguration
            ?? throw new InvalidOperationException("The bus instance returned no host configuration.");

        _dispatchers = new ConcurrentDictionary<string, Lazy<IReceiveEndpointDispatcher>>(StringComparer.Ordinal);
    }

    /// <summary>Gets or creates a receiver containing the handlers assigned to a queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <returns>The dispatcher cached for the queue.</returns>
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
            if (kind is IConsumerKindDispatcherProvider dispatcherProvider
                && dispatcherProvider.TryCreateDispatcher(registrationType, this, formatter, out IReceiveEndpointDispatcher? dispatcher))
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

    /// <summary>Releases cached dispatcher references and records their final delivery metrics.</summary>
    /// <returns>A completed value task.</returns>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return default;

        try
        {
            IEnumerable<IReceiveEndpointDispatcher> dispatchers = _dispatchers.Values.Where(x => x.IsValueCreated).Select(x => x.Value);
            foreach (var dispatcher in dispatchers)
            {
                var metrics = dispatcher.GetMetrics();
                var logger = LogContext.Debug;
                if (logger is null)
                    continue;

                Uri inputAddress = dispatcher.InputAddress;
                long deliveryCount = metrics.DeliveryCount;
                int maxConcurrentDeliveryCount = metrics.MaxConcurrentDeliveryCount;
                try
                {
                    logger.Log("Dispatcher completed {InputAddress}: {DeliveryCount} received, {ConcurrentDeliveryCount} concurrent",
                        inputAddress, deliveryCount, maxConcurrentDeliveryCount);
                }
                catch (Exception)
                {
                    // Optional metrics logging must not retain cached dispatcher references.
                }
            }
        }
        finally
        {
            _dispatchers.Clear();
        }

        return default;
    }

    IReceiveEndpointDispatcher CreateMessageReceiver(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(configure);

        Lazy<IReceiveEndpointDispatcher> lazyDispatcher = _dispatchers.GetOrAdd(queueName, name =>
            new Lazy<IReceiveEndpointDispatcher>(() => CreateMessageReceiverCore(name, configure), LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return lazyDispatcher.Value;
        }
        catch
        {
            _dispatchers.TryRemove(new KeyValuePair<string, Lazy<IReceiveEndpointDispatcher>>(queueName, lazyDispatcher));
            throw;
        }
    }

    IReceiveEndpointDispatcher CreateMessageReceiverCore(string queueName, Action<IReceiveEndpointConfigurator> configure)
    {
        var endpointConfiguration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName)
            ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                "Receive endpoint", "unknown", "The host configuration returned no endpoint configuration",
                "Return a receive endpoint configuration for the requested queue"));

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
            var receiveEndpointContext = endpointConfiguration.CreateReceiveEndpointContext()
                ?? throw new InvalidOperationException("The endpoint configuration returned no receive endpoint context.");

            return new ReceiveEndpointDispatcher(receiveEndpointContext);
        }
        catch (Exception exception)
        {
            throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", "An exception occurred during dispatcher creation", "Correct the named configuration before starting the host"), exception);
        }
    }
}
