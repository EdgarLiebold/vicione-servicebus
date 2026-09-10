using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a handler to the inbound pipe of the receive endpoint.</summary>
/// <typeparam name="TMessage">The message contract handled by the delegate.</typeparam>
public sealed class HandlerConfigurator<TMessage> :
    IHandlerConfigurator<TMessage>,
    IReceiveEndpointSpecification
    where TMessage : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _handlerConfigurator;
    readonly HandlerConfigurationObservable _observers;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _pipeConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates endpoint configuration for a message-handler delegate.</summary>
    /// <param name="handler">The delegate invoked for each matching message.</param>
    /// <param name="observer">The endpoint observer notified as handler configuration is completed.</param>
    public HandlerConfigurator(MessageHandler<TMessage> handler, IHandlerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(observer);

        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _handlerConfigurator = new HandlerPipeSpecification<TMessage>(handler);
        _observers = new HandlerConfigurationObservable();

        _observers.Connect(observer);
    }

    /// <summary>Adds middleware that runs before the handler delegate.</summary>
    /// <param name="specification">The message middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Connects handler configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.HandlerConfigured(this)));

        return _handlerConfigurator.Validate()
            .Concat(_pipeConfigurator.Validate())
            .ToArray();
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _pipeConfigurator.AddPipeSpecification(_handlerConfigurator);

        IPipe<ConsumeContext<TMessage>> pipe = _pipeConfigurator.Build();

        builder.ConnectConsumePipe(pipe);
    }
}
