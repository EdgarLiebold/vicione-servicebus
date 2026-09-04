using System;
using System.Linq;

namespace ViciOne.ServiceBus;

[Serializable]
public class RequestFaultException :
    RequestException
{
    public RequestFaultException(string requestType, Fault fault)
        : base($"The {requestType} request faulted: {string.Join(Environment.NewLine, fault.Exceptions?.Select(x => x.Message) ?? [])}")
    {
        RequestType = requestType;
        Fault = fault;
    }

    public RequestFaultException()
    {
    }

    public string? RequestType { get; private set; }
    public Fault? Fault { get; private set; }
}
