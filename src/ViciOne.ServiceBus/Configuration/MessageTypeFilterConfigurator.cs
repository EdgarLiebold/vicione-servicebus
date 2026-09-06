using System;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message type filter.</summary>
public class MessageTypeFilterConfigurator :
    IMessageTypeFilterConfigurator
{
    /// <summary>Initializes a new instance.</summary>
    public MessageTypeFilterConfigurator()
    {
        Filter = new CompositeFilter<Type>();
    }

    /// <summary>Gets the filter.</summary>
    public CompositeFilter<Type> Filter { get; }

    /// <summary>Includes the selected value.</summary>
    /// <param name="messageTypes">The message types.</param>
    public void Include(params Type[] messageTypes)
    {
        Filter.Includes.Add(type => Match(type, messageTypes));
    }

    /// <summary>Includes the selected value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Include(Func<Type, bool> filter)
    {
        Filter.Includes.Add(type => filter(type));
    }

    /// <summary>Includes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Include<T>()
        where T : class
    {
        Filter.Includes.Add(type => Match<T>(type));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="messageTypes">The message types.</param>
    public void Exclude(params Type[] messageTypes)
    {
        Filter.Excludes.Add(type => Match(type, messageTypes));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Exclude(Func<Type, bool> filter)
    {
        Filter.Excludes.Add(type => filter(type));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Exclude<T>()
        where T : class
    {
        Filter.Excludes.Add(type => Match<T>(type));
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
