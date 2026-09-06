using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Initializers.Conventions;


namespace ViciOne.ServiceBus.Initializers;

internal sealed class InitializerConventionRegistry
{
    readonly List<IInitializerConvention> _conventions;
    readonly object _lock = new();
    IInitializerConvention[]? _snapshot;
    bool _frozen;

    internal InitializerConventionRegistry(IEnumerable<IInitializerConvention> conventions)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        _conventions = conventions.ToList();
    }

    internal IReadOnlyList<IInitializerConvention> Conventions
    {
        get
        {
            lock (_lock)
            {
                _frozen = true;
                return _snapshot ??= _conventions.ToArray();
            }
        }
    }

    internal void Add<T>()
        where T : IInitializerConvention, new()
    {
        lock (_lock)
        {
            if (_frozen)
                throw new InvalidOperationException("Message initializer conventions are immutable after the first initializer is created.");

            if (_conventions.Any(static convention => convention.GetType() == typeof(T)))
                return;

            _conventions.Add(new T());
        }
    }
}
