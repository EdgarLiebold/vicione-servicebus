using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a received message filter set implementation.
/// </summary>
public class ReceivedMessageFilterSet :
    FilterSet<IReceivedMessage>
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
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
