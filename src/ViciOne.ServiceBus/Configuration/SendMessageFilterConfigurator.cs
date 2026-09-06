using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures send message filter.</summary>
public class SendMessageFilterConfigurator :
    IMessageFilterConfigurator
{
    /// <summary>Initializes a new instance.</summary>
    public SendMessageFilterConfigurator()
    {
        Filter = new CompositeFilter<SendContext>();
    }

    /// <summary>Gets the filter.</summary>
    public CompositeFilter<SendContext> Filter { get; }

    void IMessageTypeFilterConfigurator.Include(params Type[] messageTypes)
    {
        Filter.Includes.Add(message => Match(message, messageTypes));
    }

    void IMessageTypeFilterConfigurator.Include(Func<Type, bool> filter)
    {
        Filter.Includes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(SendContext<>), out Type[] types) && filter(types[0]));
    }

    void IMessageTypeFilterConfigurator.Include<T>()
    {
        Filter.Includes.Add(message => Match<T>(message));
    }

    void IMessageFilterConfigurator.Include<T>(Func<T, bool> filter)
    {
        Filter.Includes.Add(message => Match(message, filter));
    }

    void IMessageTypeFilterConfigurator.Exclude(params Type[] messageTypes)
    {
        Filter.Excludes.Add(message => Match(message, messageTypes));
    }

    void IMessageTypeFilterConfigurator.Exclude(Func<Type, bool> filter)
    {
        Filter.Excludes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(SendContext<>), out Type[] types) && filter(types[0]));
    }

    void IMessageTypeFilterConfigurator.Exclude<T>()
    {
        Filter.Excludes.Add(message => Match<T>(message));
    }

    void IMessageFilterConfigurator.Exclude<T>(Func<T, bool> filter)
    {
        Filter.Excludes.Add(message => Match(message, filter));
    }

    static bool Match(SendContext context, params Type[] messageTypes)
    {
        return messageTypes.Any(x => typeof(SendContext<>).MakeGenericType(x).IsInstanceOfType(context));
    }

    static bool Match<T>(SendContext context)
        where T : class
    {
        return context is SendContext<T>;
    }

    static bool Match<T>(SendContext context, Func<T, bool> filter)
        where T : class
    {
        var sendContext = context as SendContext<T>;

        return sendContext != null && filter(sendContext.Message);
    }
}
