using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future fault.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TFault">The fault type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FutureFaultConfigurator<TCommand, TFault, TInput> :
    IFutureFaultConfigurator<TFault, TInput>
    where TInput : class
    where TFault : class
    where TCommand : class
{
    readonly FutureFault<TCommand, TFault, TInput> _fault;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="fault">The fault.</param>
    public FutureFaultConfigurator(FutureFault<TCommand, TFault, TInput> fault)
    {
        _fault = fault;
    }

    /// <summary>Sets faulted using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetFaultedUsingFactory(EventMessageFactory<FutureState, TInput, TFault> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets faulted using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetFaultedUsingFactory(AsyncEventMessageFactory<FutureState, TInput, TFault> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets faulted using initializer.</summary>
    /// <param name="valueProvider">The value provider.</param>
    public void SetFaultedUsingInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> FactoryAsync(BehaviorContext<FutureState, TInput> context)
        {
            return context.InitAsync<TFault>(valueProvider(context));
        }

        _fault.Factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)FactoryAsync);
    }
}


/// <summary>Configures future fault.</summary>
/// <typeparam name="TFault">The fault type.</typeparam>
public class FutureFaultConfigurator<TFault> :
    IFutureFaultConfigurator<TFault>
    where TFault : class
{
    readonly FutureFault<TFault> _fault;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="fault">The fault.</param>
    public FutureFaultConfigurator(FutureFault<TFault> fault)
    {
        _fault = fault;
    }

    /// <summary>Sets faulted using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetFaultedUsingFactory(EventMessageFactory<FutureState, TFault> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets faulted using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetFaultedUsingFactory(AsyncEventMessageFactory<FutureState, TFault> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _fault.Factory = MessageFactory<TFault>.Create(factoryMethod);
    }

    /// <summary>Sets faulted using initializer.</summary>
    /// <param name="valueProvider">The value provider.</param>
    public void SetFaultedUsingInitializer(InitializerValueProvider valueProvider)
    {
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>> FactoryAsync(BehaviorContext<FutureState> context)
        {
            return context.InitAsync<TFault>(valueProvider(context));
        }

        _fault.Factory = MessageFactory<TFault>.Create((Func<BehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TFault>>>)FactoryAsync);
    }
}
