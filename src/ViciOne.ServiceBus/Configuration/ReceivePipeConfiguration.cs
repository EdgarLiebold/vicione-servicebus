using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates receive pipe configuration.</summary>
public class ReceivePipeConfiguration :
    IReceivePipeConfiguration,
    IReceivePipeConfigurator,
    ISpecification
{
    readonly IBuildPipeConfigurator<ReceiveContext> _configurator;
    bool _created;

    /// <summary>Initializes a new instance.</summary>
    public ReceivePipeConfiguration()
    {
        _configurator = new PipeConfigurator<ReceiveContext>();
        DeadLetterConfigurator = new PipeConfigurator<ReceiveContext>();
        ErrorConfigurator = new PipeConfigurator<ExceptionReceiveContext>();
    }

    /// <summary>Gets the specification.</summary>
    public ISpecification Specification => _configurator;

    /// <summary>Gets the configurator.</summary>
    public IReceivePipeConfigurator Configurator => this;

    /// <summary>Gets the dead letter configurator.</summary>
    public IBuildPipeConfigurator<ReceiveContext> DeadLetterConfigurator { get; }

    /// <summary>Gets the error configurator.</summary>
    public IBuildPipeConfigurator<ExceptionReceiveContext> ErrorConfigurator { get; }

    /// <summary>Creates pipe.</summary>
    /// <param name="consumePipe">The consume pipe.</param>
    /// <param name="serializers">The serializers.</param>
    /// <returns>The created pipe.</returns>
    public IReceivePipe CreatePipe(IConsumePipe consumePipe, ISerialization serializers)
    {
        if (_created)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive Pipe", "unknown", "The ReceivePipeConfiguration can only be used once.", "Correct the named configuration before starting the host"));

        _configurator.UseDeadLetter(CreateDeadLetterPipe());
        _configurator.UseRescue(CreateErrorPipe(), x =>
        {
            x.Ignore<OperationCanceledException>();
        });

        _configurator.UseFilter(new DeserializeFilter(serializers, consumePipe));

        _created = true;

        return new ReceivePipe(_configurator.Build(), consumePipe);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ReceiveContext> specification)
    {
        _configurator.AddPipeSpecification(specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _configurator.Validate()
            .Concat(DeadLetterConfigurator.Validate())
            .Concat(ErrorConfigurator.Validate());
    }

    IPipe<ReceiveContext> CreateDeadLetterPipe()
    {
        IPipe<ReceiveContext> deadLetterPipe = DeadLetterConfigurator.Build();
        if (deadLetterPipe.IsNotEmpty())
            return deadLetterPipe;

        DeadLetterConfigurator.UseFilter(new DeadLetterTransportFilter());

        return DeadLetterConfigurator.Build();
    }

    IPipe<ExceptionReceiveContext> CreateErrorPipe()
    {
        IPipe<ExceptionReceiveContext> errorPipe = ErrorConfigurator.Build();
        if (errorPipe.IsNotEmpty())
            return errorPipe;

        ErrorConfigurator.UseFilter(new GenerateFaultFilter());
        ErrorConfigurator.UseFilter(new ErrorTransportFilter());

        return ErrorConfigurator.Build();
    }
}
