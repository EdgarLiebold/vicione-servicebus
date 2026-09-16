using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Initializers.Conventions;


namespace ViciOne.ServiceBus.Initializers;

/// <summary>Publishes an element-immutable convention snapshot and rejects registration after publication.</summary>
internal sealed class InitializerConventionRegistry
{
    readonly List<IInitializerConvention> _conventions;
    readonly object _lock = new();
    IReadOnlyList<IInitializerConvention>? _snapshot;
    bool _frozen;

    internal InitializerConventionRegistry(IEnumerable<IInitializerConvention> conventions)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        _conventions = conventions.ToList();
        if (_conventions.Exists(static convention => convention is null))
            throw new ArgumentException("The convention collection cannot contain null elements.", nameof(conventions));
    }

    internal IReadOnlyList<IInitializerConvention> Conventions
    {
        get
        {
            lock (_lock)
            {
                _frozen = true;
                return _snapshot ??= _conventions.AsReadOnly();
            }
        }
    }

    internal void Add<T>()
        where T : IInitializerConvention, new()
    {
        lock (_lock)
        {
            if (_frozen)
                throw new InvalidOperationException("Message initializer conventions are immutable after the convention snapshot is first read.");

            if (_conventions.Any(static convention => convention.GetType() == typeof(T)))
                return;

            _conventions.Add(new T());
        }
    }
}
