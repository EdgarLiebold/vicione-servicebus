using System;
using System.Net.WebSockets;
using Azure;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;
/// <summary>Classifies Azure Service Bus send failures from typed SDK failure reasons and HTTP status codes.</summary>
public sealed class ServiceBusSendFailureClassifier : ITransportSendFailureClassifier
{
    /// <inheritdoc />
    public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (HasForeignConnectionCause(exception) && !HasAzureTransportCause(exception))
        {
            failureKind = TransportSendFailureKind.Unclassified;
            return false;
        }

        var sawTransient = false;
        if (HasPermanentCause(exception, ref sawTransient))
        {
            failureKind = TransportSendFailureKind.Permanent;
            return true;
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }

    static bool HasForeignConnectionCause(Exception exception)
    {
        if (exception is ConnectionException and not ServiceBusConnectionException
            && exception.GetType() != typeof(ConnectionException))
            return true;

        if (exception is AggregateException aggregate)
        {
            foreach (var cause in aggregate.InnerExceptions)
            {
                if (HasForeignConnectionCause(cause))
                    return true;
            }

            return false;
        }

        return exception.InnerException is { } inner && HasForeignConnectionCause(inner);
    }

    static bool HasAzureTransportCause(Exception exception)
    {
        if (exception is ServiceBusConnectionException or ServiceBusException)
            return true;

        if (exception is AggregateException aggregate)
        {
            foreach (var cause in aggregate.InnerExceptions)
            {
                if (HasAzureTransportCause(cause))
                    return true;
            }

            return false;
        }

        return exception.InnerException is { } inner && HasAzureTransportCause(inner);
    }

    static bool HasPermanentCause(Exception exception, ref bool sawTransient)
    {
        switch (exception)
        {
            case UnauthorizedAccessException:
            case ServiceBusException broker when !ServiceBusFailureTaxonomy.CanRetryBrokerFailure(broker, receive: false):
            case RequestFailedException request when IsPermanentStatus(request.Status):
            case ServiceBusConnectionException { IsTransient: false }:
                return true;

            case ServiceBusException:
            case RequestFailedException request when IsTransientStatus(request.Status):
            case WebSocketException:
            case TimeoutException:
            case ServiceBusConnectionException:
                sawTransient = true;
                break;
        }

        if (exception is AggregateException aggregate)
        {
            foreach (var cause in aggregate.InnerExceptions)
            {
                if (HasPermanentCause(cause, ref sawTransient))
                    return true;
            }

            return false;
        }

        return exception.InnerException is { } inner && HasPermanentCause(inner, ref sawTransient);
    }

    internal static bool IsTransientStatus(int statusCode) => statusCode is 0 or 408 or 429 or >= 500;

    internal static bool IsPermanentStatus(int statusCode) => statusCode is >= 400 and < 500 && !IsTransientStatus(statusCode);
}
