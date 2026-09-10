using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Validates consumer factories and convention-discovered message contracts.</summary>
public static class ConsumerFactoryConfiguratorExtensions
{
    /// <summary>Validates that a consumer exposes at least one message contract and that concrete messages can be initialized safely.</summary>
    /// <typeparam name="TConsumer">The consumer implementation whose contracts are validated.</typeparam>
    /// <param name="configurator">The specification that owns any reported validation result.</param>
    /// <returns>The validation failures and guidance produced for the consumer.</returns>
    public static IEnumerable<ValidationResult> ValidateConsumer<TConsumer>(this ISpecification? configurator)
        where TConsumer : class
    {
        if (ConsumerMetadataCache<TConsumer>.ConsumerTypes.Count == 0)
        {
            yield return configurator.Failure(
                "Consumer",
                $"No registered consumer convention discovered a message contract on {TypeCache<TConsumer>.ShortName}");
        }

        IEnumerable<ValidationResult> warningForMessages = ConsumerMetadataCache<TConsumer>
            .ConsumerTypes
            .Where(x => !x.MessageType.IsInterface)
            .Where(x => !HasProtectedDefaultConstructor(x.MessageType))
            .Select(x =>
                $"The {TypeCache.GetShortName(x.MessageType)} message should have a public or protected default constructor."
                + " Without an available constructor, ViciOne.ServiceBus will initialize new message instances"
                + " without calling a constructor, which can lead to unpredictable behavior if the message"
                + " depends upon logic in the constructor to be executed.")
            .Select(message => configurator.Warning("Message", message));

        foreach (var message in warningForMessages)
            yield return message;
    }

    /// <summary>Validates a consumer factory and every convention-discovered message contract.</summary>
    /// <typeparam name="TConsumer">The consumer implementation supplied by the factory.</typeparam>
    /// <param name="consumerFactory">The factory configuration to validate.</param>
    /// <returns>The factory and consumer-contract validation results.</returns>
    public static IEnumerable<ValidationResult> Validate<TConsumer>(this IConsumerFactory<TConsumer> consumerFactory)
        where TConsumer : class
    {
        if (consumerFactory == null)
            yield return ValidationResultExtensions.Failure(null, "UseConsumerFactory", "must not be null");

        foreach (var result in ValidateConsumer<TConsumer>(null))
            yield return result;
    }

    static bool HasProtectedDefaultConstructor(Type type)
    {
        return type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(constructorInfo => !constructorInfo.GetParameters().Any());
    }
}
