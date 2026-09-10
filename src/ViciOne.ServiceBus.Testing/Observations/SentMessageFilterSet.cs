using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores predicates that match send observations by message contract and content.</summary>
public sealed class SentMessageFilterSet :
    FilterSet<ISentMessage>
{
    /// <summary>Adds a predicate that matches every send of a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <returns>This filter set.</returns>
    public SentMessageFilterSet Add<TMessage>()
        where TMessage : class
    {
        static bool Filter(ISentMessage element)
        {
            return element is ISentMessage<TMessage>;
        }

        Add(Filter);

        return this;
    }

    /// <summary>Adds a predicate for sends of a message contract.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="filter">The predicate applied to sends of the contract.</param>
    /// <returns>This filter set.</returns>
    public SentMessageFilterSet Add<TMessage>(FilterDelegate<ISentMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(ISentMessage element)
        {
            return element is ISentMessage<TMessage> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
