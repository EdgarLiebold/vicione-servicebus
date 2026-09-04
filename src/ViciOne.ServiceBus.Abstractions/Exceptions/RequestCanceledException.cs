using System;
using System.Threading;

namespace ViciOne.ServiceBus;

[Serializable]
public class RequestCanceledException :
    OperationCanceledException
{
    public RequestCanceledException()
    {
    }

    public RequestCanceledException(string requestId, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), cancellationToken)
    {
    }

    public RequestCanceledException(string requestId, Exception innerException, CancellationToken cancellationToken)
        : base(FormatMessage(requestId), innerException, cancellationToken)
    {
    }

    static string FormatMessage(string requestId)
    {
        return $"The request was canceled, RequestId: {requestId}";
    }
}
