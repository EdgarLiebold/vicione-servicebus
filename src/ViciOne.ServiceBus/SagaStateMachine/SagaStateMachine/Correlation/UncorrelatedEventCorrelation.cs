// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Generic;


    public partial class ViciOneServiceBusStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        public class UncorrelatedEventCorrelation<TData> :
            EventCorrelation<TInstance, TData>
            where TData : class
        {
            public UncorrelatedEventCorrelation(Event<TData> @event)
            {
                Event = @event;
            }

            public SagaFilterFactory<TInstance, TData> FilterFactory => null;

            public Event<TData> Event { get; }

            Type EventCorrelation.DataType => typeof(TData);

            public bool ConfigureConsumeTopology => false;

            public IFilter<ConsumeContext<TData>> MessageFilter => null;

            public ISagaPolicy<TInstance, TData> Policy => null;

            public IEnumerable<ValidationResult> Validate()
            {
                yield return this.Failure(Event.Name, "Correlation", "was not specified");
            }
        }
    }
}
