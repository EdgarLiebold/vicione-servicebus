using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface AllocateJobSlot
{
    Guid JobTypeId { get; }

    TimeSpan JobTimeout { get; }

    Guid JobId { get; }

    /// <summary>
    /// The job properties
    /// </summary>
    Dictionary<string, object>? JobProperties { get; }
}
