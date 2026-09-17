using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Records the data and endpoint required to compensate a completed activity.</summary>
public interface ICompensateLog
{
    /// <summary>The identifier of the completed activity execution.</summary>
    Guid ExecutionId { get; }

    /// <summary>The compensation address where the routing slip should be sent for compensation.</summary>
    Uri Address { get; }

    /// <summary>The results of the activity saved for compensation.</summary>
    IReadOnlyDictionary<string, object> Data { get; }
}
