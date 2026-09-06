using System;
using System.Linq;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to request fault.
/// </summary>
public class RequestFaultException :
    RequestException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="requestType">The request type value.</param>
    /// <param name="fault">The fault value.</param>
    public RequestFaultException(string requestType, Fault fault)
        : base($"The {requestType} request faulted: {string.Join(Environment.NewLine, fault.Exceptions?.Select(x => x.Message) ?? [])}")
    {
        RequestType = requestType;
        Fault = fault;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RequestFaultException()
    {
    }

    /// <summary>
    /// Gets or sets the request type value.
    /// </summary>
    public string? RequestType { get; private set; }
    /// <summary>
    /// Gets or sets the fault value.
    /// </summary>
    public Fault? Fault { get; private set; }
}
