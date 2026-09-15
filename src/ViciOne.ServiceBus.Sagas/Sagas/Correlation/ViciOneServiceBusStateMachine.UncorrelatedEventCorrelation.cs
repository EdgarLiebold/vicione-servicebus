using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Represents a message event with no saga correlation and reports that configuration as invalid.</summary>
    /// <typeparam name="TData">The uncorrelated event's message contract.</typeparam>
    public class UncorrelatedEventCorrelation<TData> :
        IEventCorrelation<TInstance, TData>
        where TData : class
    {
        /// <summary>Identifies the event whose required correlation is missing.</summary>
        /// <param name="event">The message event reported by validation.</param>
        public UncorrelatedEventCorrelation(IEvent<TData> @event)
        {
            Event = @event;
        }

        /// <summary>Gets <see langword="null" /> because no saga filter factory is configured.</summary>
        public SagaFilterFactory<TInstance, TData>? FilterFactory => null;

        /// <summary>Gets the message event whose correlation is missing.</summary>
        public IEvent<TData> Event { get; }

        Type IEventCorrelation.DataType => typeof(TData);

        /// <summary>Gets <see langword="false" /> because this invalid correlation does not request consume topology.</summary>
        public bool ConfigureConsumeTopology => false;

        /// <summary>Gets <see langword="null" /> because no message filter is configured.</summary>
        public IFilter<ConsumeContext<TData>>? MessageFilter => null;

        /// <summary>Gets <see langword="null" /> because no saga policy is configured.</summary>
        public ISagaPolicy<TInstance, TData>? Policy => null;

        /// <summary>Reports the missing correlation for this event.</summary>
        /// <returns>A single failure identifying the event and its unspecified correlation.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(Event.Name, "Correlation", "was not specified");
        }
    }
}
