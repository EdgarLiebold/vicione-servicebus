using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores predicates that match consumption observations by message contract and content.</summary>
public sealed class ConsumedMessageFilterSet :
    FilterSet<IConsumedMessage>
{
    /// <summary>Adds a predicate that matches every consumption of a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <returns>This filter set.</returns>
    public ConsumedMessageFilterSet Add<TMessage>()
        where TMessage : class
    {
        static bool Filter(IConsumedMessage element)
        {
            return element is IConsumedMessage<TMessage>;
        }

        Add(Filter);

        return this;
    }

    /// <summary>Adds a predicate for consumptions of a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="filter">The predicate applied to consumptions of the contract.</param>
    /// <returns>This filter set.</returns>
    public ConsumedMessageFilterSet Add<TMessage>(FilterDelegate<IConsumedMessage<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(filter);

        bool Filter(IConsumedMessage element)
        {
            return element is IConsumedMessage<TMessage> result && filter(result);
        }

        Add(Filter);

        return this;
    }
}
