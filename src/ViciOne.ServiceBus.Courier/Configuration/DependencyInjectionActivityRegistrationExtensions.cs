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
            return new ActivityRegistrar<TActivity, TArguments, TLog>().Register(collection, registrar);

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
