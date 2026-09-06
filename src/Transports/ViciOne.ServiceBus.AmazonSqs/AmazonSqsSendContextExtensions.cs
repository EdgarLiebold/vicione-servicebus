using System;
using ViciOne.ServiceBus.AmazonSqs;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SQS-native settings on a transport send context.</summary>
public static class AmazonSqsSendContextExtensions
{
    /// <summary>Sets the FIFO message-group identifier.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="groupId">The Amazon SQS message-group identifier.</param>
    /// <exception cref="ArgumentException">The context does not contain an Amazon SQS send payload.</exception>
    public static void SetGroupId(this SendContext context, string groupId)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            throw new ArgumentException("The AmazonSqsSendContext was not available");

        sendContext.GroupId = groupId;
    }

    /// <summary>Tries to set the FIFO message-group identifier.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="groupId">The Amazon SQS message-group identifier.</param>
    /// <returns><see langword="true" /> when the context contains an Amazon SQS payload and the value was set; otherwise, <see langword="false" />.</returns>
    public static bool TrySetGroupId(this SendContext context, string groupId)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            return false;

        sendContext.GroupId = groupId;
        return true;
    }

    /// <summary>Sets the FIFO message-deduplication identifier.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="deduplicationId">The Amazon SQS deduplication identifier.</param>
    /// <exception cref="ArgumentException">The context does not contain an Amazon SQS send payload.</exception>
    public static void SetDeduplicationId(this SendContext context, string deduplicationId)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            throw new ArgumentException("The AmazonSqsSendContext was not available");

        sendContext.DeduplicationId = deduplicationId;
    }

    /// <summary>Tries to set the FIFO message-deduplication identifier.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="deduplicationId">The Amazon SQS deduplication identifier.</param>
    /// <returns><see langword="true" /> when the context contains an Amazon SQS payload and the value was set; otherwise, <see langword="false" />.</returns>
    public static bool TrySetDeduplicationId(this SendContext context, string deduplicationId)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            return false;

        sendContext.DeduplicationId = deduplicationId;
        return true;
    }

    /// <summary>Sets the per-message Amazon SQS delivery delay.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="delay">The delay, rounded up to whole seconds and limited to the provider maximum.</param>
    /// <exception cref="ArgumentException">The context does not contain an Amazon SQS send payload.</exception>
    public static void SetDelay(this SendContext context, TimeSpan delay)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            throw new ArgumentException("The AmazonSqsSendContext was not available");

        sendContext.DelaySeconds = AmazonSqsDelay.FromTimeSpan(delay);
    }

    /// <summary>Tries to set the per-message Amazon SQS delivery delay.</summary>
    /// <param name="context">The transport send context.</param>
    /// <param name="delay">The delay, rounded up to whole seconds and limited to the provider maximum.</param>
    /// <returns><see langword="true" /> when the context contains an Amazon SQS payload and the delay was set; otherwise, <see langword="false" />.</returns>
    public static bool TrySetDelay(this SendContext context, TimeSpan delay)
    {
        if (!context.TryGetPayload(out AmazonSqsSendContext? sendContext))
            return false;

        sendContext.DelaySeconds = AmazonSqsDelay.FromTimeSpan(delay);
        return true;
    }
}
