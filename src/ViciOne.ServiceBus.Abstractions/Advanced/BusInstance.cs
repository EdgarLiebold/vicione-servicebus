using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a typed bus facade for an additional bus registered in the same dependency-injection container.</summary>
/// <typeparam name="TBus">The interface that uniquely identifies the bus registration.</typeparam>
public abstract class BusInstance<TBus> :
    IBusControl,
    IAdvancedPublishEndpoint,
    IMessageRouteProvider
    where TBus : class, IBus
{
    readonly IBusControl _busControl;

    /// <summary>Creates a typed facade over a bus control.</summary>
    /// <param name="busControl">The bus control that performs all operations.</param>
    protected BusInstance(IBusControl busControl)
    {
        ArgumentNullException.ThrowIfNull(busControl);
        _busControl = busControl;
    }

    /// <summary>Subscribes an observer to publish pipeline notifications.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _busControl.ConnectPublishObserver(observer);
    }

    /// <summary>Publishes a message using its compile-time contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, cancellationToken);
    }

    /// <summary>Publishes a message through a typed publish pipeline.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishPipe">The pipeline that configures the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a typed message through an untyped publish pipeline.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishPipe">The pipeline that configures the publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(message, publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message using its runtime type as the contract.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync(object message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _busControl.Advanced().PublishAsync(message, message.GetType(), cancellationToken);
    }

    /// <summary>Publishes a message using its runtime type through a publish pipeline.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="publishPipe">The pipeline that configures the publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _busControl.Advanced().PublishAsync(message, message.GetType(), publishPipe, cancellationToken);
    }

    /// <summary>Publishes a message as an explicitly selected contract.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
    {
        return _busControl.PublishAsync(message, messageType, cancellationToken);
    }

    /// <summary>Publishes a message as an explicitly selected contract through a publish pipeline.</summary>
    /// <param name="message">The message to publish.</param>
    /// <param name="messageType">The message contract exposed to consumers.</param>
    /// <param name="publishPipe">The pipeline that configures the publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the message has been sent to the transport.</returns>
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
    {
        return _busControl.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    /// <summary>Initializes and publishes a message contract from property values.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public Task PublishAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync<T>(values, cancellationToken);
    }

    /// <summary>Initializes and publishes a message contract through a typed publish pipeline.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="publishPipe">The pipeline that configures the typed publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync(values, publishPipe, cancellationToken);
    }

    /// <summary>Initializes and publishes a message contract through an untyped publish pipeline.</summary>
    /// <typeparam name="T">The message contract to initialize.</typeparam>
    /// <param name="values">The object whose public properties supply message values.</param>
    /// <param name="publishPipe">The pipeline that configures the publish context.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the initialized message has been sent to the transport.</returns>
    public Task PublishAsync<T>(object values, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    /// <summary>Resolves the publish destination for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the publish send endpoint.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _busControl.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>Subscribes an observer to send pipeline notifications.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _busControl.ConnectSendObserver(observer);
    }

    /// <summary>Resolves an endpoint that sends messages to a destination.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the send endpoint.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _busControl.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>Connects a pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _busControl.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects a configurable pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <param name="options">The settings that control pipe connection and scheduling.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _busControl.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects a pipeline for a specific request and response message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request correlation identifier.</param>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _busControl.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Subscribes a typed observer to consume pipeline notifications.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _busControl.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Subscribes an observer to consume pipeline notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _busControl.ConnectConsumeObserver(observer);
    }

    /// <summary>Subscribes an observer to receive transport notifications.</summary>
    /// <param name="observer">The observer that receives transport notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _busControl.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes an observer to receive-endpoint lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives endpoint lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _busControl.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Subscribes an observer to receive-endpoint configuration notifications.</summary>
    /// <param name="observer">The observer that receives endpoint configuration notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _busControl.ConnectEndpointConfigurationObserver(observer);
    }

    /// <summary>Creates and starts a receive endpoint from an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the bus default.</param>
    /// <param name="configureEndpoint">An optional callback that augments endpoint configuration.</param>
    /// <returns>A handle that exposes readiness and controls the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return _busControl.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>Creates and starts a receive endpoint on a named queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures the endpoint.</param>
    /// <returns>A handle that exposes readiness and controls the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _busControl.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>Adds diagnostics from the underlying bus to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        _busControl.Probe(context);
    }

    /// <summary>Gets the base address of the underlying bus.</summary>
    public Uri Address => _busControl.Address;

    /// <summary>Gets the topology of the underlying bus.</summary>
    public IBusTopology Topology => _busControl.Topology;

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _busControl is IMessageRouteProvider routeProvider
        ? routeProvider.MessageRoutes
        : throw new ConfigurationException(
            global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Bus Instance", "unknown", $"The wrapped bus control {_busControl.GetType().Name} does not expose its message routes.", "Correct the named configuration before starting the host"));

    /// <summary>Starts the underlying bus and its configured endpoints.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A task that completes when the bus has started.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return _busControl.StartAsync(cancellationToken);
    }

    /// <summary>Stops the underlying bus and its endpoints.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes when the bus has stopped.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _busControl.StopAsync(cancellationToken);
    }

    /// <summary>Creates a health snapshot of the underlying bus and its endpoints.</summary>
    /// <returns>The current bus health snapshot.</returns>
    public BusHealthResult CheckHealth()
    {
        return _busControl.CheckHealth();
    }
}
