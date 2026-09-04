using System;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a message type filter configurator implementation.
/// </summary>
public class MessageTypeFilterConfigurator :
    IMessageTypeFilterConfigurator
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageTypeFilterConfigurator()
    {
        Filter = new CompositeFilter<Type>();
    }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    public CompositeFilter<Type> Filter { get; }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="messageTypes">The message types value.</param>
    public void Include(params Type[] messageTypes)
    {
        Filter.Includes += type => Match(type, messageTypes);
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public void Include(Func<Type, bool> filter)
    {
        Filter.Includes += type => filter(type);
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Include<T>()
        where T : class
    {
        Filter.Includes += type => Match<T>(type);
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="messageTypes">The message types value.</param>
    public void Exclude(params Type[] messageTypes)
    {
        Filter.Excludes += type => Match(type, messageTypes);
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public void Exclude(Func<Type, bool> filter)
    {
        Filter.Excludes += type => filter(type);
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Exclude<T>()
        where T : class
    {
        Filter.Excludes += type => Match<T>(type);
    }

    static bool Match(Type type, params Type[] messageTypes)
    {
        return messageTypes.Any(x => x == type);
    }

    static bool Match<T>(Type type)
        where T : class
    {
        return type == typeof(T);
    }
}
