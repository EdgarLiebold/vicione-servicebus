using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consume message filter.</summary>
public class ConsumeMessageFilterConfigurator :
    IMessageFilterConfigurator
{
    /// <summary>Initializes a new instance.</summary>
    public ConsumeMessageFilterConfigurator()
    {
        Filter = new CompositeFilter<ConsumeContext>();
    }

    /// <summary>Gets the filter.</summary>
    public CompositeFilter<ConsumeContext> Filter { get; }

    /// <summary>Includes the selected value.</summary>
    /// <param name="messageTypes">The message types.</param>
    public void Include(params Type[] messageTypes)
    {
        Filter.Includes.Add(message => Match(message, messageTypes));
    }

    /// <summary>Includes the selected value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Include(Func<Type, bool> filter)
    {
        Filter.Includes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(ConsumeContext<>), out Type[] types) && filter(types[0]));
    }

    /// <summary>Includes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Include<T>()
        where T : class
    {
        Filter.Includes.Add(message => Match<T>(message));
    }

    /// <summary>Includes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Include<T>(Func<T, bool> filter)
        where T : class
    {
        Filter.Includes.Add(message => Match(message, filter));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="messageTypes">The message types.</param>
    public void Exclude(params Type[] messageTypes)
    {
        Filter.Excludes.Add(message => Match(message, messageTypes));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Exclude(Func<Type, bool> filter)
    {
        Filter.Excludes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(ConsumeContext<>), out Type[] types) && filter(types[0]));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Exclude<T>()
        where T : class
    {
        Filter.Excludes.Add(message => Match<T>(message));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Exclude<T>(Func<T, bool> filter)
        where T : class
    {
        Filter.Excludes.Add(message => Match(message, filter));
    }

    static bool Match(ConsumeContext context, params Type[] messageTypes)
    {
        return messageTypes.Any(type => context.HasMessageType(type));
    }

    static bool Match<T>(ConsumeContext context)
        where T : class
    {
        return context.TryGetMessage(out ConsumeContext<T>? _);
    }

    static bool Match<T>(ConsumeContext context, Func<T, bool> filter)
        where T : class
    {
        return context.TryGetMessage(out ConsumeContext<T>? consumeContext) && filter(consumeContext.Message);
    }
}
