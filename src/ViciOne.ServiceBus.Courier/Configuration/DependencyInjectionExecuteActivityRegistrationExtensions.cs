using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers execute-only Courier activity implementations and optional definitions in dependency injection.</summary>
internal static class DependencyInjectionExecuteActivityRegistrationExtensions
{
    /// <summary>Registers a scoped execute-only activity with a generated endpoint definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and registration metadata.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterExecuteActivity<TActivity, TArguments>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a scoped execute-only activity through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration metadata.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection, IContainerRegistrar
        registrar)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        return new ExecuteActivityRegistrar<TActivity, TArguments>().Register(collection, registrar);
    }

    /// <summary>Registers a scoped execute-only activity with a compile-time endpoint definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The service collection that receives the activity, definition, and registration metadata.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments, TDefinition>(this IServiceCollection collection)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
        where TDefinition : class, IExecuteActivityDefinition<TActivity, TArguments>
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterExecuteActivity<TActivity, TArguments, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a scoped execute-only activity and compile-time definition through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
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

    /// <summary>Registers a scoped execute-only activity with a runtime endpoint definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The service collection that receives the activity, definition, and registration metadata.</param>
    /// <param name="activityDefinitionType">The endpoint-definition type whose activity and arguments must match.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
    public static IExecuteActivityRegistration RegisterExecuteActivity<TActivity, TArguments>(this IServiceCollection collection, Type
        activityDefinitionType)
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(activityDefinitionType);

        return RegisterExecuteActivity<TActivity, TArguments>(collection, new DependencyInjectionContainerRegistrar(collection), activityDefinitionType);
    }

    /// <summary>Registers a scoped execute-only activity and optional runtime definition through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <param name="activityDefinitionType">The optional endpoint-definition type whose activity and arguments must match.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
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

    /// <summary>Registers an execute-only activity and optional definition from runtime types.</summary>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <param name="activityType">The runtime type implementing exactly one closed execute-only activity contract.</param>
    /// <param name="activityDefinitionType">The optional endpoint-definition type whose activity and arguments must match.</param>
    /// <returns>The existing or newly added execution-activity registration.</returns>
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
