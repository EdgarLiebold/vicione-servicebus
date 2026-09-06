using System;
using ViciOne.ServiceBus.ExceptionFilters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for exception.</summary>
public abstract class ExceptionSpecification :
    IExceptionConfigurator
{
    readonly CompositeFilter<Exception> _exceptionFilter;

    /// <summary>Initializes a new instance.</summary>
    protected ExceptionSpecification()
    {
        _exceptionFilter = new CompositeFilter<Exception>();
        Filter = new CompositeExceptionFilter(_exceptionFilter);
    }

    /// <summary>Gets the filter.</summary>
    protected IExceptionFilter Filter { get; }

    /// <summary>Creates filter snapshot.</summary>
    /// <returns>The created filter snapshot.</returns>
    protected IExceptionFilter CreateFilterSnapshot()
    {
        return new CompositeExceptionFilter(_exceptionFilter.CreateSnapshot());
    }

    /// <summary>Handles the supplied message or context.</summary>
    /// <param name="exceptionTypes">The exception types.</param>
    public void Handle(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Includes.Add(exception => Match(exception, snapshot));
    }

    /// <summary>Handles the supplied message or context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Handle<T>()
        where T : Exception
    {
        _exceptionFilter.Includes.Add(exception => Match(exception, typeof(T)));
    }

    /// <summary>Handles the supplied message or context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Handle<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);
        _exceptionFilter.Includes.Add(exception => Match(exception, filter));
    }

    /// <summary>Ignores the selected event or message.</summary>
    /// <param name="exceptionTypes">The exception types.</param>
    public void Ignore(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Excludes.Add(exception => Match(exception, snapshot));
    }

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Ignore<T>()
        where T : Exception
    {
        _exceptionFilter.Excludes.Add(exception => Match(exception, typeof(T)));
    }

    /// <summary>Ignores the selected event or message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Ignore<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);
        _exceptionFilter.Excludes.Add(exception => Match(exception, filter));
    }

    static Type[] SnapshotTypes(Type[] exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(exceptionTypes);

        var snapshot = (Type[])exceptionTypes.Clone();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (snapshot[index] == null || !typeof(Exception).IsAssignableFrom(snapshot[index]))
                throw new ArgumentException("Every configured type must derive from Exception.", nameof(exceptionTypes));
        }

        return snapshot;
    }

    static bool Match(Exception exception, params Type[] exceptionTypes)
    {
        var baseException = exception.GetBaseException();

        if (baseException is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                var baseInnerException = innerException.GetBaseException();

                for (var i = 0; i < exceptionTypes.Length; i++)
                {
                    if (exceptionTypes[i].IsInstanceOfType(innerException))
                        return true;

                    if (exceptionTypes[i].IsInstanceOfType(baseInnerException))
                        return true;
                }
            }
        }

        for (var i = 0; i < exceptionTypes.Length; i++)
        {
            if (exceptionTypes[i].IsInstanceOfType(exception))
                return true;

            if (exceptionTypes[i].IsInstanceOfType(baseException))
                return true;
        }

        return false;
    }

    static bool Match<T>(Exception exception, Func<T, bool> filter)
        where T : Exception
    {
        if (exception is T ofT)
            return filter(ofT);

        var baseException = exception.GetBaseException();

        if (baseException is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                var baseInnerException = innerException.GetBaseException();

                if (baseInnerException is T innerExceptionOfT && filter(innerExceptionOfT))
                    return true;
            }
        }

        return baseException is T exceptionOfT && filter(exceptionOfT);
    }
}
