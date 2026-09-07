using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Carries the activity log for routing slip compensate.</summary>
internal sealed class RoutingSlipCompensateLog :
    CompensateLog
{
    /// <summary>Initializes a new instance.</summary>
    public RoutingSlipCompensateLog()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="executionId">The execution id.</param>
    /// <param name="address">The address.</param>
    /// <param name="data">The data.</param>
    public RoutingSlipCompensateLog(Guid executionId, Uri address, IDictionary<string, object> data)
    {
        ExecutionId = executionId;
        Address = address;
        Data = data;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="compensateLog">The compensate log.</param>
    public RoutingSlipCompensateLog(CompensateLog compensateLog)
    {
        if (compensateLog.Address == null)
            throw new SerializationException("An CompensateLog CompensateAddress is required");

        ExecutionId = compensateLog.ExecutionId;
        Address = compensateLog.Address;
        Data = compensateLog.Data ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets or sets the execution id.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the address.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the data.</summary>
    public IDictionary<string, object> Data { get; set; } = null!;
}
