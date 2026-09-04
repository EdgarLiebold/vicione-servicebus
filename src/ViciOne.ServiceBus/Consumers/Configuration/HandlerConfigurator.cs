using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Connects a handler to the inbound pipe of the receive endpoint
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class HandlerConfigurator<TMessage> :
    IHandlerConfigurator<TMessage>,
    IReceiveEndpointSpecification
    where TMessage : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _handlerConfigurator;
    readonly HandlerConfigurationObservable _observers;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _pipeConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="handler">The handler value.</param>
    /// <param name="observer">The observer value.</param>
    public HandlerConfigurator(MessageHandler<TMessage> handler, IHandlerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(observer);

        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _handlerConfigurator = new HandlerPipeSpecification<TMessage>(handler);
        _observers = new HandlerConfigurationObservable();

        _observers.Connect(observer);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects handler configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectHandlerConfigurationObserver(IHandlerConfigurationObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.HandlerConfigured(this)));

        return _handlerConfigurator.Validate()
            .Concat(_pipeConfigurator.Validate())
            .ToArray();
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _pipeConfigurator.AddPipeSpecification(_handlerConfigurator);

        IPipe<ConsumeContext<TMessage>> pipe = _pipeConfigurator.Build();

        builder.ConnectConsumePipe(pipe);
    }
}
