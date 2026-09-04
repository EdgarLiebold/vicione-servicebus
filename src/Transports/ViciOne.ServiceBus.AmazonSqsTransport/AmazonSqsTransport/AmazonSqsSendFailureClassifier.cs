using System;
using System.Net;
using Amazon.Runtime;
using Amazon.SQS.Model;

#nullable enable
namespace ViciOne.ServiceBus.AmazonSqsTransport;
/// <summary>
/// Classifies Amazon SQS send failures from typed SDK exceptions and HTTP status codes.
/// </summary>
public sealed class AmazonSqsSendFailureClassifier : ITransportSendFailureClassifier
{
    /// <inheritdoc />
    public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sawTransient = false;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case AmazonSqsConnectionException { IsTransient: false }:
                case InvalidMessageContentsException:
                case AmazonServiceException serviceException when IsPermanentStatus(serviceException.StatusCode):
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case AmazonSqsConnectionException:
                case AmazonServiceException serviceException when IsTransientStatus(serviceException.StatusCode):
                    sawTransient = true;
                    break;
            }
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }

    internal static bool IsTransientStatus(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || code >= 500;
    }

    internal static bool IsPermanentStatus(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        return code is >= 400 and < 500 && !IsTransientStatus(statusCode);
    }
}
