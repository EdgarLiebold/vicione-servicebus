using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for dependency injection execute activity registration.</summary>
internal static class DependencyInjectionExecuteActivityRegistrationExtensions
{
    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterExecuteActivity<TActivity, TArguments>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection, IContainerRegistrar
        registrar)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        return new ExecuteActivityRegistrar<TActivity, TArguments>().Register(collection, registrar);
    }

    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments, TDefinition>(this IServiceCollection collection)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
        where TDefinition : class, IExecuteActivityDefinition<TActivity, TArguments>
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterExecuteActivity<TActivity, TArguments, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments, TDefinition>(this IServiceCollection collection,
        IContainerRegistrar registrar)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
        where TDefinition : class, IExecuteActivityDefinition<TActivity, TArguments>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        return new ExecuteActivityDefinitionRegistrar<TActivity, TArguments, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection, Type
        activityDefinitionType)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(activityDefinitionType);

        return RegisterExecuteActivity<TActivity, TArguments>(collection, new DependencyInjectionContainerRegistrar(collection), activityDefinitionType);
    }

    /// <summary>Registers execute activity.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection,
        IContainerRegistrar registrar, Type? activityDefinitionType)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        if (activityDefinitionType == null)
            return RegisterExecuteActivity<TActivity, TArguments>(collection, registrar);

        if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IExecuteActivityDefinition<,>), out Type[] types)
            || types[0] != typeof(TActivity)
            || types[1] != typeof(TArguments))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(activityDefinitionType)} is not an activity definition of {TypeCache<TActivity>.ShortName}",
                nameof(activityDefinitionType));
        }

        var register = (IExecuteActivityRegistrar)(Activator.CreateInstance(typeof(ExecuteActivityDefinitionRegistrar<,,>)
            .MakeGenericType(typeof(TActivity), typeof(TArguments), activityDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>Registers execute activity.</summary>
    /// <param name="collection">The collection.</param>
    /// <param name="registrar">The registrar.</param>
    /// <param name="activityType">The runtime activity type used by the operation.</param>
    /// <param name="activityDefinitionType">The runtime activity definition type used by the operation.</param>
    /// <returns>The execute activity registration produced by the operation.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity(this IServiceCollection collection, IContainerRegistrar registrar, Type activityType,
        Type? activityDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(activityType);

        if (activityType.TryGetSingleClosedGenericArguments(typeof(IActivity<,>), out Type[] _))
        {
            throw new ArgumentException($"Activities must be registered using RegisterActivity: {TypeCache.GetShortName(activityType)}",
                nameof(activityType));
        }

        if (!activityType.TryGetSingleClosedGenericArguments(typeof(IExecuteActivity<>), out Type[] argumentTypes))
        {
            throw new ArgumentException($"Execute activities must implement IExecuteActivity<TArguments>: {TypeCache.GetShortName(activityType)}",
                nameof(activityType));
        }

        if (activityDefinitionType != null)
        {
            if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IExecuteActivityDefinition<,>), out Type[] types)
                || types[0] != activityType
                || types[1] != argumentTypes[0])
            {
                throw new ArgumentException(
                    $"{TypeCache.GetShortName(activityDefinitionType)} is not an activity definition of {TypeCache.GetShortName(activityType)}",
                    nameof(activityDefinitionType));
            }

            var activityRegistrar = (IExecuteActivityRegistrar)(Activator.CreateInstance(typeof(ExecuteActivityDefinitionRegistrar<,,>)
                .MakeGenericType(activityType, argumentTypes[0], activityDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            return activityRegistrar.Register(collection, registrar);
        }


        var register = (IExecuteActivityRegistrar)(Activator.CreateInstance(typeof(ExecuteActivityRegistrar<,>)
            .MakeGenericType(activityType, argumentTypes[0])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }


    interface IExecuteActivityRegistrar
    {
        IExecuteActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class ExecuteActivityRegistrar<TActivity, TArguments> :
        IExecuteActivityRegistrar
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        public virtual IExecuteActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);

            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, ExecuteActivityConsumerKind>());
            collection.TryAddScoped<TActivity>();

            return registrar.GetOrAddRegistration<IExecuteActivityRegistration>(typeof(TActivity),
                _ => new ExecuteActivityRegistration<TActivity, TArguments>(registrar));
        }
    }


    sealed class ExecuteActivityDefinitionRegistrar<TActivity, TArguments, TDefinition> :
        ExecuteActivityRegistrar<TActivity, TArguments>
        where TDefinition : class, IExecuteActivityDefinition<TActivity, TArguments>
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        public override IExecuteActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<IExecuteActivityDefinition<TActivity, TArguments>, TDefinition>();

            return registration;
        }
    }
}
