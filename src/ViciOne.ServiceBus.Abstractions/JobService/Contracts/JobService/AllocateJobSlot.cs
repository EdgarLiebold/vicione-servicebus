namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;
    using System.Collections.Generic;


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
}
