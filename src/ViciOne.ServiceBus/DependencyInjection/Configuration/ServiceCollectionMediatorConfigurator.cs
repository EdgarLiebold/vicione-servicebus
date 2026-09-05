using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a service collection mediator configurator implementation.
/// </summary>
public class ServiceCollectionMediatorConfigurator :
    RegistrationConfigurator,
    IMediatorRegistrationConfigurator
{
    Action<IMediatorRegistrationContext, IMediatorConfigurator> _configure = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="baseAddress">The base address value.</param>
    public ServiceCollectionMediatorConfigurator(IServiceCollection collection, Uri? baseAddress)
        : base(collection, new DependencyInjectionMediatorContainerRegistrar(collection))
    {
        IMediatorRegistrationContext CreateRegistrationContext(IServiceProvider provider)
        {
            var setter = provider.GetRequiredService<Bind<IMediator, ISetScopedConsumeContext>>();
            var registration = CreateRegistration(provider, setter.Value);
            return new MediatorRegistrationContext(registration);
        }

        collection.AddSingleton(e => MediatorFactory(e, baseAddress));
        collection.AddSingleton(CreateRegistrationContext);

        AddViciOneServiceBusComponents(collection);
    }

    /// <summary>
    /// Configures mediator.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ConfigureMediator(Action<IMediatorRegistrationContext, IMediatorConfigurator> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ThrowIfAlreadyConfigured(nameof(ConfigureMediator));
        _configure = configure;
    }

    static void AddViciOneServiceBusComponents(IServiceCollection collection)
    {
        collection.AddScoped<IScopedMediator, ScopedMediator>();

        collection.TryAddScoped<ScopedConsumeContextProvider>();
        collection.TryAddScoped<IScopedConsumeContextProvider>(provider => provider.GetRequiredService<ScopedConsumeContextProvider>());
        collection.AddSingleton(_ =>
            Bind<IMediator>.Create((ISetScopedConsumeContext)new SetScopedConsumeContext(provider =>
                provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value)));

        static Bind<IMediator, IScopedConsumeContextProvider> CreateScopeProvider(IServiceProvider provider)
        {
            var global = provider.GetRequiredService<IScopedConsumeContextProvider>();
            return Bind<IMediator>.Create((IScopedConsumeContextProvider)new TypedScopedConsumeContextProvider(global));
        }

        collection.TryAddScoped(CreateScopeProvider);
        collection.TryAddScoped(provider => provider.GetRequiredService<IScopedConsumeContextProvider>().GetContext() ?? MissingConsumeContext.Instance);

        collection.TryAddScoped(typeof(IRequestClient<>), typeof(GenericRequestClient<>));
    }

    IMediator MediatorFactory(IServiceProvider provider, Uri? baseAddress)
    {
        ConfigureLogContext(provider);

        var context = provider.GetRequiredService<IMediatorRegistrationContext>();
        MessageLimits limits = provider.GetService<MediatorMessageLimitsRegistration>()?.Limits
            ?? throw new ConfigurationException(
                "Message limits for bus 'mediator': MaxBodyBytes is not declared. Call mediator.Limits(...) with explicit byte limits.");

        return Bus.Factory.CreateMediator(
            baseAddress,
            cfg =>
            {
                cfg.Limits(limits);
                _configure?.Invoke(context, cfg);

                cfg.ConfigureConsumers(context);
                cfg.ConfigureSagas(context);
            },
            provider.GetService<TimeProvider>() ?? TimeProvider.System);
    }
}
