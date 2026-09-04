using System;
using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;

#nullable enable
namespace ViciOne.ServiceBus.AzureServiceBusTransport;
/// <summary>
/// Classifies Azure Service Bus send failures from typed SDK failure reasons and HTTP status codes.
/// </summary>
public sealed class ServiceBusSendFailureClassifier : ITransportSendFailureClassifier
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
                case UnauthorizedAccessException:
                case ServiceBusException { Reason: ServiceBusFailureReason.MessageSizeExceeded }:
                case RequestFailedException requestFailedException when IsPermanentStatus(requestFailedException.Status):
                case ServiceBusConnectionException { IsTransient: false }:
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case ServiceBusException { IsTransient: true }:
                case RequestFailedException requestFailedException when IsTransientStatus(requestFailedException.Status):
                case WebSocketException:
                case TimeoutException:
                case ServiceBusConnectionException:
                    sawTransient = true;
                    break;
            }
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }

    internal static bool IsTransientStatus(int statusCode) => statusCode is 408 or 429 or >= 500;

    internal static bool IsPermanentStatus(int statusCode) => statusCode is >= 400 and < 500 && !IsTransientStatus(statusCode);
}
