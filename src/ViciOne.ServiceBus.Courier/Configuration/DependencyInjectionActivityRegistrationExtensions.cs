using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers compensatable Courier activity implementations and optional definitions in dependency injection.</summary>
internal static class DependencyInjectionActivityRegistrationExtensions
{
    /// <summary>Registers a scoped compensatable activity with generated endpoint definitions.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and registration metadata.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterActivity<TActivity, TArguments, TLog>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a scoped compensatable activity through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration metadata.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, IContainerRegistrar registrar)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        return new ActivityRegistrar<TActivity, TArguments, TLog>().Register(collection, registrar);
    }

    /// <summary>Registers a scoped compensatable activity with a compile-time endpoint definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The service collection that receives the activity, definition, and registration metadata.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog, TDefinition>(this IServiceCollection collection)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
    {
        ArgumentNullException.ThrowIfNull(collection);

        return RegisterActivity<TActivity, TArguments, TLog, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>Registers a scoped compensatable activity and compile-time definition through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <typeparam name="TDefinition">The definition type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog, TDefinition>(this IServiceCollection collection,
        IContainerRegistrar registrar)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        return new ActivityDefinitionRegistrar<TActivity, TArguments, TLog, TDefinition>().Register(collection, registrar);
    }

    /// <summary>Registers a scoped compensatable activity with an optional runtime endpoint definition.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="collection">The service collection that receives the activity, definition, and registration metadata.</param>
    /// <param name="activityDefinitionType">The endpoint-definition type whose complete activity signature must match.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, Type activityDefinitionType)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(activityDefinitionType);

        return RegisterActivity<TActivity, TArguments, TLog>(collection, new DependencyInjectionContainerRegistrar(collection), activityDefinitionType);
    }

    /// <summary>Registers a scoped compensatable activity and optional runtime definition through an explicit container registrar.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <param name="activityDefinitionType">The optional endpoint-definition type whose complete activity signature must match.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, IContainerRegistrar registrar,
        Type? activityDefinitionType)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);

        if (activityDefinitionType == null)
            return RegisterActivity<TActivity, TArguments, TLog>(collection, registrar);

        if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>), out Type[] types)
            || types[0] != typeof(TActivity)
            || types[1] != typeof(TArguments)
            || types[2] != typeof(TLog))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(activityDefinitionType)} is not an activity definition of {TypeCache<TActivity>.ShortName}",
                nameof(activityDefinitionType));
        }

        var register = (IActivityRegistrar)(Activator.CreateInstance(typeof(ActivityDefinitionRegistrar<,,,>)
            .MakeGenericType(typeof(TActivity), typeof(TArguments), typeof(TLog), activityDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>Registers a compensatable activity and optional definition from runtime types.</summary>
    /// <param name="collection">The service collection that receives the activity and consumer-kind services.</param>
    /// <param name="registrar">The container integration that owns registration and definition metadata.</param>
    /// <param name="activityType">The runtime type implementing exactly one closed compensatable activity contract.</param>
    /// <param name="activityDefinitionType">The optional endpoint-definition type whose complete activity signature must match.</param>
    /// <returns>The existing or newly added compensatable-activity registration.</returns>
    public static IActivityRegistration RegisterActivity(this IServiceCollection collection, IContainerRegistrar registrar, Type activityType,
        Type? activityDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(activityType);

        if (!activityType.TryGetSingleClosedGenericArguments(typeof(IActivity<,>), out Type[] argumentTypes))
        {
            throw new ArgumentException($"Activities must implement IActivity<TArguments, TLog>: {TypeCache.GetShortName(activityType)}",
                nameof(activityType));
        }

        if (activityDefinitionType != null)
        {
            if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>), out Type[] types)
                || types[0] != activityType
                || types[1] != argumentTypes[0]
                || types[2] != argumentTypes[1])
            {
                throw new ArgumentException(
                    $"{TypeCache.GetShortName(activityDefinitionType)} is not an activity definition of {TypeCache.GetShortName(activityType)}",
                    nameof(activityDefinitionType));
            }

            var activityRegistrar = (IActivityRegistrar)(Activator.CreateInstance(typeof(ActivityDefinitionRegistrar<,,,>)
                .MakeGenericType(activityType, argumentTypes[0], argumentTypes[1], activityDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            return activityRegistrar.Register(collection, registrar);
        }


        var register = (IActivityRegistrar)(Activator.CreateInstance(typeof(ActivityRegistrar<,,>)
            .MakeGenericType(activityType, argumentTypes[0], argumentTypes[1])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }


    interface IActivityRegistrar
    {
        IActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar);
    }


    class ActivityRegistrar<TActivity, TArguments, TLog> :
        IActivityRegistrar
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        public virtual IActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(registrar);

            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, ActivityConsumerKind>());
            collection.TryAddScoped<TActivity>();

            return registrar.GetOrAddRegistration<IActivityRegistration>(typeof(TActivity),
                _ => new ActivityRegistration<TActivity, TArguments, TLog>(registrar));
        }
    }


    sealed class ActivityDefinitionRegistrar<TActivity, TArguments, TLog, TDefinition> :
        ActivityRegistrar<TActivity, TArguments, TLog>
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        public override IActivityRegistration Register(IServiceCollection collection, IContainerRegistrar registrar)
        {
            var registration = base.Register(collection, registrar);

            registrar.AddDefinition<IActivityDefinition<TActivity, TArguments, TLog>, TDefinition>();

            return registration;
        }
    }
}
