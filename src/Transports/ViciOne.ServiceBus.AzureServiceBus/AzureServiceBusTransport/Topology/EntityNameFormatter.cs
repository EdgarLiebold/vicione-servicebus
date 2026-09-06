namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Builds Azure Service Bus subscription and transfer subqueue paths.</summary>
public static class EntityNameFormatter
{
    const string PathDelimiter = @"/";
    const string Subscriptions = "Subscriptions";
    const string SubQueuePrefix = "$";
    const string DeadLetterQueueSuffix = "DeadLetterQueue";
    const string ErrorQueueSuffix = "Error";
    const string DeadLetterQueueName = SubQueuePrefix + DeadLetterQueueSuffix;
    const string ErrorQueueName = SubQueuePrefix + ErrorQueueSuffix;

    /// <summary>Appends the Azure dead-letter subqueue name to a queue or subscription path.</summary>
    /// <param name="entityPath">The queue or subscription entity path.</param>
    /// <returns>The dead-letter subqueue path.</returns>
    public static string FormatDeadLetterPath(string entityPath)
    {
        return FormatSubQueuePath(entityPath, DeadLetterQueueName);
    }

    /// <summary>Appends the transport error subqueue name to a queue or subscription path.</summary>
    /// <param name="entityPath">The queue or subscription entity path.</param>
    /// <returns>The transport error subqueue path.</returns>
    public static string FormatErrorPath(string entityPath)
    {
        return FormatSubQueuePath(entityPath, ErrorQueueName);
    }

    /// <summary>Appends a subqueue name to a queue or subscription path.</summary>
    /// <param name="entityPath">The queue or subscription entity path.</param>
    /// <param name="subQueueName">The provider subqueue name, including its leading dollar sign.</param>
    /// <returns>The complete subqueue path.</returns>
    public static string FormatSubQueuePath(string entityPath, string subQueueName)
    {
        return string.Concat(entityPath, PathDelimiter, subQueueName);
    }

    /// <summary>Builds a subscription entity path from its topic and subscription names.</summary>
    /// <param name="topicPath">The namespace-relative topic path.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>The formatted subscription path.</returns>
    public static string FormatSubscriptionPath(string topicPath, string subscriptionName)
    {
        return string.Concat(topicPath, PathDelimiter, Subscriptions, PathDelimiter, subscriptionName);
    }
}
