using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Defines the operations required by test harness.</summary>
public interface ITestHarness :
    IBaseTestHarness
{
    /// <summary>Gets the bus.</summary>
    IBus Bus { get; }

    /// <summary>Gets the scope.</summary>
    IServiceScope Scope { get; }

    /// <summary>Gets the provider.</summary>
    IServiceProvider Provider { get; }

    /// <summary>Gets the endpoint name formatter.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>Returns a task completion source that is automatically canceled when the test is canceled.</summary>
    /// <typeparam name="T">The task type.</typeparam>
    /// <returns>The task.</returns>
    TaskCompletionSource<T> GetTask<T>();

    /// <summary>Gets consumer harness.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <returns>The consumer harness.</returns>
    IConsumerTestHarness<T> GetConsumerHarness<T>()
        where T : class, IConsumer;

    /// <summary>Gets saga harness.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <returns>The saga harness.</returns>
    ISagaTestHarness<T> GetSagaHarness<T>()
        where T : class, ISaga;

    /// <summary>Gets saga state machine harness.</summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <returns>The saga state machine harness.</returns>
    ISagaStateMachineTestHarness<TStateMachine, T> GetSagaStateMachineHarness<TStateMachine, T>()
        where TStateMachine : class, SagaStateMachine<T>
        where T : class, SagaStateMachineInstance;

    /// <summary>Gets request client.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The request client.</returns>
    IRequestClient<T> GetRequestClient<T>()
        where T : class;

    /// <summary>Use the endpoint name formatter to get the send endpoint for the consumer type.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetConsumerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IConsumer;

    /// <summary>Use the endpoint name formatter to get the send endpoint for the message handler by message type.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetHandlerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Returns the endpoint address for the specified consumer type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The consumer address.</returns>
    Uri GetConsumerAddress<T>()
        where T : class, IConsumer;

    /// <summary>Returns the endpoint address for the specified handler type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The handler address.</returns>
    Uri GetHandlerAddress<T>()
        where T : class;

    /// <summary>Use the endpoint name formatter to get the send endpoint for the saga type.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetSagaEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ISaga;

    /// <summary>Returns the endpoint address for the saga.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The saga address.</returns>
    Uri GetSagaAddress<T>()
        where T : class, ISaga;

    /// <summary>Use the endpoint name formatter to get the execute send endpoint for the activity type.</summary>
    /// <typeparam name="T">The activity type.</typeparam>
    /// <typeparam name="TArguments">The argument type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendEndpoint> GetExecuteActivityEndpointAsync<T, TArguments>(CancellationToken cancellationToken = default)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Returns the endpoint address for the execute activity.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <returns>The execute activity address.</returns>
    Uri GetExecuteActivityAddress<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);
}
