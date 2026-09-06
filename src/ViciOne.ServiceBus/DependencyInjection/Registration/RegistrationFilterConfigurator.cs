using System;
using System.Linq;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.DependencyInjection.Registration;

/// <summary>Configures registration filter.</summary>
public class RegistrationFilterConfigurator :
    IRegistrationFilterConfigurator
{
    readonly CompositeFilter<Type> _filter;

    /// <summary>Initializes a new instance.</summary>
    public RegistrationFilterConfigurator()
    {
        _filter = new CompositeFilter<Type>();

        Filter = new RegistrationFilter(_filter);
    }

    /// <summary>Gets the filter.</summary>
    public IRegistrationFilter Filter { get; }

    /// <summary>Includes the selected value.</summary>
    /// <param name="types">The types.</param>
    public void Include(params Type[] types)
    {
        _filter.Includes.Add(type => types.Any(x => x == type));
    }

    /// <summary>Includes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Include<T>()
    {
        _filter.Includes.Add(type => type == typeof(T));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="types">The types.</param>
    public void Exclude(params Type[] types)
    {
        _filter.Excludes.Add(type => types.Any(x => x == type));
    }

    /// <summary>Excludes the selected value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void Exclude<T>()
    {
        _filter.Excludes.Add(type => type == typeof(T));
    }
}
