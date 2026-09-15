using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Adapts saga-only or message-only specifications into a saga/message-context pipeline.</summary>
    public class SagaPipeSpecificationProxy :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga, TMessage>> _specification;

        /// <summary>Creates a proxy using a required saga-context specification.</summary>
        /// <param name="specification">The specification whose filters receive the saga-only context view.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaSplitFilterSpecification(specification);
        }

        /// <summary>Creates a proxy using a required message-context specification.</summary>
        /// <param name="specification">The specification whose filters receive the message-only context view.</param>
        public SagaPipeSpecificationProxy(IPipeSpecification<ConsumeContext<TMessage>> specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            _specification = new SagaMessageSplitFilterSpecification(specification);
        }

        /// <summary>Applies the selected split-filter specification to the saga/message builder.</summary>
        /// <param name="builder">The builder receiving adapted context filters.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(builder);
        }

        /// <summary>Returns validation from the selected split-filter specification.</summary>
        /// <returns>The underlying specification's validation sequence.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate();
        }
    }
}
