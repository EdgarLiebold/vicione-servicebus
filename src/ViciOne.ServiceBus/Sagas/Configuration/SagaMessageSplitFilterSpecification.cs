using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga connector implementation.
/// </summary>
public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Provides a saga message split filter specification implementation.
    /// </summary>
    public class SagaMessageSplitFilterSpecification :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<ConsumeContext<TMessage>> _specification;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="specification">The specification value.</param>
        public SagaMessageSplitFilterSpecification(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            _specification = specification;
        }

        /// <summary>
        /// Applies this specification to the target builder.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(new BuilderProxy(builder));
        }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The result of the operation.</returns>
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
