using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides runtime future registration extensions.
/// </summary>
public static class FutureRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>
    /// Adds a future registration, along with an optional definition
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="futureType"></param>
    /// <param name="futureDefinitionType">The future definition type</param>
    public static IFutureRegistrationConfigurator AddFuture(this IRegistrationConfigurator configurator, Type futureType,
        Type? futureDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(futureType);

        if (!futureType.TryGetSingleClosedGenericArguments(typeof(SagaStateMachine<>), out Type[] types)
            || types.Length != 1
            || types[0] != typeof(FutureState))
            throw new ArgumentException($"The type is not a future: {TypeCache.GetShortName(futureType)}", nameof(futureType));

        var register = (IRegisterFuture)(Activator.CreateInstance(typeof(RegisterFuture<>).MakeGenericType(futureType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, futureDefinitionType);
    }

    interface IRegisterFuture
    {
        IFutureRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? futureDefinitionType);
    }


    class RegisterFuture<TFuture> :
        IRegisterFuture
        where TFuture : class, SagaStateMachine<FutureState>
    {
        public IFutureRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? futureDefinitionType)
        {
            return configurator.AddFuture<TFuture>(futureDefinitionType);
        }
    }
}
