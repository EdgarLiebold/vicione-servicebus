using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures exception types and predicates included in or excluded from a policy.</summary>
public interface IExceptionConfigurator
{
    /// <summary>Includes the specified exception types.</summary>
    /// <param name="exceptionTypes">The exception types to include.</param>
    void Handle(params Type[] exceptionTypes);

    /// <summary>Includes an exception type.</summary>
    /// <typeparam name="TException">The exception type to include.</typeparam>
    void Handle<TException>()
        where TException : Exception;

    /// <summary>Includes exceptions of a type when they match a predicate.</summary>
    /// <typeparam name="TException">The exception type to evaluate.</typeparam>
    /// <param name="filter">The predicate that selects included exceptions.</param>
    void Handle<TException>(Func<TException, bool> filter)
        where TException : Exception;

    /// <summary>Excludes the specified exception types.</summary>
    /// <param name="exceptionTypes">The exception types to exclude.</param>
    void Ignore(params Type[] exceptionTypes);

    /// <summary>Excludes an exception type.</summary>
    /// <typeparam name="TException">The exception type to exclude.</typeparam>
    void Ignore<TException>()
        where TException : Exception;

    /// <summary>Excludes exceptions of a type when they match a predicate.</summary>
    /// <typeparam name="TException">The exception type to evaluate.</typeparam>
    /// <param name="filter">The predicate that selects excluded exceptions.</param>
    void Ignore<TException>(Func<TException, bool> filter)
        where TException : Exception;
}
