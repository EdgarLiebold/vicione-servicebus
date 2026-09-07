using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Restores request metadata carried in private routing-slip variables.</summary>
/// <typeparam name="T">The original request contract.</typeparam>
internal readonly struct RoutingSlipRequestInfo<T>
    where T : class
{
    public Guid RequestId { get; }
    public Uri ResponseAddress { get; }
    public Uri? FaultAddress { get; }
    public Uri? RequestAddress { get; }
    public int? RetryAttempt { get; }
    public T Request { get; }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="variables">The variables.</param>
    public RoutingSlipRequestInfo(IObjectDeserializer context, IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(variables);

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
