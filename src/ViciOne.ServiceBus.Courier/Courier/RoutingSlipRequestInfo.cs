using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Restores request metadata carried in private routing-slip variables.</summary>
/// <typeparam name="T">The original request contract.</typeparam>
internal readonly struct RoutingSlipRequestInfo<T>
    where T : class
{
    /// <summary>Gets the transport request identifier used to correlate the terminal response.</summary>
    public Guid RequestId { get; }
    /// <summary>Gets the endpoint that receives a successful response.</summary>
    public Uri ResponseAddress { get; }
    /// <summary>Gets the endpoint that receives a terminal fault, when it differs from the response endpoint.</summary>
    public Uri? FaultAddress { get; }
    /// <summary>Gets the original request endpoint used when retrying a failed routing slip.</summary>
    public Uri? RequestAddress { get; }
    /// <summary>Gets the zero-based retry attempt carried by the routing slip.</summary>
    public int? RetryAttempt { get; }
    /// <summary>Gets the original request message.</summary>
    public T Request { get; }

    /// <summary>Restores and validates request metadata from routing-slip variables.</summary>
    /// <param name="context">The deserializer that converts stored metadata values.</param>
    /// <param name="variables">The routing-slip variables containing request metadata.</param>
    public RoutingSlipRequestInfo(IObjectDeserializer context, IReadOnlyDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(variables);

        Request = context.GetValue<T>(variables, RoutingSlipRequestVariableNames.Request)
            ?? throw Missing(RoutingSlipRequestVariableNames.Request);

        RequestId = context.GetValue<Guid>(variables, RoutingSlipRequestVariableNames.RequestId)
            ?? throw Missing(RoutingSlipRequestVariableNames.RequestId);
        if (RequestId == Guid.Empty)
            throw Invalid(RoutingSlipRequestVariableNames.RequestId, "must be a non-empty identifier");

        ResponseAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.ResponseAddress)
            ?? throw Missing(RoutingSlipRequestVariableNames.ResponseAddress);
        ValidateAddress(ResponseAddress, RoutingSlipRequestVariableNames.ResponseAddress);

        FaultAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.FaultAddress);
        if (FaultAddress is not null)
            ValidateAddress(FaultAddress, RoutingSlipRequestVariableNames.FaultAddress);

        RequestAddress = context.GetValue<Uri>(variables, RoutingSlipRequestVariableNames.RequestAddress);
        if (RequestAddress is not null)
            ValidateAddress(RequestAddress, RoutingSlipRequestVariableNames.RequestAddress);

        RetryAttempt = context.GetValue<int>(variables, RoutingSlipRequestVariableNames.RetryAttempt);
        if (RetryAttempt < 0)
            throw Invalid(RoutingSlipRequestVariableNames.RetryAttempt, "cannot be negative");
    }

    static void ValidateAddress(Uri address, string variableName)
    {
        if (!address.IsAbsoluteUri)
            throw Invalid(variableName, "must be an absolute endpoint address");
    }

    static SerializationException Missing(string variableName) =>
        new($"The routing slip does not contain the required '{variableName}' request variable.");

    static SerializationException Invalid(string variableName, string reason) =>
        new($"The routing-slip request variable '{variableName}' {reason}.");
}
