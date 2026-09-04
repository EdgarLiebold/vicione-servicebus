using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a sent message filter set implementation.
/// </summary>
public class SentMessageFilterSet :
    FilterSet<ISentMessage>
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public SentMessageFilterSet Add<T>()
        where T : class
    {
        static bool Filter(ISentMessage element)
        {
            return element is ISentMessage<T>;
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
    public SentMessageFilterSet Add<T>(FilterDelegate<ISentMessage<T>> filter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(ISentMessage element)
        {
            return element is ISentMessage<T> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
