using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>
/// Provides a registration filter configurator implementation.
/// </summary>
public class RegistrationFilterConfigurator :
    IRegistrationFilterConfigurator
{
    readonly CompositeFilter<Type> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RegistrationFilterConfigurator()
    {
        _filter = new CompositeFilter<Type>();

        Filter = new RegistrationFilter(_filter);
    }

    /// <summary>
    /// Gets the filter value.
    /// </summary>
    public IRegistrationFilter Filter { get; }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="types">The types value.</param>
    public void Include(params Type[] types)
    {
        _filter.Includes.Add(type => types.Any(x => x == type));
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Include<T>()
    {
        _filter.Includes.Add(type => type == typeof(T));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="types">The types value.</param>
    public void Exclude(params Type[] types)
    {
        _filter.Excludes.Add(type => types.Any(x => x == type));
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void Exclude<T>()
    {
        _filter.Excludes.Add(type => type == typeof(T));
    }
}
