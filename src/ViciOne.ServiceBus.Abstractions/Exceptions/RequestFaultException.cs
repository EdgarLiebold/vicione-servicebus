using System;
using System.Linq;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a request completed with a fault response.</summary>
public sealed class RequestFaultException :
    RequestException
{
    /// <summary>Creates an exception from the fault returned for the specified request contract.</summary>
    /// <param name="requestType">The runtime request type used by the operation.</param>
    /// <param name="fault">The fault response returned by the consumer.</param>
    public RequestFaultException(Type requestType, Fault fault)
        : base(FormatMessage(requestType, fault))
    {
        RequestType = requestType;
        Fault = fault;
    }

    /// <summary>Creates a request-fault exception without request or fault context.</summary>
    public RequestFaultException()
    {
    }

    /// <summary>Gets the request contract type associated with the fault, when one was supplied.</summary>
    public Type? RequestType { get; }

    /// <summary>Gets the fault response returned for the request, when one was supplied.</summary>
    public Fault? Fault { get; }

    static string FormatMessage(Type requestType, Fault fault)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(fault);

        return $"The {TypeCache.GetShortName(requestType)} request faulted: "
            + string.Join(Environment.NewLine, fault.Exceptions.Select(exception => exception.Message));
    }
}
