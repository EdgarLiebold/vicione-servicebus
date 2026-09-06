using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures future result.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class FutureResultConfigurator<TCommand, TResult, TInput> :
    IFutureResultConfigurator<TResult, TInput>
    where TCommand : class
    where TInput : class
    where TResult : class
{
    readonly FutureResult<TCommand, TResult, TInput> _result;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result.</param>
    public FutureResultConfigurator(FutureResult<TCommand, TResult, TInput> result)
    {
        _result = result;
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(EventMessageFactory<FutureState, TInput, TResult> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TInput, TResult> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets completed using initializer.</summary>
    /// <param name="valueProvider">The value provider.</param>
    public void SetCompletedUsingInitializer(InitializerValueProvider<TInput> valueProvider)
    {
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>> FactoryAsync(BehaviorContext<FutureState, TInput> context)
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

        _result.Factory = MessageFactory<TResult>.Create((Func<BehaviorContext<FutureState, TInput>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>>>)FactoryAsync);
    }
}


/// <summary>Configures future result.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class FutureResultConfigurator<TCommand, TResult> :
    IFutureResultConfigurator<TResult>
    where TCommand : class
    where TResult : class
{
    readonly FutureResult<TCommand, TResult> _result;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="result">The result.</param>
    public FutureResultConfigurator(FutureResult<TCommand, TResult> result)
    {
        _result = result;
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(EventMessageFactory<FutureState, TResult> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets completed using factory.</summary>
    /// <param name="factoryMethod">The factory method.</param>
    public void SetCompletedUsingFactory(AsyncEventMessageFactory<FutureState, TResult> factoryMethod)
    {
        if (factoryMethod == null)
            throw new ArgumentNullException(nameof(factoryMethod));

        _result.Factory = MessageFactory<TResult>.Create(factoryMethod);
    }

    /// <summary>Sets completed using initializer.</summary>
    /// <param name="valueProvider">The value provider.</param>
    public void SetCompletedUsingInitializer(InitializerValueProvider valueProvider)
    {
        if (valueProvider == null)
            throw new ArgumentNullException(nameof(valueProvider));

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>> FactoryAsync(BehaviorContext<FutureState> context)
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

        _result.Factory = MessageFactory<TResult>.Create((Func<BehaviorContext<FutureState>, Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<TResult>>>)FactoryAsync);
    }
}
