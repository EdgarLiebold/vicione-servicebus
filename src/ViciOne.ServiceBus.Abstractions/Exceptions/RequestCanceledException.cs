using System;
using System.Threading;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a request was canceled before a response completed it.</summary>
public class RequestCanceledException :
    OperationCanceledException
{
    /// <summary>Creates a cancellation exception without a request identifier.</summary>
    public RequestCanceledException()
    {
    }

    /// <summary>Creates a cancellation exception for the specified request.</summary>
    /// <param name="requestId">The identifier of the canceled request.</param>
    /// <param name="cancellationToken">The token that canceled the request.</param>
    public RequestCanceledException(Guid requestId, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), cancellationToken)
    {
        RequestId = requestId;
    }

    /// <summary>Creates a cancellation exception for the specified request and underlying failure.</summary>
    /// <param name="requestId">The identifier of the canceled request.</param>
    /// <param name="innerException">The exception observed when the request was canceled.</param>
    /// <param name="cancellationToken">The token that canceled the request.</param>
    public RequestCanceledException(Guid requestId, Exception innerException, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), innerException, cancellationToken)
    {
        RequestId = requestId;
    }

    /// <summary>Gets the identifier of the canceled request, when one was supplied.</summary>
    public Guid? RequestId { get; }

    static string FormatMessage(Guid requestId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(requestId), requestId, "The request identifier cannot be empty.");

        return $"The request was canceled, RequestId: {requestId:D}";
    }
}
