using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;

namespace ViciOne.ServiceBus.Courier.Messages;

/// <summary>Materializes the data required to compensate one completed routing-slip activity.</summary>
internal sealed class RoutingSlipCompensateLog :
    CompensateLog
{
    /// <summary>Creates an empty instance for contract materialization.</summary>
    public RoutingSlipCompensateLog()
    {
    }

    /// <summary>Creates a compensation log with an isolated data snapshot.</summary>
    /// <param name="executionId">The non-empty activity execution identifier.</param>
    /// <param name="address">The compensation endpoint address.</param>
    /// <param name="data">The data required to compensate the activity.</param>
    public RoutingSlipCompensateLog(Guid executionId, Uri address, IDictionary<string, object> data)
    {
        if (executionId == Guid.Empty)
            throw new ArgumentException("The activity execution identifier cannot be empty.", nameof(executionId));
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(data);

        ExecutionId = executionId;
        Address = address;
        Data = Snapshot(data);
    }

    /// <summary>Creates a validated snapshot of received compensation-log data.</summary>
    /// <param name="compensateLog">The received compensation log to copy.</param>
    public RoutingSlipCompensateLog(CompensateLog compensateLog)
    {
        ArgumentNullException.ThrowIfNull(compensateLog);

        if (compensateLog.ExecutionId == Guid.Empty)
            throw new SerializationException("A compensation log requires a non-empty activity execution identifier.");
        if (compensateLog.Address == null)
            throw new SerializationException("A compensation log requires a compensation endpoint address.");

        ExecutionId = compensateLog.ExecutionId;
        Address = compensateLog.Address;
        Data = Snapshot(compensateLog.Data ?? new Dictionary<string, object>());
    }

    /// <summary>Gets or sets the activity execution identifier.</summary>
    public Guid ExecutionId { get; set; }
    /// <summary>Gets or sets the compensation endpoint address.</summary>
    public Uri Address { get; set; } = null!;
    /// <summary>Gets or sets the data required to compensate the activity.</summary>
    public IDictionary<string, object> Data { get; set; } = null!;

    static IDictionary<string, object> Snapshot(IDictionary<string, object> data) =>
        new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(data, StringComparer.OrdinalIgnoreCase));
}
