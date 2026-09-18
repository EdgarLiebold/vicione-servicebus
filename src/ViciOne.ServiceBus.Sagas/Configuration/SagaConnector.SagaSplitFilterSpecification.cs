using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Adapts saga-context filters into a saga/message-context pipeline.</summary>
    public class SagaSplitFilterSpecification :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga>> _specification;

        /// <summary>Associates the saga-context specification to adapt.</summary>
        /// <param name="specification">The specification supplying filters and validation results.</param>
        public SagaSplitFilterSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        /// <summary>Applies the wrapped specification through a builder that adds saga split filters.</summary>
        /// <param name="builder">The saga/message-context builder receiving the adapted filters.</param>
        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            _specification.Apply(new BuilderProxy(builder));
        }

        /// <summary>Returns validation results from the wrapped saga specification.</summary>
        /// <returns>The wrapped specification's validation sequence.</returns>
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
                _builder = builder ?? throw new ArgumentNullException(nameof(builder));
            }

            public void AddFilter(IFilter<SagaConsumeContext<TSaga>> filter)
            {
                ArgumentNullException.ThrowIfNull(filter);

                _builder.AddFilter(new SagaSplitFilter<TSaga, TMessage>(filter));
            }
        }
    }
}
