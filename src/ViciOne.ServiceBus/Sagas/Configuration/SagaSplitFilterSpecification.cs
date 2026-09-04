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
    /// Provides a saga split filter specification implementation.
    /// </summary>
    public class SagaSplitFilterSpecification :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga>> _specification;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="specification">The specification value.</param>
        public SagaSplitFilterSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
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
            return _specification.Validate();
        }


        class BuilderProxy :
            IPipeBuilder<SagaConsumeContext<TSaga>>
        {
            readonly IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> _builder;

            public BuilderProxy(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
            {
                _builder = builder;
            }

            public void AddFilter(IFilter<SagaConsumeContext<TSaga>> filter)
            {
                _builder.AddFilter(new SagaSplitFilter<TSaga, TMessage>(filter));
            }
        }
    }
}
