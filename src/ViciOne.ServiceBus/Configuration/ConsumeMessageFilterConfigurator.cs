using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consume message filter configurator implementation.
/// </summary>
public class ConsumeMessageFilterConfigurator :
    IMessageFilterConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConsumeMessageFilterConfigurator()
    {
        Filter = new CompositeFilter<ConsumeContext>();
    }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    public CompositeFilter<ConsumeContext> Filter { get; }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="messageTypes">The message types value.</param>
    public void Include(params Type[] messageTypes)
    {
        Filter.Includes.Add(message => Match(message, messageTypes));
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public void Include(Func<Type, bool> filter)
    {
        Filter.Includes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(ConsumeContext<>), out Type[] types) && filter(types[0]));
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Include<T>()
        where T : class
    {
        Filter.Includes.Add(message => Match<T>(message));
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    public void Include<T>(Func<T, bool> filter)
        where T : class
    {
        Filter.Includes.Add(message => Match(message, filter));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="messageTypes">The message types value.</param>
    public void Exclude(params Type[] messageTypes)
    {
        Filter.Excludes.Add(message => Match(message, messageTypes));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public void Exclude(Func<Type, bool> filter)
    {
        Filter.Excludes.Add(context => context.GetType().TryGetSingleClosedGenericArguments(typeof(ConsumeContext<>), out Type[] types) && filter(types[0]));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Exclude<T>()
        where T : class
    {
        Filter.Excludes.Add(message => Match<T>(message));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
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
