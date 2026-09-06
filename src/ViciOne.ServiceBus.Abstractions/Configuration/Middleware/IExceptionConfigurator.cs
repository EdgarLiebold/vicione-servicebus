using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures exception.</summary>
public interface IExceptionConfigurator
{
    /// <summary>Handles the supplied message or context.</summary>
    /// <param name="exceptionTypes">The exception types.</param>
    void Handle(params Type[] exceptionTypes);

    /// <summary>Handles the supplied message or context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void Handle<T>()
        where T : Exception;

    /// <summary>Handles the supplied message or context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    void Handle<T>(Func<T, bool> filter)
        where T : Exception;

    /// <summary>Ignores the selected event or message.</summary>
    /// <param name="exceptionTypes">The exception types.</param>
    void Ignore(params Type[] exceptionTypes);

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void Ignore<T>()
        where T : Exception;

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    void Ignore<T>(Func<T, bool> filter)
        where T : Exception;
}
