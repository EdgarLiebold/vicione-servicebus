using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a unique set of published message filter values.</summary>
public class PublishedMessageFilterSet :
    FilterSet<IPublishedMessage>
{
    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The published message filter set produced by the operation.</returns>
    public PublishedMessageFilterSet Add<T>()
        where T : class
    {
        static bool Filter(IPublishedMessage element)
        {
            return element is IPublishedMessage<T>;
        }

        Add(Filter);

        return this;
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The published message filter set produced by the operation.</returns>
    public PublishedMessageFilterSet Add<T>(FilterDelegate<IPublishedMessage<T>> filter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(IPublishedMessage element)
        {
            return element is IPublishedMessage<T> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
