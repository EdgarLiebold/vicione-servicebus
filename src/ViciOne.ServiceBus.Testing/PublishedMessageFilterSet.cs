using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a published message filter set implementation.
/// </summary>
public class PublishedMessageFilterSet :
    FilterSet<IPublishedMessage>
{
    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
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
