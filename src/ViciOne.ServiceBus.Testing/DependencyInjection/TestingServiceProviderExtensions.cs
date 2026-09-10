using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Controls dependency-injection-owned test harnesses, temporary observations, and in-memory saga state.</summary>
public static class TestingServiceProviderExtensions
{
    /// <summary>Resolves the test harness owned by a service provider.</summary>
    /// <param name="provider">The service provider containing the harness registration.</param>
    /// <returns>The registered test harness.</returns>
    public static ITestHarness GetTestHarness(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.GetRequiredService<ITestHarness>();
    }

    /// <summary>Resolves and starts the test harness owned by a service provider.</summary>
    /// <param name="provider">The service provider containing the harness registration.</param>
    /// <param name="cancellationToken">The token used to cancel startup.</param>
    /// <returns>The started test harness.</returns>
    public static async Task<ITestHarness> StartTestHarnessAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var testHarness = provider.GetRequiredService<ITestHarness>();

        await testHarness.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return testHarness;
    }

    /// <summary>Creates a temporary endpoint that observes the first published message accepted by a filter.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="harness">The running test harness.</param>
    /// <param name="filter">The predicate that selects the message context to return.</param>
    /// <param name="cancellationToken">The token used to cancel endpoint creation and readiness.</param>
    /// <returns>An owner for the endpoint and its first accepted message.</returns>
    public static async Task<IPublishMessageObservation<TMessage>> ObservePublishedMessageAsync<TMessage>(
        this ITestHarness harness,
        Func<ConsumeContext<TMessage>, bool> filter,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(filter);
        cancellationToken.ThrowIfCancellationRequested();

        TaskCompletionSource<ConsumeContext<TMessage>> source = harness.GetTask<ConsumeContext<TMessage>>();

        IHostReceiveEndpointHandle handle = harness.Bus.ConnectReceiveEndpoint(configurator =>
        {
            configurator.Handler<TMessage>(context =>
            {
                if (filter(context))
                    source.TrySetResult(context);

                return Task.CompletedTask;
            });
        });

        try
        {
            using var readinessCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                harness.CancellationToken);

            await handle.Ready
                .WaitAsync(harness.TestTimeout, harness.TimeProvider, readinessCancellation.Token)
                .ConfigureAwait(false);

            return new Internal.PublishMessageObservation<TMessage>(
                handle,
                source.Task,
                harness.TestTimeout,
                harness.TimeProvider);
        }
        catch (Exception exception)
        {
            try
            {
                await handle.StopAsync(CancellationToken.None)
                    .WaitAsync(harness.TestTimeout, harness.TimeProvider, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "The publish observation could not become ready and its temporary endpoint could not be stopped.",
                    exception,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(exception).Throw();
            throw;
        }
    }

    /// <summary>Registers a harness-owned completion source for a result type.</summary>
    /// <typeparam name="TResult">The completion result type.</typeparam>
    /// <param name="configurator">The bus registration configurator.</param>
    public static void AddTaskCompletionSource<TResult>(this IBusRegistrationConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.Services.AddSingleton(provider => provider.GetRequiredService<ITestHarness>().GetTask<TResult>());
    }

    /// <summary>Stops every hosted service in reverse dependency-injection registration order.</summary>
    /// <param name="harness">The harness whose provider owns the hosted services.</param>
    /// <param name="cancellationToken">The token used to cancel shutdown.</param>
    /// <returns>A task that completes after all reached services stop.</returns>
    public static async Task StopAsync(this ITestHarness harness, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        IHostedService[] services = harness.Provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services.Reverse())
            await service.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops hosted services in reverse order and then starts them in registration order.</summary>
    /// <param name="harness">The harness whose provider owns the hosted services.</param>
    /// <param name="cancellationToken">The token used to cancel the restart.</param>
    /// <returns>A task that completes after the restart sequence.</returns>
    public static async Task RestartHostedServicesAsync(this ITestHarness harness, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(harness);
        IHostedService[] services = harness.Provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services.Reverse())
            await service.StopAsync(cancellationToken).ConfigureAwait(false);

        foreach (var service in services)
            await service.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a saga instance to the in-memory saga repository.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="harness">The harness whose provider owns the repository.</param>
    /// <param name="correlationId">The correlation identifier, or <see langword="null"/> to generate one.</param>
    /// <param name="configureSaga">An optional callback that initializes additional saga state.</param>
    public static void AddSagaInstance<TSaga>(this ITestHarness harness, Guid? correlationId = default, Action<TSaga>? configureSaga = null)
        where TSaga : class, ISaga, new()
    {
        ArgumentNullException.ThrowIfNull(harness);

        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<TSaga>>();
        if (dictionary == null)
            throw new InvalidOperationException($"No in-memory saga repository is registered for {TypeCache<TSaga>.ShortName}.");

        if (correlationId.HasValue && dictionary[correlationId.Value] != null)
            throw new ArgumentException(
                $"An in-memory saga with correlation id '{correlationId}' already exists.",
                nameof(correlationId));

        var instance = new TSaga { CorrelationId = correlationId ?? NewId.NextGuid() };
        configureSaga?.Invoke(instance);

        dictionary.Add(new SagaInstance<TSaga>(instance));
    }

    /// <summary>Adds or updates an existing saga instance using the in-memory saga repository.</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="harness">The harness whose provider owns the repository.</param>
    /// <param name="correlationId">The correlation identifier, or <see langword="null"/> to generate one.</param>
    /// <param name="configureSaga">An optional callback that initializes the replacement saga state.</param>
    /// <param name="cancellationToken">The token used to cancel repository access.</param>
    /// <returns>A task that completes after the saga state has been stored.</returns>
    public static async Task AddOrUpdateSagaInstanceAsync<TSaga>(this ITestHarness harness, Guid? correlationId = default,
        Action<TSaga>? configureSaga = null,
        CancellationToken cancellationToken = default)
        where TSaga : class, ISaga, new()
    {
        ArgumentNullException.ThrowIfNull(harness);
        correlationId ??= NewId.NextGuid();

        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<TSaga>>();
        if (dictionary == null)
            throw new InvalidOperationException($"No in-memory saga repository is registered for {TypeCache<TSaga>.ShortName}.");

        await dictionary.MarkInUseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<TSaga>? existingSaga = dictionary[correlationId.Value];
            if (existingSaga != null)
            {
                await existingSaga.MarkInUseAsync(cancellationToken).ConfigureAwait(false);

                existingSaga.Remove();
                dictionary.Remove(existingSaga);
            }

            var instance = new TSaga { CorrelationId = correlationId.Value };
            configureSaga?.Invoke(instance);

            dictionary.Add(new SagaInstance<TSaga>(instance));
        }
        finally
        {
            dictionary.Release();
        }
    }

    /// <summary>Removes a saga instance from the in-memory saga repository (if it exists).</summary>
    /// <typeparam name="TSaga">The saga state type.</typeparam>
    /// <param name="harness">The harness whose provider owns the repository.</param>
    /// <param name="correlationId">The correlation identifier of the saga to remove.</param>
    /// <param name="cancellationToken">The token used to cancel repository access.</param>
    /// <returns><see langword="true"/> when an existing saga was removed; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> TryRemoveSagaInstanceAsync<TSaga>(this ITestHarness harness, Guid correlationId,
        CancellationToken cancellationToken = default)
        where TSaga : class, ISaga
    {
        ArgumentNullException.ThrowIfNull(harness);

        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<TSaga>>();
        if (dictionary == null)
            throw new InvalidOperationException($"No in-memory saga repository is registered for {TypeCache<TSaga>.ShortName}.");

        await dictionary.MarkInUseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<TSaga>? existingSaga = dictionary[correlationId];
            if (existingSaga != null)
            {
                await existingSaga.MarkInUseAsync(cancellationToken).ConfigureAwait(false);

                existingSaga.Remove();
                dictionary.Remove(existingSaga);

                return true;
            }
        }
        finally
        {
            dictionary.Release();
        }

        return false;
    }
}
