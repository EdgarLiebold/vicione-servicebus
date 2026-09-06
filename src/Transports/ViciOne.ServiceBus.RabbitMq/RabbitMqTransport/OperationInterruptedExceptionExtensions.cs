using System;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Classifies RabbitMQ broker shutdown replies for channel and retry handling.</summary>
public static class OperationInterruptedExceptionExtensions
{
    /// <summary>AMQP 0-9-1 channel exception RESOURCE_LOCKED.</summary>
    const ushort ResourceLocked = 405;

    /// <summary>Determines whether the channel must be closed.</summary>
    /// <param name="ex">The RabbitMQ operation interruption.</param>
    /// <returns><see langword="true" /> when no broker reply exists or its AMQP code is an error.</returns>
    public static bool ChannelShouldBeClosed(this OperationInterruptedException ex)
    {
        if (ex.ShutdownReason == null)
            return true;

        return ex.ShutdownReason?.ReplyCode >= 300;
    }

    /// <summary>
    /// Determines whether any nested broker reply is AMQP <c>RESOURCE_LOCKED</c> (405). This exact
    /// declaration conflict is permanent; adjacent replies such as <c>PRECONDITION_FAILED</c> remain retryable.
    /// </summary>
    /// <param name="exception">The exception chain to inspect.</param>
    /// <returns><see langword="true" /> when it contains a RabbitMQ 405 shutdown reply.</returns>
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
