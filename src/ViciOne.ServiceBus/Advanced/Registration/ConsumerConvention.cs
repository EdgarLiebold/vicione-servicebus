using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers.Conventions;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Manages process-wide conventions that discover message contracts implemented by consumer types.</summary>
public static class ConsumerConvention
{
    /// <summary>Registers a newly constructed process-wide consumer convention when its concrete type is not already registered.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    public static bool Register<TConvention>()
        where TConvention : IConsumerConvention, new()
    {
        var convention = new TConvention();

        return ConsumerConventionCache.TryAdd(convention);
    }

    /// <summary>Registers a process-wide consumer convention when its concrete type is not already registered.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="convention">The convention to register.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    public static bool Register<TConvention>(TConvention convention)
        where TConvention : IConsumerConvention
    {
        if (convention == null)
            throw new ArgumentNullException(nameof(convention));

        return ConsumerConventionCache.TryAdd(convention);
    }

    /// <summary>Removes the first process-wide convention of the specified type.</summary>
    /// <typeparam name="TConvention">The convention type to remove.</typeparam>
    public static void Remove<TConvention>()
        where TConvention : IConsumerConvention
    {
        ConsumerConventionCache.Remove<TConvention>();
    }
}
