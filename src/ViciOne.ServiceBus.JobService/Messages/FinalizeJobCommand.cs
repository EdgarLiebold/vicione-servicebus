using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request used to release a terminal job from coordination.</summary>
internal sealed class FinalizeJobCommand :
    FinalizeJob
{
    public Guid JobId { get; set; }
}
