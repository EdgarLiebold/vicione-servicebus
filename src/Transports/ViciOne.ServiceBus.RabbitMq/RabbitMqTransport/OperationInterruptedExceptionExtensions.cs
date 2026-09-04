using System;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides extension methods for operation interrupted exception.
/// </summary>
public static class OperationInterruptedExceptionExtensions
{
    /// <summary>AMQP 0-9-1 channel exception RESOURCE_LOCKED.</summary>
    const ushort ResourceLocked = 405;

    /// <summary>
    /// Performs the channel should be closed operation.
    /// </summary>
    /// <param name="ex">The ex value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool ChannelShouldBeClosed(this OperationInterruptedException ex)
    {
        if (ex.ShutdownReason == null)
            return true;

        return ex.ShutdownReason?.ReplyCode >= 300;
    }

    /// <summary>
    /// Whether the broker refused the operation because the resource is exclusively held elsewhere.
    /// <para>
    /// Deliberately one reply code and not a range. RESOURCE_LOCKED answers a queue declare that
    /// asks for exclusive ownership the broker cannot grant, and that attempt cannot succeed by
    /// being repeated — the answer is about the declaration, not about the connection. Its
    /// neighbour PRECONDITION_FAILED (406) is the opposite case and must keep its retry: that is
    /// the code the delivery acknowledgement timeout produces, and the redelivery depends on the
    /// transport reconnecting after the broker closes the channel. A rule over the whole 4xx range
    /// would take one with the other.
    /// </para>
    /// </summary>
    internal static bool IsExclusiveResourceConflict(this Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is not OperationInterruptedException interrupted)
                continue;

            var reason = interrupted.ShutdownReason;
            if (reason == null)
                continue;

            // The typed broker reply is authoritative; do not reconstruct protocol state from
            // exception text or auxiliary data.
            if (reason.ReplyCode == ResourceLocked)
                return true;

        }

        return false;
    }
}
