using System;
using ViciOne.ServiceBus.ExceptionFilters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an exception specification implementation.
/// </summary>
public abstract class ExceptionSpecification :
    IExceptionConfigurator
{
    readonly CompositeFilter<Exception> _exceptionFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected ExceptionSpecification()
    {
        _exceptionFilter = new CompositeFilter<Exception>();
        Filter = new CompositeExceptionFilter(_exceptionFilter);
    }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    protected IExceptionFilter Filter { get; }

    /// <summary>
    /// Creates filter snapshot.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected IExceptionFilter CreateFilterSnapshot()
    {
        return new CompositeExceptionFilter(_exceptionFilter.CreateSnapshot());
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <param name="exceptionTypes">The exception types value.</param>
    public void Handle(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Includes.Add(exception => Match(exception, snapshot));
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Handle<T>()
        where T : Exception
    {
        _exceptionFilter.Includes.Add(exception => Match(exception, typeof(T)));
    }

    /// <summary>
    /// Performs the handle operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
    public void Handle<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);
        _exceptionFilter.Includes.Add(exception => Match(exception, filter));
    }

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <param name="exceptionTypes">The exception types value.</param>
    public void Ignore(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Excludes.Add(exception => Match(exception, snapshot));
    }

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Ignore<T>()
        where T : Exception
    {
        _exceptionFilter.Excludes.Add(exception => Match(exception, typeof(T)));
    }

    /// <summary>
    /// Performs the ignore operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="filter">The filter value.</param>
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
