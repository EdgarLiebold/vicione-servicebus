using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a unique set of received message filter values.</summary>
public class ReceivedMessageFilterSet :
    FilterSet<IReceivedMessage>
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The received message filter set produced by the operation.</returns>
    public ReceivedMessageFilterSet Add<T>()
        where T : class
    {
        static bool Filter(IReceivedMessage element)
        {
            return element is IReceivedMessage<T>;
        }

        Add(Filter);

        return this;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The received message filter set produced by the operation.</returns>
    public ReceivedMessageFilterSet Add<T>(FilterDelegate<IReceivedMessage<T>> filter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(IReceivedMessage element)
        {
            return element is IReceivedMessage<T> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
