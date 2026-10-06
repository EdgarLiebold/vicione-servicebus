using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores one configuration-options instance per exact option type.</summary>
public class OptionsSet :
    IOptionsSet
{
    readonly IDictionary<Type, IOptions> _options;

    /// <summary>Creates an empty options set.</summary>
    public OptionsSet()
    {
        _options = new Dictionary<Type, IOptions>();
    }

    /// <inheritdoc />
    public T Options<T>(Action<T>? configure = null)
        where T : IOptions, new()
    {
        if (_options.TryGetValue(typeof(T), out IOptions? existingOptions))
        {
            var options = (T)existingOptions;
            configure?.Invoke(options);
            return options;
        }

        var createdOptions = new T();
        _options.Add(typeof(T), createdOptions);

        configure?.Invoke(createdOptions);
        return createdOptions;
    }

    /// <inheritdoc />
    public T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_options.TryGetValue(typeof(T), out IOptions? existingOptions))
        {
            if (!ReferenceEquals(existingOptions, options))
                throw new ArgumentException($"The options type was already configured: {TypeCache<T>.ShortName}", nameof(options));
        }
        else
            _options.Add(typeof(T), options);

        configure?.Invoke(options);
        return options;
    }

    /// <inheritdoc />
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        if (_options.TryGetValue(typeof(T), out IOptions? existingOptions))
        {
            options = (T)existingOptions;
            return true;
        }

        options = default!;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        foreach (IOptions value in _options.Values)
        {
            if (value is T requested)
                yield return requested;
        }
    }

    /// <summary>Validates every stored option that implements <see cref="ISpecification"/>.</summary>
    /// <returns>The validation results from all option specifications.</returns>
    protected IEnumerable<ValidationResult> ValidateOptions()
    {
        return SelectOptions<ISpecification>().SelectMany(specification => specification.Validate());
    }
}
