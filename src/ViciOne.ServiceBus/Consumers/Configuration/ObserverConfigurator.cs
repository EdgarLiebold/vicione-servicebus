using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an observer configurator implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ObserverConfigurator<TMessage> :
    IObserverConfigurator<TMessage>,
    IReceiveEndpointSpecification
    where TMessage : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _handlerConfigurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _pipeConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    public ObserverConfigurator(IObserver<ConsumeContext<TMessage>> observer)
    {
        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _handlerConfigurator = new ObserverPipeSpecification<TMessage>(observer);
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
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _handlerConfigurator.Validate().Concat(_pipeConfigurator.Validate());
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        _pipeConfigurator.AddPipeSpecification(_handlerConfigurator);

        IPipe<ConsumeContext<TMessage>> pipe = _pipeConfigurator.Build();

        builder.ConnectConsumePipe(pipe);
    }
}
