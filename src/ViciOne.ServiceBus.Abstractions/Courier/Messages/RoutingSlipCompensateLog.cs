using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>
/// Provides a routing slip compensate log implementation.
/// </summary>
[Serializable]
public class RoutingSlipCompensateLog :
    CompensateLog
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RoutingSlipCompensateLog()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="executionId">The execution id value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="data">The data value.</param>
    public RoutingSlipCompensateLog(Guid executionId, Uri address, IDictionary<string, object> data)
    {
        ExecutionId = executionId;
        Address = address;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="compensateLog">The compensate log value.</param>
    [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition")]
    public RoutingSlipCompensateLog(CompensateLog compensateLog)
    {
        if (compensateLog.Address == null)
            throw new SerializationException("An CompensateLog CompensateAddress is required");

        ExecutionId = compensateLog.ExecutionId;
        Address = compensateLog.Address;
        Data = compensateLog.Data ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the execution id value.
    /// </summary>
    public Guid ExecutionId { get; set; }
    /// <summary>
    /// Gets or sets the address value.
    /// </summary>
    public Uri Address { get; set; } = null!;
    /// <summary>
    /// Gets or sets the data value.
    /// </summary>
    public IDictionary<string, object> Data { get; set; } = null!;
}
