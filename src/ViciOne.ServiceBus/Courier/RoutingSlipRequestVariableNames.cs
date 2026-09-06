namespace ViciOne.ServiceBus.Courier;

/// <summary>Defines canonical names for routing slip request variable.</summary>
public static class RoutingSlipRequestVariableNames
{
    /// <summary>Exposes the request id used by the containing type.</summary>
    public const string RequestId = "RequestId";
    /// <summary>Exposes the request used by the containing type.</summary>
    public const string Request = "Request";
    /// <summary>Exposes the fault address used by the containing type.</summary>
    public const string FaultAddress = "FaultAddress";
    /// <summary>Exposes the response address used by the containing type.</summary>
    public const string ResponseAddress = "ResponseAddress";
    /// <summary>Exposes the request address used by the containing type.</summary>
    public const string RequestAddress = "RequestAddress";
    /// <summary>Exposes the retry attempt used by the containing type.</summary>
    public const string RetryAttempt = "RetryAttempt";
}
