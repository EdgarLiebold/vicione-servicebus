using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable result returned when no execution capacity is available.</summary>
internal sealed class JobSlotUnavailableResponse :
    JobSlotUnavailable
{
    public Guid JobId { get; set; }
}
