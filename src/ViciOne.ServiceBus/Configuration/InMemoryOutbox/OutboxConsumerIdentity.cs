using System;
using ViciOne.ServiceBus.JobService;

namespace ViciOne.ServiceBus.Configuration;

internal static class OutboxConsumerIdentity
{
    internal static Guid Create<TConsumer, TMessage>(string busKey, Uri inputAddress)
        where TConsumer : class
        where TMessage : class
    {
        if (string.IsNullOrWhiteSpace(busKey))
            throw new ArgumentException("A bus key is required.", nameof(busKey));
        ArgumentNullException.ThrowIfNull(inputAddress);

        return JobMetadataCache<TConsumer, TMessage>.GenerateJobTypeId($"{busKey}|{inputAddress.AbsoluteUri}");
    }
}
