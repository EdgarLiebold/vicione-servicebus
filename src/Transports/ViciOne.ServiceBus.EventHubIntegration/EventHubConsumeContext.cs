using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

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
