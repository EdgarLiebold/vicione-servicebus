using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures message inclusion and exclusion predicates in addition to type filters.</summary>
public interface IMessageFilterConfigurator :
    IMessageTypeFilterConfigurator
{
    /// <summary>Includes messages of the specified type that satisfy the predicate.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="filter">The filter expression.</param>
    void Include<T>(Func<T, bool> filter)
        where T : class;

    /// <summary>Excludes messages of the specified type that satisfy the predicate.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="filter">The filter expression.</param>
    void Exclude<T>(Func<T, bool> filter)
        where T : class;
}
