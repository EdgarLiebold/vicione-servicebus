namespace ViciOne.ServiceBus
{
    using System;
    using System.Collections.Generic;


    public interface EventHubConsumeContext :
        PartitionKeyConsumeContext
    {
        DateTimeOffset EnqueuedTime { get; }

        string OffsetString { get; }
        string PartitionId { get; }
        long SequenceNumber { get; }
        IReadOnlyDictionary<string, object> SystemProperties { get; }
        IDictionary<string, object> Properties { get; }
    }
}
