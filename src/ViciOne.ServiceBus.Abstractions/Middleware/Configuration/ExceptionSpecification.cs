using System;
using ViciOne.ServiceBus.ExceptionFilters;

namespace ViciOne.ServiceBus.Configuration;

public abstract class ExceptionSpecification :
    IExceptionConfigurator
{
    readonly CompositeFilter<Exception> _exceptionFilter;

    protected ExceptionSpecification()
    {
        _exceptionFilter = new CompositeFilter<Exception>();
        Filter = new CompositeExceptionFilter(_exceptionFilter);
    }

    protected IExceptionFilter Filter { get; }

    protected IExceptionFilter CreateFilterSnapshot()
    {
        return new CompositeExceptionFilter(_exceptionFilter.CreateSnapshot());
    }

    public void Handle(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Includes += exception => Match(exception, snapshot);
    }

    public void Handle<T>()
        where T : Exception
    {
        _exceptionFilter.Includes += exception => Match(exception, typeof(T));
    }

    public void Handle<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);
        _exceptionFilter.Includes += exception => Match(exception, filter);
    }

    public void Ignore(params Type[] exceptionTypes)
    {
        var snapshot = SnapshotTypes(exceptionTypes);
        _exceptionFilter.Excludes += exception => Match(exception, snapshot);
    }

    public void Ignore<T>()
        where T : Exception
    {
        _exceptionFilter.Excludes += exception => Match(exception, typeof(T));
    }

    public void Ignore<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);
        _exceptionFilter.Excludes += exception => Match(exception, filter);
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
