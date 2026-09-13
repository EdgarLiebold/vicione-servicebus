using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures a future result produced from an input event.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
/// <typeparam name="TInput">The event contract that triggers completion.</typeparam>
internal sealed class FutureResultConfigurator<TCommand, TResult, TInput> :
    IFutureResultConfigurator<TResult, TInput>
    where TCommand : class
    where TInput : class
    where TResult : class
{
    readonly FutureResult<TCommand, TResult, TInput> _result;

    /// <summary>Creates a configurator for an event-driven future result.</summary>
    /// <param name="result">The result producer to configure.</param>
    public FutureResultConfigurator(FutureResult<TCommand, TResult, TInput> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;
    }

    /// <summary>Sets the synchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The result factory invoked for the triggering event.</param>
    public void SetResultFactory(EventMessageFactory<FutureState, TInput, TResult> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The asynchronous result factory invoked for the triggering event.</param>
    public void SetResultFactory(AsyncEventMessageFactory<FutureState, TInput, TResult> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the successful future result.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    public void SetResultInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>> FactoryAsync(IBehaviorContext<FutureState, TInput> context)
        {
            return MessageInitializerCache<TResult>.InitializeMessageAsync(context, valueProvider(context), new object?[]
            {
                new
                {
                    context.Saga.Completed,
                    context.Saga.Created,
                    context.Saga.Faulted,
                    context.Saga.Location,
                },
                context.GetCommand<TCommand>(),
                context.Message
            });
        }

        _result.Factory = MessageFactory<TResult>.Create((Func<IBehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>>>)FactoryAsync);
    }
}


/// <summary>Configures a future result produced from future state.</summary>
/// <typeparam name="TCommand">The command contract stored by the future.</typeparam>
/// <typeparam name="TResult">The successful future result contract.</typeparam>
internal sealed class FutureResultConfigurator<TCommand, TResult> :
    IFutureResultConfigurator<TResult>
    where TCommand : class
    where TResult : class
{
    readonly FutureResult<TCommand, TResult> _result;

    /// <summary>Creates a configurator for a result produced from future state.</summary>
    /// <param name="result">The result producer to configure.</param>
    public FutureResultConfigurator(FutureResult<TCommand, TResult> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;
    }

    /// <summary>Sets the synchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The result factory invoked with future state.</param>
    public void SetResultFactory(EventMessageFactory<FutureState, TResult> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets the asynchronous factory that creates the successful future result.</summary>
    /// <param name="factoryMethod">The asynchronous result factory invoked with future state.</param>
    public void SetResultFactory(AsyncEventMessageFactory<FutureState, TResult> factoryMethod)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets the initializer values used to create the successful future result.</summary>
    /// <param name="valueProvider">The provider of additional result property values.</param>
    public void SetResultInitializer(InitializerValueProvider valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>> FactoryAsync(IBehaviorContext<FutureState> context)
        {
            return MessageInitializerCache<TResult>.InitializeMessageAsync(context, valueProvider(context), new object?[]
            {
                new
                {
                    context.Saga.Completed,
                    context.Saga.Created,
                    context.Saga.Faulted,
                    context.Saga.Location,
                },
                context.GetCommand<TCommand>()
            });
        }

        _result.Factory = MessageFactory<TResult>.Create((Func<IBehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>>>)FactoryAsync);
    }
}
