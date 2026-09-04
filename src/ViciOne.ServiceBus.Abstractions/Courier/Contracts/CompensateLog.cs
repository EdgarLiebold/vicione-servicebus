using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

public interface CompensateLog
{
    /// <summary>
    /// The tracking number for completion of the activity
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// The compensation address where the routing slip should be sent for compensation
    /// </summary>
    Uri Address { get; }

    /// <summary>
    /// The results of the activity saved for compensation
    /// </summary>
    IDictionary<string, object> Data { get; }
}
