// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;


    public class DelayedRedeliveryConfigurationObserver :
        ScheduledRedeliveryConfigurationObserver
    {
        public DelayedRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
            : base(configurator, configure)
        {
        }

        protected override IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
        {
            var redeliverySpecification = new DelayedRedeliveryPipeSpecification<TMessage>();

            configurator.AddPipeSpecification(redeliverySpecification);

            return redeliverySpecification;
        }

        public void Method7()
        {
        }

        public void Method8()
        {
        }

        public void Method9()
        {
        }
    }
}
