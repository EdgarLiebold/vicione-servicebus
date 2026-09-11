using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Consumers.Conventions;

/// <summary>Stores the process-wide ordered set of consumer-discovery conventions.</summary>
internal static class ConsumerConventionCache
{
    static ConsumerConventionCache()
    {
        ConsumerConvention.Register<AsyncConsumerConvention>();
        ConsumerConvention.Register<BatchConsumerConvention>();
    }

    /// <summary>Adds a convention when no convention of the same concrete type is registered.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <param name="convention">The convention to add.</param>
    /// <returns><see langword="true" /> when the convention was added; otherwise, <see langword="false" />.</returns>
    public static bool TryAdd<TConvention>(TConvention convention)
        where TConvention : IConsumerConvention
    {
        ArgumentNullException.ThrowIfNull(convention);

        lock (Cached.MutateLock)
        {
            if (Cached.Registered.Any(x => x.GetType() == convention.GetType()))
                return false;

            Cached.Registered.Add(convention);
            Cached.Version++;
        }

        return true;
    }

    /// <summary>Removes the first convention assignable to the requested convention type.</summary>
    /// <typeparam name="TConvention">The convention type.</typeparam>
    /// <returns><see langword="true" /> when a convention was removed; otherwise, <see langword="false" />.</returns>
    public static bool Remove<TConvention>()
        where TConvention : IConsumerConvention
    {
        lock (Cached.MutateLock)
        {
            for (var i = 0; i < Cached.Registered.Count; i++)
            {
                if (Cached.Registered[i] is TConvention)
                {
                    Cached.Registered.RemoveAt(i);
                    Cached.Version++;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Returns the conventions registered for identifying message consumer types.</summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The conventions.</returns>
    public static IEnumerable<IConsumerMessageConvention> GetConventions<TConsumer>()
        where TConsumer : class
    {
        (_, IConsumerConvention[] conventions) = GetSnapshot();

        return conventions.Select(convention => convention.GetConsumerMessageConvention<TConsumer>()).ToArray();
    }

    internal static (long Version, IConsumerConvention[] Conventions) GetSnapshot()
    {
        lock (Cached.MutateLock)
            return (Cached.Version, Cached.Registered.ToArray());
    }


    static class Cached
    {
        internal static readonly object MutateLock = new();
        internal static readonly List<IConsumerConvention> Registered = [];
        internal static long Version;
    }
}
