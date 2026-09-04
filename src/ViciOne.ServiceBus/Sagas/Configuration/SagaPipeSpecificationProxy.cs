using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga connector implementation.
/// </summary>
public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Provides a saga pipe specification proxy implementation.
    /// </summary>
    public class SagaPipeSpecificationProxy :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga, TMessage>> _specification;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="specification">The specification value.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaSplitFilterSpecification(specification);
        }

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="specification">The specification value.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaMessageSplitFilterSpecification(specification);
        }

        /// <summary>
        /// Applies this specification to the target builder.
        /// </summary>
        /// <param name="builder">The builder value.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(builder);
        }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate();
        }
    }
}
