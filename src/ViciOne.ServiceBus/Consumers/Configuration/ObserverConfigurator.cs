using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures observer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ObserverConfigurator<TMessage> :
    IObserverConfigurator<TMessage>,
    IReceiveEndpointSpecification
    where TMessage : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _handlerConfigurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _pipeConfigurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="observer">The observer to connect.</param>
    public ObserverConfigurator(IObserver<ConsumeContext<TMessage>> observer)
    {
        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _handlerConfigurator = new ObserverPipeSpecification<TMessage>(observer);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _handlerConfigurator.Validate().Concat(_pipeConfigurator.Validate());
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        _pipeConfigurator.AddPipeSpecification(_handlerConfigurator);

        IPipe<ConsumeContext<TMessage>> pipe = _pipeConfigurator.Build();

        builder.ConnectConsumePipe(pipe);
    }
}
