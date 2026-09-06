using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a terminal future fault produced from an input event.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
/// <typeparam name="TInput">The event contract that triggers the fault.</typeparam>
internal sealed class FutureFaultConfigurator<TCommand, TFault, TInput> :
    IFutureFaultConfigurator<TFault, TInput>
    where TInput : class
    where TFault : class
    where TCommand : class
{
    readonly FutureFault<TCommand, TFault, TInput> _fault;

    /// <summary>Creates a configurator for an event-driven future fault.</summary>
    /// <param name="fault">The fault producer to configure.</param>
    public FutureFaultConfigurator(FutureFault<TCommand, TFault, TInput> fault)
    {
        ArgumentNullException.ThrowIfNull(fault);
        _fault = fault;
    }

    /// <summary>Sets the synchronous factory that creates the terminal future fault.</summary>
    /// <param name="factoryMethod">The fault factory invoked for the triggering event.</param>
    public void SetFaultFactory(EventMessageFactory<FutureState, TInput, TFault> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the terminal future fault.</summary>
    /// <param name="factoryMethod">The asynchronous fault factory invoked for the triggering event.</param>
    public void SetFaultFactory(AsyncEventMessageFactory<FutureState, TInput, TFault> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the terminal future fault.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    public void SetFaultInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> FactoryAsync(BehaviorContext<FutureState, TInput> context)
        {
            return context.InitAsync<TFault>(valueProvider(context));
        }

        _fault.Factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)FactoryAsync);
    }
}


/// <summary>Configures a terminal future fault produced from future state.</summary>
/// <typeparam name="TFault">The terminal future fault contract.</typeparam>
internal sealed class FutureFaultConfigurator<TFault> :
    IFutureFaultConfigurator<TFault>
    where TFault : class
{
    readonly FutureFault<TFault> _fault;

    /// <summary>Creates a configurator for a fault produced from future state.</summary>
    /// <param name="fault">The fault producer to configure.</param>
    public FutureFaultConfigurator(FutureFault<TFault> fault)
    {
        ArgumentNullException.ThrowIfNull(fault);
        _fault = fault;
    }

    /// <summary>Sets the synchronous factory that creates the terminal future fault.</summary>
    /// <param name="factoryMethod">The fault factory invoked with future state.</param>
    public void SetFaultFactory(EventMessageFactory<FutureState, TFault> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the terminal future fault.</summary>
    /// <param name="factoryMethod">The asynchronous fault factory invoked with future state.</param>
    public void SetFaultFactory(AsyncEventMessageFactory<FutureState, TFault> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the terminal future fault.</summary>
    /// <param name="valueProvider">The provider of additional fault property values.</param>
    public void SetFaultInitializer(InitializerValueProvider valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> FactoryAsync(BehaviorContext<FutureState> context)
        {
            return context.InitAsync<TFault>(valueProvider(context));
        }

        _fault.Factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)FactoryAsync);
    }
}
