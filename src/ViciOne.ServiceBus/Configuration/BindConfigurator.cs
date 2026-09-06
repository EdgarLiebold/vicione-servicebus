using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures bind.</summary>
/// <typeparam name="TLeft">The left type.</typeparam>
public class BindConfigurator<TLeft> :
    IBindConfigurator<TLeft>
    where TLeft : class, PipeContext
{
    readonly IPipeConfigurator<TLeft> _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public BindConfigurator(IPipeConfigurator<TLeft> configurator)
    {
        _configurator = configurator;
    }

    void IBindConfigurator<TLeft>.Source<T>(IPipeContextSource<T, TLeft> source, Action<IBindConfigurator<TLeft, T>> configureTarget)
    {
        var specification = new BindPipeSpecification<TLeft, T>(source);

        configureTarget?.Invoke(specification);

        _configurator.AddPipeSpecification(specification);
    }
}
