using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a unique set of sent message filter values.</summary>
public class SentMessageFilterSet :
    FilterSet<ISentMessage>
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The sent message filter set produced by the operation.</returns>
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

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The sent message filter set produced by the operation.</returns>
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
