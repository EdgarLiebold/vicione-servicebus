using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines persisted state that is uniquely addressable as a saga instance.</summary>
[ConsumerRegistrationExclusion]
public interface ISaga
{
    /// <summary>Gets or sets the identifier that uniquely correlates the saga instance.</summary>
    Guid CorrelationId { get; set; }
}
