using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Represents a routing slip request info.</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly struct RoutingSlipRequestInfo<T>
    where T : class
{
    /// <summary>Exposes the request id used by the containing type.</summary>
    public readonly Guid RequestId;
    /// <summary>Exposes the response address used by the containing type.</summary>
    public readonly Uri ResponseAddress;
    /// <summary>Exposes the fault address used by the containing type.</summary>
    public readonly Uri? FaultAddress;
    /// <summary>Exposes the request address used by the containing type.</summary>
    public readonly Uri? RequestAddress;
    /// <summary>Exposes the retry attempt used by the containing type.</summary>
    public readonly int? RetryAttempt;
    /// <summary>Exposes the request used by the containing type.</summary>
    public readonly T Request;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="variables">The variables.</param>
    public RoutingSlipRequestInfo(IObjectDeserializer context, IDictionary<string, object> variables)
    {
        Request = context.GetValue<T>(variables, RoutingSlipRequestVariableNames.Request)
            ?? throw new ArgumentException($"Routing Slip Request variable was not found: {RoutingSlipRequestVariableNames.Request}");

        RequestId = context.GetValue<Guid>(variables, RoutingSlipRequestVariableNames.RequestId)
            ?? throw new ArgumentException($"Routing Slip RequestId variable was not found: {RoutingSlipRequestVariableNames.RequestId}");

        ResponseAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.ResponseAddress)
            ?? throw new ArgumentException($"Routing Slip ResponseAddress variable was not found: {RoutingSlipRequestVariableNames.ResponseAddress}");

        FaultAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.FaultAddress);

        RequestAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.RequestAddress);
        RetryAttempt = context.GetValue<int>(variables, RoutingSlipRequestVariableNames.RetryAttempt);
    }
}
