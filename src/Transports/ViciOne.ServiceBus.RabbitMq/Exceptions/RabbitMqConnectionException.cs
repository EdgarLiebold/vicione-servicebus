using System;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Reports a RabbitMQ connection failure and whether waiting may restore availability.</summary>
public class RabbitMqConnectionException :
    ConnectionException
{
    /// <summary>Creates an exception without connection details.</summary>
    public RabbitMqConnectionException()
    {
    }

    /// <summary>Creates a non-transient connection exception.</summary>
    /// <param name="message">The connection failure description.</param>
    public RabbitMqConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a transport-owned connection exception with an explicit transient classification.
    /// </summary>
    /// <param name="message">The connection failure description.</param>
    /// <param name="isTransient">Whether retrying after a delay may restore availability.</param>
    internal RabbitMqConnectionException(string message, bool isTransient)
        : base(message, isTransient)
    {
    }

    /// <summary>
    /// Creates a transient failure for a connection that is stopping and cannot serve the caller.
    /// <para>
    /// A subsequent connection start can restore availability, so retry policies may wait for the
    /// replacement connection instead of treating the shutdown as a permanent broker failure.
    /// </para>
    /// </summary>
    /// <param name="description">The sanitized RabbitMQ host description.</param>
    /// <returns>A transient RabbitMQ connection exception.</returns>
    internal static RabbitMqConnectionException Stopping(string description)
    {
        return new RabbitMqConnectionException($"The connection is stopping and cannot be used: {description}", true);
    }

    /// <summary>Creates a connection exception classified from the RabbitMQ client failure.</summary>
    /// <param name="message">The connection failure description.</param>
    /// <param name="innerException">The RabbitMQ client exception.</param>
    public RabbitMqConnectionException(string message, Exception innerException)
        : base(message, innerException, IsExceptionTransient(innerException))
    {
    }

    /// <summary>
    /// Classifies authentication failures and exclusive-queue conflicts as permanent; other
    /// connection failures are considered transient.
    /// </summary>
    /// <param name="exception">The RabbitMQ connection failure to classify.</param>
    /// <returns><see langword="true" /> when retrying may restore availability.</returns>
    static bool IsExceptionTransient(Exception exception)
    {
        if (exception.IsExclusiveResourceConflict())
            return false;

        return exception switch
        {
            BrokerUnreachableException bue => bue.InnerException switch
            {
                AuthenticationFailureException _ => false,
                _ => true
            },
            _ => true
        };
    }
}
