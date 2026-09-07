using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers mediator runtime services and materializes its handler configuration.</summary>
internal sealed class ServiceCollectionMediatorConfigurator :
    RegistrationConfigurator,
    IMediatorRegistrationConfigurator
{
    Action<IMediatorRegistrationContext, IMediatorConfigurator>? _configure;

    /// <summary>Initializes mediator registration for a service collection.</summary>
    /// <param name="collection">The service collection that receives mediator services.</param>
    /// <param name="baseAddress">The loopback address used as the root for mediator endpoints.</param>
    public ServiceCollectionMediatorConfigurator(IServiceCollection collection, Uri? baseAddress)
        : base(collection, new MediatorContainerRegistrar(collection))
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

    /// <summary>Stores the callback that configures the materialized mediator pipeline.</summary>
    /// <param name="configure">The callback that receives resolved registrations and the mediator configurator.</param>
    public void ConfigureMediator(Action<IMediatorRegistrationContext, IMediatorConfigurator> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ThrowIfAlreadyConfigured(nameof(ConfigureMediator));
        _configure = configure;
    }

    static void AddViciOneServiceBusComponents(IServiceCollection collection)
    {
        collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, ConsumerKind>());

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
        collection.TryAddScoped<IScopedClientFactory>(provider =>
        {
            var mediator = provider.GetRequiredService<IScopedMediator>();
            var context = provider.GetRequiredService<Bind<IMediator, IScopedConsumeContextProvider>>().Value.GetContext();
            return new ScopedClientFactory(mediator, context);
        });
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

                context.ConfigureConsumerKinds(cfg);
            },
            provider.GetService<TimeProvider>() ?? TimeProvider.System);
    }
}
