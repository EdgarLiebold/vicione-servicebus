using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer convention cache implementation.
/// </summary>
public static class ConsumerConventionCache
{
    static ConsumerConventionCache()
    {
        ConsumerConvention.Register<AsyncConsumerConvention>();
        ConsumerConvention.Register<BatchConsumerConvention>();
        ConsumerConvention.Register<JobConsumerConvention>();
    }

    /// <summary>
    /// Performs the try add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryAdd<T>(T convention)
        where T : IConsumerConvention
    {
        lock (Cached.MutateLock)
        {
            if (Cached.Registered.Any(x => x.GetType() == convention.GetType()))
                return false;

            Cached.Registered.Add(convention);
        }

        return true;
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool Remove<T>()
        where T : IConsumerConvention
    {
        lock (Cached.MutateLock)
        {
            for (var i = 0; i < Cached.Registered.Count; i++)
            {
                if (Cached.Registered[i] is T)
                {
                    Cached.Registered.RemoveAt(i);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the conventions registered for identifying message consumer types
    /// </summary>
    /// <typeparam name="T">The consumer type</typeparam>
    /// <returns></returns>
    public static IEnumerable<IConsumerMessageConvention> GetConventions<T>()
        where T : class
    {
        IConsumerConvention[] conventions;
        lock (Cached.MutateLock)
            conventions = Cached.Registered.ToArray();

        return conventions.Select(convention => convention.GetConsumerMessageConvention<T>()).ToArray();
    }


    static class Cached
    {
        internal static readonly object MutateLock = new object();
        internal static readonly List<IConsumerConvention> Registered = new List<IConsumerConvention>();
    }
}
