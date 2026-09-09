using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a message filter, for including and excluding message types.</summary>
public interface IMessageTypeFilterConfigurator
{
    /// <summary>Includes messages assignable to any specified message type.</summary>
    /// <param name="messageTypes">The message types.</param>
    void Include(params Type[] messageTypes);

    /// <summary>Includes message types that satisfy the predicate.</summary>
    /// <param name="filter">The filter expression.</param>
    void Include(Func<Type, bool> filter);

    /// <summary>Includes messages assignable to the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    void Include<T>()
        where T : class;

    /// <summary>Excludes messages assignable to any specified message type.</summary>
    /// <param name="messageTypes">The message types.</param>
    void Exclude(params Type[] messageTypes);

    /// <summary>Excludes message types that satisfy the predicate.</summary>
    /// <param name="filter">The filter expression.</param>
    void Exclude(Func<Type, bool> filter);

    /// <summary>Excludes messages assignable to the specified message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    void Exclude<T>()
        where T : class;
}
