using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures and attaches a consume observer to a receive endpoint.</summary>
/// <typeparam name="TMessage">The message contract reported to the observer.</typeparam>
public sealed class ObserverConfigurator<TMessage> :
    IObserverConfigurator<TMessage>,
    IReceiveEndpointSpecification
    where TMessage : class
{
    readonly IPipeSpecification<ConsumeContext<TMessage>> _handlerConfigurator;
    readonly IBuildPipeConfigurator<ConsumeContext<TMessage>> _pipeConfigurator;

    /// <summary>Creates endpoint configuration for a typed consume observer.</summary>
    /// <param name="observer">The observer notified for matching consume contexts.</param>
    public ObserverConfigurator(IObserver<ConsumeContext<TMessage>> observer)
    {
        _pipeConfigurator = new PipeConfigurator<ConsumeContext<TMessage>>();
        _handlerConfigurator = new ObserverPipeSpecification<TMessage>(observer);
    }

    /// <summary>Adds middleware that runs before the observer.</summary>
    /// <param name="specification">The message middleware specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

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
        ArgumentNullException.ThrowIfNull(builder);

        _pipeConfigurator.AddPipeSpecification(_handlerConfigurator);

        IPipe<ConsumeContext<TMessage>> pipe = _pipeConfigurator.Build();

        builder.ConnectConsumePipe(pipe);
    }
}
