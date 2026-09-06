using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Forwards saga pipe specification operations to an underlying context.</summary>
    public class SagaPipeSpecificationProxy :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga, TMessage>> _specification;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="specification">The specification.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaSplitFilterSpecification(specification);
        }

        /// <summary>Initializes a new instance.</summary>
        /// <param name="specification">The specification.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaMessageSplitFilterSpecification(specification);
        }

        /// <summary>Applies this specification to the target builder.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(builder);
        }

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate();
        }
    }
}
