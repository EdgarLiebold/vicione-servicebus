using System;
using System.Threading;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to request canceled.</summary>
public class RequestCanceledException :
    OperationCanceledException
{
    /// <summary>Initializes a new instance.</summary>
    public RequestCanceledException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public RequestCanceledException(string requestId, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), cancellationToken)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public RequestCanceledException(string requestId, Exception innerException, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), innerException, cancellationToken)
    {
    }

    static string FormatMessage(string requestId)
    {
        return $"The request was canceled, RequestId: {requestId}";
    }
}
