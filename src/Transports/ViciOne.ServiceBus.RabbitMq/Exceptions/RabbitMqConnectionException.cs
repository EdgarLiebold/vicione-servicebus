using System;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Represents an error related to rabbit mq connection.
/// </summary>
public class RabbitMqConnectionException :
    ConnectionException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RabbitMqConnectionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public RabbitMqConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The transport's own failures, where this assembly decides whether waiting can resolve them.
    /// <para>
    /// The public string constructor remains non-transient. Internal call sites can opt into
    /// transient classification without changing the semantics observed by external callers.
    /// </para>
    /// </summary>
    internal RabbitMqConnectionException(string message, bool isTransient)
        : base(message, isTransient)
    {
    }

    /// <summary>
    /// The connection is shutting down and cannot serve this caller.
    /// <para>
    /// Transient, because a stop is exactly the failure a later start resolves. A caller asking
    /// "can waiting fix this?" has to be told yes here; announced as permanent, a routine shutdown
    /// carries the same answer as a refused credential and every run after a stop fails specs that
    /// have nothing to do with it.
    /// </para>
    /// </summary>
    internal static RabbitMqConnectionException Stopping(string description)
    {
        return new RabbitMqConnectionException($"The connection is stopping and cannot be used: {description}", true);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public RabbitMqConnectionException(string message, Exception innerException)
        : base(message, innerException, IsExceptionTransient(innerException))
    {
    }

    /// <summary>
    /// Whether the failure is worth waiting out.
    /// <para>
    /// Transient is the default: an unreachable broker, a dropped connection and every other fault
    /// the transport recovers from. Two answers are not transient, because repeating them cannot
    /// change them — a refused credential, and a queue the broker will not hand over exclusively.
    /// Counting the second one as transient hides the endpoint start in a background retry loop and
    /// leaves the caller with the generic readiness timeout after sixty seconds instead of the
    /// broker's answer.
    /// </para>
    /// </summary>
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
