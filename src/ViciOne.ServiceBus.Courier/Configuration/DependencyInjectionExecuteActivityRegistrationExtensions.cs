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
            return new ExecuteActivityRegistrar<TActivity, TArguments>().Register(collection, registrar);

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
