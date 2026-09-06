using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for message consume pipe.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MessageConsumePipeSpecification<TMessage> :
    IMessageConsumePipeSpecification<TMessage>,
    IMessageConsumePipeSpecification
    where TMessage : class
{
    readonly List<IPipeSpecification<ConsumeContext>> _baseSpecifications;
    readonly List<ISpecificationPipeSpecification<ConsumeContext<TMessage>>> _parentMessageSpecifications;
    readonly List<IPipeSpecification<ConsumeContext<TMessage>>> _specifications;

    /// <summary>Initializes a new instance.</summary>
    public MessageConsumePipeSpecification()
    {
        _specifications = new List<IPipeSpecification<ConsumeContext<TMessage>>>();
        _baseSpecifications = new List<IPipeSpecification<ConsumeContext>>();
        _parentMessageSpecifications = new List<ISpecificationPipeSpecification<ConsumeContext<TMessage>>>();
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
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

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
    {
        _specifications.Add(specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _specifications.SelectMany(x => x.Validate());
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
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

    /// <summary>Builds message pipe.</summary>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>The configured message pipe.</returns>
    public IPipe<ConsumeContext<TMessage>> BuildMessagePipe(IPipe<ConsumeContext<TMessage>> pipe)
    {
        var pipeBuilder = new PipeConfigurator<ConsumeContext<TMessage>>.SpecificationPipeBuilder();

        Apply(pipeBuilder);

        return pipeBuilder.Build(pipe);
    }

    /// <summary>Adds parent message specification to the configuration.</summary>
    /// <param name="parentSpecification">The parent specification.</param>
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
