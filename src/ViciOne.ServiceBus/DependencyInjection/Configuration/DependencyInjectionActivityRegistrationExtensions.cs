using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for dependency injection activity registration.
/// </summary>
public static class DependencyInjectionActivityRegistrationExtensions
{
    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return RegisterActivity<TActivity, TArguments, TLog>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, IContainerRegistrar registrar)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return new ActivityRegistrar<TActivity, TArguments, TLog>().Register(collection, registrar);
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog, TDefinition>(this IServiceCollection collection)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
    {
        return RegisterActivity<TActivity, TArguments, TLog, TDefinition>(collection, new DependencyInjectionContainerRegistrar(collection));
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <typeparam name="TDefinition">The t definition type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog, TDefinition>(this IServiceCollection collection,
        IContainerRegistrar registrar)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
        where TDefinition : class, IActivityDefinition<TActivity, TArguments, TLog>
    {
        return new ActivityDefinitionRegistrar<TActivity, TArguments, TLog, TDefinition>().Register(collection, registrar);
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <param name="activityDefinitionType">The activity definition type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, Type activityDefinitionType)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        return RegisterActivity<TActivity, TArguments, TLog>(collection, new DependencyInjectionContainerRegistrar(collection), activityDefinitionType);
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <typeparam name="TArguments">The t arguments type.</typeparam>
    /// <typeparam name="TLog">The t log type.</typeparam>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    /// <param name="activityDefinitionType">The activity definition type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity<TActivity, TArguments, TLog>(this IServiceCollection collection, IContainerRegistrar registrar,
        Type? activityDefinitionType)
        where TActivity : class, IActivity<TArguments, TLog>
        where TArguments : class
        where TLog : class
    {
        if (activityDefinitionType == null)
            return RegisterActivity<TActivity, TArguments, TLog>(collection, registrar);

        if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>), out Type[] types) || types[0] != typeof(TActivity))
        {
            throw new ArgumentException(
                $"{TypeCache.GetShortName(activityDefinitionType)} is not an activity definition of {TypeCache<TActivity>.ShortName}",
                nameof(activityDefinitionType));
        }

        var register = (IActivityRegistrar)(Activator.CreateInstance(typeof(ActivityDefinitionRegistrar<,,,>)
            .MakeGenericType(typeof(TActivity), typeof(TArguments), typeof(TLog), activityDefinitionType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(collection, registrar);
    }

    /// <summary>
    /// Performs the register activity operation.
    /// </summary>
    /// <param name="collection">The collection value.</param>
    /// <param name="registrar">The registrar value.</param>
    /// <param name="activityType">The activity type value.</param>
    /// <param name="activityDefinitionType">The activity definition type value.</param>
    /// <returns>The result of the operation.</returns>
    public static IActivityRegistration RegisterActivity(this IServiceCollection collection, IContainerRegistrar registrar, Type activityType,
        Type? activityDefinitionType = null)
    {
        if (!activityType.TryGetSingleClosedGenericArguments(typeof(IActivity<,>), out Type[] argumentTypes))
        {
            throw new ArgumentException($" activities must implement IActivity<TArguments, TLog>: {TypeCache.GetShortName(activityType)}",
                nameof(activityType));
        }

        if (activityDefinitionType != null)
        {
            if (!activityDefinitionType.TryGetSingleClosedGenericArguments(typeof(IActivityDefinition<,,>), out Type[] types) || types[0] != activityType)
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
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumerKind, ActivityConsumerKind>());
            collection.TryAddScoped<TActivity>();

            return registrar.GetOrAddRegistration<IActivityRegistration>(typeof(TActivity),
                _ => new ActivityRegistration<TActivity, TArguments, TLog>(registrar));
        }
    }


    class ActivityDefinitionRegistrar<TActivity, TArguments, TLog, TDefinition> :
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
