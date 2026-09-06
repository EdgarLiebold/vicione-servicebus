using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Caches registration data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class RegistrationCache<T> :
    IRegistrationCache<T>
{
    readonly IDictionary<Type, T> _dictionary;
    readonly Func<Type, T>? _missingRegistrationFactory = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="missingRegistrationFactory">The missing registration factory.</param>
    public RegistrationCache(Func<Type, T>? missingRegistrationFactory = default)
    {
        _missingRegistrationFactory = missingRegistrationFactory;
        _dictionary = new Dictionary<Type, T>();
    }

    /// <summary>Gets the values.</summary>
    public IEnumerable<T> Values => _dictionary.Values;

    /// <summary>Gets or add.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <param name="missingRegistrationFactory">The missing registration factory.</param>
    /// <returns>The or add.</returns>
    public T GetOrAdd(Type type, Func<Type, T>? missingRegistrationFactory = default)
    {
        lock (_dictionary)
        {
            if (_dictionary.TryGetValue(type, out var value))
                return value;

            Func<Type, T> factory = missingRegistrationFactory ?? _missingRegistrationFactory
                ?? throw new ArgumentNullException(nameof(missingRegistrationFactory));

            value = factory(type);
            _dictionary.Add(type, value);

            return value;
        }
    }
}
