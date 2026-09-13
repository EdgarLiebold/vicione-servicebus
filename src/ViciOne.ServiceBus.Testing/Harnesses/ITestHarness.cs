using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides access to a dependency-injection test host, its bus, and its registered component harnesses.</summary>
public interface ITestHarness :
    IBaseTestHarness
{
    /// <summary>Gets the hosted bus.</summary>
    IBus Bus { get; }

    /// <summary>Gets the service scope that owns scoped test dependencies.</summary>
    IServiceScope Scope { get; }

    /// <summary>Gets the root service provider.</summary>
    IServiceProvider Provider { get; }

    /// <summary>Gets the formatter used to derive registered endpoint names.</summary>
    IEndpointNameFormatter EndpointNameFormatter { get; }

    /// <summary>Creates a completion source canceled by either the current test scope or the supplied token.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="cancellationToken">An additional token that can cancel the completion source.</param>
    /// <returns>A completion source whose continuations run asynchronously.</returns>
    TaskCompletionSource<T> CreateTaskCompletionSource<T>(CancellationToken cancellationToken = default);

    /// <summary>Gets the registered harness for a consumer.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <returns>The consumer harness.</returns>
    IConsumerTestHarness<T> GetConsumerHarness<T>()
        where T : class, IConsumer;

    /// <summary>Gets the registered harness for a saga.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <returns>The saga harness.</returns>
    ISagaTestHarness<T> GetSagaHarness<T>()
        where T : class, ISaga;

    /// <summary>Gets the registered harness for a saga state machine.</summary>
    /// <typeparam name="TStateMachine">The state-machine implementation.</typeparam>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <returns>The saga state machine harness.</returns>
    ISagaStateMachineTestHarness<TStateMachine, T> GetSagaStateMachineHarness<TStateMachine, T>()
        where TStateMachine : class, ISagaStateMachine<T>
        where T : class, ISagaStateMachineInstance;

    /// <summary>Creates a request client for the registered request contract.</summary>
    /// <typeparam name="T">The request contract.</typeparam>
    /// <returns>The request client.</returns>
    IRequestClient<T> CreateRequestClient<T>()
        where T : class;

    /// <summary>Gets the send endpoint for a registered consumer.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the consumer endpoint.</returns>
    Task<ISendEndpoint> GetConsumerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IConsumer;

    /// <summary>Gets the send endpoint for a registered message handler.</summary>
    /// <typeparam name="T">The handled message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the handler endpoint.</returns>
    Task<ISendEndpoint> GetHandlerEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Gets the endpoint address for a registered consumer.</summary>
    /// <typeparam name="T">The consumer implementation.</typeparam>
    /// <returns>The consumer address.</returns>
    Uri GetConsumerAddress<T>()
        where T : class, IConsumer;

    /// <summary>Gets the endpoint address for a registered message handler.</summary>
    /// <typeparam name="T">The handled message contract.</typeparam>
    /// <returns>The handler address.</returns>
    Uri GetHandlerAddress<T>()
        where T : class;

    /// <summary>Gets the send endpoint for a registered saga.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the saga endpoint.</returns>
    Task<ISendEndpoint> GetSagaEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class, ISaga;

    /// <summary>Gets the endpoint address for a registered saga.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <returns>The saga address.</returns>
    Uri GetSagaAddress<T>()
        where T : class, ISaga;

    /// <summary>Gets the execute send endpoint for a registered routing-slip activity.</summary>
    /// <typeparam name="T">The execute activity implementation.</typeparam>
    /// <typeparam name="TArguments">The execute arguments.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the activity execute endpoint.</returns>
    Task<ISendEndpoint> GetExecuteActivityEndpointAsync<T, TArguments>(CancellationToken cancellationToken = default)
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Gets the execute endpoint address for a registered routing-slip activity.</summary>
    /// <typeparam name="T">The execute activity implementation.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <returns>The execute activity address.</returns>
    Uri GetExecuteActivityAddress<T, TArguments>()
        where T : class, IExecuteActivity<TArguments>
        where TArguments : class;

    /// <summary>Starts the test host and its configured bus.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A task that completes when the test host is ready.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops the test host and every hosted service started by the harness.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes when all reached services have stopped.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops and starts the test host while preserving dependency-injection registration order.</summary>
    /// <param name="cancellationToken">The token that cancels the restart.</param>
    /// <returns>A task that completes when the restarted test host is ready.</returns>
    Task RestartAsync(CancellationToken cancellationToken = default);
}
