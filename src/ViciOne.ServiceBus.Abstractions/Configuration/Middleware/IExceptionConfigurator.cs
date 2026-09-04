using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for exception configurator.
/// </summary>
public interface IExceptionConfigurator
{
    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="exceptionTypes">The exception types value.</param>
    void Handle(params Type[] exceptionTypes);

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    void Handle<T>()
        where T : Exception;

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    void Handle<T>(Func<T, bool> filter)
        where T : Exception;

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <param name="exceptionTypes">The exception types value.</param>
    void Ignore(params Type[] exceptionTypes);

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    void Ignore<T>()
        where T : Exception;

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    void Ignore<T>(Func<T, bool> filter)
        where T : Exception;
}
