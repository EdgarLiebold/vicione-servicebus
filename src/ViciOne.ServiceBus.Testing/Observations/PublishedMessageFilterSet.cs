using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores predicates that match publication observations by message contract and content.</summary>
public sealed class PublishedMessageFilterSet :
    FilterSet<IPublishedMessage>
{
    /// <summary>Adds a predicate that matches every publication of a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <returns>This filter set.</returns>
    public PublishedMessageFilterSet Add<TMessage>()
        where TMessage : class
    {
        static bool Filter(IPublishedMessage element)
        {
            return element is IPublishedMessage<TMessage>;
        }

        Add(Filter);

        return this;
    }

    /// <summary>Adds a predicate for publications of a message contract.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="filter">The predicate applied to publications of the contract.</param>
    /// <returns>This filter set.</returns>
    public PublishedMessageFilterSet Add<TMessage>(FilterDelegate<IPublishedMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(IPublishedMessage element)
        {
            return element is IPublishedMessage<TMessage> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
