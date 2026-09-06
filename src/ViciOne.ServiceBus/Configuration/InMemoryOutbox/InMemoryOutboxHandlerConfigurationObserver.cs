using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Configures a message retry for a handler, on the handler configurator, which is constrained to
/// the message types for that handler, and only applies to the handler.
/// </summary>
public class InMemoryOutboxHandlerConfigurationObserver :
    IHandlerConfigurationObserver
{
    readonly Action<IOutboxConfigurator>? _configure;
    readonly ISetScopedConsumeContext? _setter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxHandlerConfigurationObserver(IRegistrationContext context, Action<IOutboxConfigurator>? configure)
        : this(context as ISetScopedConsumeContext ?? throw new ArgumentException(nameof(context)), configure)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="setter">The setter.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public InMemoryOutboxHandlerConfigurationObserver(ISetScopedConsumeContext? setter, Action<IOutboxConfigurator>? configure)
    {
        _setter = setter;
        _configure = configure;
    }

    void IHandlerConfigurationObserver.HandlerConfigured<T>(IHandlerConfigurator<T> configurator)
    {
        var specification = new InMemoryOutboxSpecification<T>(_setter);

        _configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }
}
