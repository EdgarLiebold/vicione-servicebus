using System;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Specify the consumer, saga, and activity types to include/exclude.</summary>
public interface IRegistrationFilterConfigurator
{
    /// <summary>Include the specified types.</summary>
    /// <param name="types">The types.</param>
    void Include(params Type[] types);

    /// <summary>Include the specified type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void Include<T>();

    /// <summary>Exclude the specified types.</summary>
    /// <param name="types">The types.</param>
    void Exclude(params Type[] types);

    /// <summary>Exclude the specified type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void Exclude<T>();
}
