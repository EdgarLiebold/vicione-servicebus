using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Represents a routing slip request info value.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public readonly struct RoutingSlipRequestInfo<T>
    where T : class
{
    /// <summary>
    /// Defines the request id value.
    /// </summary>
    public readonly Guid RequestId;
    /// <summary>
    /// Defines the response address value.
    /// </summary>
    public readonly Uri ResponseAddress;
    /// <summary>
    /// Defines the fault address value.
    /// </summary>
    public readonly Uri? FaultAddress;
    /// <summary>
    /// Defines the request address value.
    /// </summary>
    public readonly Uri? RequestAddress;
    /// <summary>
    /// Defines the retry attempt value.
    /// </summary>
    public readonly int? RetryAttempt;
    /// <summary>
    /// Defines the request value.
    /// </summary>
    public readonly T Request;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="variables">The variables value.</param>
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
