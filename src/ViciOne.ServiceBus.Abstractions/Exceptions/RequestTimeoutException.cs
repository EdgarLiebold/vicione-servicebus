using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a request did not receive a response before its timeout expired.</summary>
public class RequestTimeoutException :
    RequestException
{
    /// <summary>Creates a timeout exception without a request identifier.</summary>
    public RequestTimeoutException()
    {
    }

    /// <summary>Creates a timeout exception for the specified request.</summary>
    /// <param name="requestId">The identifier of the timed-out request.</param>
    public RequestTimeoutException(Guid requestId)
        : base(FormatMessage(requestId))
    {
        RequestId = requestId;
    }

    /// <summary>Creates a timeout exception for the specified request and underlying failure.</summary>
    /// <param name="requestId">The identifier of the timed-out request.</param>
    /// <param name="innerException">The exception that caused the request to time out.</param>
    public RequestTimeoutException(Guid requestId, Exception innerException)
        : base(FormatMessage(requestId), innerException)
    {
        RequestId = requestId;
    }

    /// <summary>Gets the identifier of the timed-out request, when one was supplied.</summary>
    public Guid? RequestId { get; }

    static string FormatMessage(Guid requestId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(requestId), requestId, "The request identifier cannot be empty.");

        return $"Timeout waiting for response, RequestId: {requestId:D}";
    }
}
