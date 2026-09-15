using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Adapts message-context filters into a saga/message-context pipeline.</summary>
    public class SagaMessageSplitFilterSpecification :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<ConsumeContext<TMessage>> _specification;

        /// <summary>Associates the message-context specification to adapt.</summary>
        /// <param name="specification">The specification supplying filters and validation results.</param>
        public SagaMessageSplitFilterSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            _specification = specification;
        }

        /// <summary>Applies the wrapped specification through a builder that adds saga-message split filters.</summary>
        /// <param name="builder">The saga/message-context builder receiving the adapted filters.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(new BuilderProxy(builder));
        }

        /// <summary>Enumerates validation results from the wrapped message specification.</summary>
        /// <returns>The wrapped specification's results in their original order.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            foreach (var validationResult in _specification.Validate())
                yield return validationResult;
        }


        class BuilderProxy :
            IPipeBuilder<ConsumeContext<TMessage>>
        {
            readonly IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> _builder;

            public BuilderProxy(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
            {
                _builder = builder;
            }

            public void AddFilter(IFilter<ConsumeContext<TMessage>> filter)
            {
                _builder.AddFilter(new SagaMessageSplitFilter<TSaga, TMessage>(filter));
            }
        }
    }
}
