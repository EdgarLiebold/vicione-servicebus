using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a registration cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class RegistrationCache<T> :
    IRegistrationCache<T>
{
    readonly IDictionary<Type, T> _dictionary;
    readonly Func<Type, T>? _missingRegistrationFactory = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="missingRegistrationFactory">The missing registration factory value.</param>
    public RegistrationCache(Func<Type, T>? missingRegistrationFactory = default)
    {
        _missingRegistrationFactory = missingRegistrationFactory;
        _dictionary = new Dictionary<Type, T>();
    }

    /// <summary>
    /// Gets the values value.
    /// </summary>
    public IEnumerable<T> Values => _dictionary.Values;

    /// <summary>
    /// Gets or add.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="missingRegistrationFactory">The missing registration factory value.</param>
    /// <returns>The result of the operation.</returns>
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
