using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers a future selected by its runtime type.</summary>
public static class FutureRegistrationConfiguratorRuntimeExtensions
{
    /// <summary>Adds a future and optional definition selected by runtime type.</summary>
    /// <param name="configurator">The application registration configurator.</param>
    /// <param name="futureType">The runtime future state-machine type.</param>
    /// <param name="futureDefinitionType">The runtime future definition type, or <see langword="null" /> for the default.</param>
    /// <returns>A configurator for the registered future.</returns>
    public static IFutureRegistrationConfigurator AddFuture(this IRegistrationConfigurator configurator, Type futureType,
        Type? futureDefinitionType = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(futureType);

        if (!futureType.IsClass || futureType.IsAbstract || futureType.ContainsGenericParameters
            || !futureType.TryGetSingleClosedGenericArguments(typeof(ISagaStateMachine<>), out Type[] types)
            || types.Length != 1
            || types[0] != typeof(FutureState))
        {
            throw new ArgumentException(
                $"The future type must be a concrete, closed state machine for {TypeCache<FutureState>.ShortName}: {TypeCache.GetShortName(futureType)}.",
                nameof(futureType));
        }

        var register = (IRegisterFuture)(Activator.CreateInstance(typeof(RegisterFuture<>).MakeGenericType(futureType))
            ?? throw new InvalidOperationException("The requested runtime type could not be activated."));

        return register.Register(configurator, futureDefinitionType);
    }

    interface IRegisterFuture
    {
        IFutureRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? futureDefinitionType);
    }


    sealed class RegisterFuture<TFuture> :
        IRegisterFuture
        where TFuture : class, ISagaStateMachine<FutureState>
    {
        public IFutureRegistrationConfigurator Register(IRegistrationConfigurator configurator, Type? futureDefinitionType)
        {
            return configurator.AddFuture<TFuture>(futureDefinitionType);
        }
    }
}
