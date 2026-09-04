using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    public class SagaSplitFilterSpecification :
        IPipeSpecification<SagaConsumeContext<TSaga, TMessage>>
    {
        readonly IPipeSpecification<SagaConsumeContext<TSaga>> _specification;

        public SagaSplitFilterSpecification(IPipeSpecification<SagaConsumeContext<TSaga>> specification)
        {
            _specification = specification;
        }

        public void Apply(IPipeBuilder<SagaConsumeContext<TSaga, TMessage>> builder)
        {
            _specification.Apply(new BuilderProxy(builder));
        }

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
