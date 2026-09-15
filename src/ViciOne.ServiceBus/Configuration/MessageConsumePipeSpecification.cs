using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Composes parent-message, message-specific and base consume specifications.</summary>
/// <typeparam name="TMessage">The message contract handled by the consume pipeline.</typeparam>
public class MessageConsumePipeSpecification<TMessage> :
    IMessageConsumePipeSpecification<TMessage>,
    IMessageConsumePipeSpecification
    where TMessage : class
{
    readonly List<IPipeSpecification<ConsumeContext>> _baseSpecifications;
    readonly List<ISpecificationPipeSpecification<ConsumeContext<TMessage>>> _parentMessageSpecifications;
    readonly List<IPipeSpecification<ConsumeContext<TMessage>>> _specifications;

    /// <summary>Creates an empty configuration for each consume specification layer.</summary>
    public MessageConsumePipeSpecification()
    {
        _specifications = new List<IPipeSpecification<ConsumeContext<TMessage>>>();
        _baseSpecifications = new List<IPipeSpecification<ConsumeContext>>();
        _parentMessageSpecifications = new List<ISpecificationPipeSpecification<ConsumeContext<TMessage>>>();
    }

    /// <summary>Appends a base consume-context specification.</summary>
    /// <param name="specification">The specification applied after message-specific filters unless the builder is marked implemented.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext> specification)
    {
        _baseSpecifications.Add(specification);
    }

    IMessageConsumePipeSpecification<T> IMessageConsumePipeSpecification.GetMessageSpecification<T>()
    {
        if (this is IMessageConsumePipeSpecification<T> result)
            return result;

        throw new ArgumentException($"The expected message type was invalid: {TypeCache<T>.ShortName}");
    }

    /// <summary>Appends a specification for this message's consume context.</summary>
    /// <param name="specification">The specification applied after parent-message specifications.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _specifications.Add(specification);
    }

    /// <summary>Enumerates validation results from the message-specific specifications.</summary>
    /// <returns>The results produced by the message-specific layer, in registration order.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specifications.SelectMany(x => x.Validate());
    }

    /// <summary>Applies parent and message specifications, then base specifications when the builder is not marked implemented.</summary>
    /// <param name="builder">The builder receiving each specification's filters.</param>
    public void Apply(ISpecificationPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        var parentCount = _parentMessageSpecifications.Count;
        if (parentCount > 0)
        {
            ISpecificationPipeBuilder<ConsumeContext<TMessage>> delegatedBuilder = builder.CreateDelegatedBuilder();

            for (var index = 0; index < parentCount; index++)
                _parentMessageSpecifications[index].Apply(delegatedBuilder);
        }

        for (var index = 0; index < _specifications.Count; index++)
            _specifications[index].Apply(builder);

        if (!builder.IsImplemented)
        {
            for (var index = 0; index < _baseSpecifications.Count; index++)
            {
                var split = new PipeConfigurator<ConsumeContext<TMessage>>.SplitFilterPipeSpecification<ConsumeContext>(_baseSpecifications[index],
                    MergeContext, FilterContext);

                split.Apply(builder);
            }
        }
    }

    /// <summary>Builds the consume specification layers before the supplied continuation.</summary>
    /// <param name="pipe">The continuation invoked after the configured filters.</param>
    /// <returns>The composed pipeline, or the supplied continuation when no filters are registered.</returns>
    public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(IPipe<ConsumeContext<TMessage>> pipe)
    {
        var pipeBuilder = new PipeConfigurator<ConsumeContext<TMessage>>.SpecificationPipeBuilder();

        Apply(pipeBuilder);

        return pipeBuilder.Build(pipe);
    }

    /// <summary>Appends a parent-message specification applied through a delegated builder.</summary>
    /// <param name="parentSpecification">The specification applied before this message's own specifications.</param>
    public void AddParentMessageSpecification(ISpecificationPipeSpecification<ConsumeContext<TMessage>> parentSpecification)
    {
        _parentMessageSpecifications.Add(parentSpecification);
    }

    static ConsumeContext FilterContext(ConsumeContext<TMessage> context)
    {
        return context.Advanced();
    }

    static ConsumeContext<TMessage> MergeContext(ConsumeContext<TMessage> input, ConsumeContext context)
    {
        var result = context as ConsumeContext<TMessage>;

        return result ?? new MessageConsumeContext<TMessage>(context, input.Message);
    }
}
