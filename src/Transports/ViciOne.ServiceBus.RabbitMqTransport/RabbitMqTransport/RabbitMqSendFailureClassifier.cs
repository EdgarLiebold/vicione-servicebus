namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using RabbitMQ.Client.Exceptions;


/// <summary>
/// Classifies RabbitMQ send failures using exception identity and AMQP reply codes.
/// </summary>
public sealed class RabbitMqSendFailureClassifier : ITransportSendFailureClassifier
{
    const ushort ContentTooLarge = 311;
    const ushort AccessRefused = 403;
    const ushort ResourceLocked = 405;
    const ushort NotAllowed = 530;
    const ushort NotImplemented = 540;

    public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var foundTransientProviderFailure = false;

        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is AuthenticationFailureException)
            {
                failureKind = TransportSendFailureKind.Permanent;
                return true;
            }

            if (current is OperationInterruptedException interrupted && interrupted.ShutdownReason != null)
            {
                if (interrupted.ShutdownReason.ReplyCode is
                    ContentTooLarge or AccessRefused or ResourceLocked or NotAllowed or NotImplemented)
                {
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;
                }

                foundTransientProviderFailure = true;
            }

            if (current is BrokerUnreachableException or AlreadyClosedException)
                foundTransientProviderFailure = true;
        }

        if (foundTransientProviderFailure)
        {
            failureKind = TransportSendFailureKind.Transient;
            return true;
        }

        failureKind = TransportSendFailureKind.Unclassified;
        return false;
    }
}
