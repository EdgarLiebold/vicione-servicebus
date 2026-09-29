using System;
using System.Collections.Generic;
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
        foreach (Exception candidate in Traverse(exception))
        {
            for (var i = 0; i < exceptionTypes.Length; i++)
            {
                if (exceptionTypes[i].IsInstanceOfType(candidate))
                    return true;
            }
        }

        return false;
    }

    static bool Match<T>(Exception exception, Func<T, bool> filter)
        where T : Exception
    {
        foreach (Exception candidate in Traverse(exception))
        {
            if (candidate is T selected && filter(selected))
                return true;
        }

        return false;
    }

    static IEnumerable<Exception> Traverse(Exception exception)
    {
        var pending = new Stack<(Exception Exception, bool CheckBase)>();
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        var baseChecked = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        pending.Push((exception, true));

        while (pending.Count > 0)
        {
            (Exception current, bool checkBase) = pending.Pop();
            bool firstVisit = visited.Add(current);
            if (firstVisit)
                yield return current;

            if ((checkBase || current.InnerException is null) && baseChecked.Add(current))
            {
                Exception baseException = current.GetBaseException();
                if (baseException is not null && !ReferenceEquals(baseException, current))
                    pending.Push((baseException, false));
            }

            if (!firstVisit)
                continue;

            if (current is AggregateException aggregate)
            {
                for (var index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
                    pending.Push((aggregate.InnerExceptions[index], true));
            }
            else if (current.InnerException is { } inner)
                pending.Push((inner, false));
        }
    }
}
