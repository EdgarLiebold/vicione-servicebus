using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to request timeout.
/// </summary>
public class RequestTimeoutException :
    RequestException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RequestTimeoutException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    public RequestTimeoutException(string requestId)
        : base(FormatMessage(requestId))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public RequestTimeoutException(string requestId, Exception innerException)
        : base(FormatMessage(requestId), innerException)
    {
    }

    static string FormatMessage(string requestId)
    {
        return $"Timeout waiting for response, RequestId: {requestId}";
    }
}
