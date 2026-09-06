using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for testing service provider.</summary>
public static class TestingServiceProviderExtensions
{
    /// <summary>Gets test harness.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <returns>The test harness.</returns>
    public static ITestHarness GetTestHarness(this IServiceProvider provider)
    {
        return provider.GetRequiredService<ITestHarness>();
    }

    /// <summary>Starts test harness.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the start test harness outcome.</returns>
    public static async Task<ITestHarness> StartTestHarnessAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        var testHarness = provider.GetRequiredService<ITestHarness>();

        await testHarness.StartAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        return testHarness;
    }

    /// <summary>Connects publish handler.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces a handle that disconnects the registration.</returns>
    public static async Task<Task<ConsumeContext<T>>> ConnectPublishHandlerAsync<T>(this ITestHarness harness, Func<ConsumeContext<T>, bool> filter, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(harness);
        ArgumentNullException.ThrowIfNull(filter);

        TaskCompletionSource<ConsumeContext<T>> source = harness.GetTask<ConsumeContext<T>>();

        var handle = harness.Bus.ConnectReceiveEndpoint(configurator =>
        {
            configurator.Handler<T>(async context =>
            {
                if (filter(context))
                    source.TrySetResult(context);
            });
        });

        await handle.Ready
            .WaitAsync(harness.TestTimeout, harness.TimeProvider, harness.CancellationToken)
            .ConfigureAwait(false);

        return source.Task;
    }

    /// <summary>Adds task completion source to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public static void AddTaskCompletionSource<T>(this IBusRegistrationConfigurator configurator)
    {
        configurator.Services.AddSingleton(provider => provider.GetRequiredService<ITestHarness>().GetTask<T>());
    }

    /// <summary>Stop the test harness, which stops the bus and all hosted services that were started.</summary>
    /// <param name="harness">The harness.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task StopAsync(this ITestHarness harness, CancellationToken cancellationToken = default)
    {
        IHostedService[] services = harness.Provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services.Reverse())
            await service.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Restarts hosted services.</summary>
    /// <param name="harness">The harness.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task RestartHostedServicesAsync(this ITestHarness harness, CancellationToken cancellationToken = default)
    {
        IHostedService[] services = harness.Provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services.Reverse())
            await service.StopAsync(cancellationToken).ConfigureAwait(false);

        foreach (var service in services)
            await service.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Adds a saga instance to the in-memory saga repository.</summary>
    /// <typeparam name="T">The saga type.</typeparam>
    /// <param name="harness">The test harness.</param>
    /// <param name="correlationId">The correlationId for the newly created saga instance.</param>
    /// <param name="callback">Callback to set any additional properties on the saga instance.</param>
    public static void AddSagaInstance<T>(this ITestHarness harness, Guid? correlationId = default, Action<T>? callback = null)
        where T : class, ISaga, new()
    {
        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<T>>();
        if (dictionary == null)
            throw new ArgumentException("In-memory saga repository not found", nameof(T));

        if (correlationId.HasValue && dictionary[correlationId.Value] != null)
            throw new ArgumentException($"An existing saga instance with the specified correlationId was found: {correlationId}", nameof(correlationId));

        var instance = new T { CorrelationId = correlationId ?? NewId.NextGuid() };
        callback?.Invoke(instance);

        dictionary.Add(new SagaInstance<T>(instance));
    }

    /// <summary>Adds or updates an existing saga instance using the in-memory saga repository.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task AddOrUpdateSagaInstanceAsync<T>(this ITestHarness harness, Guid? correlationId = default, Action<T>? callback = null,
        CancellationToken cancellationToken = default)
        where T : class, ISaga, new()
    {
        correlationId ??= NewId.NextGuid();

        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<T>>();
        if (dictionary == null)
            throw new ArgumentException("In-memory saga repository not found", nameof(T));

        await dictionary.MarkInUseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<T>? existingSaga = dictionary[correlationId.Value];
            if (existingSaga != null)
            {
                await existingSaga.MarkInUseAsync(cancellationToken).ConfigureAwait(false);

                existingSaga.Remove();
                dictionary.Remove(existingSaga);
            }

            var instance = new T { CorrelationId = correlationId.Value };
            callback?.Invoke(instance);

            dictionary.Add(new SagaInstance<T>(instance));
        }
        finally
        {
            dictionary.Release();
        }
    }

    /// <summary>Removes a saga instance from the in-memory saga repository (if it exists).</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="harness">The harness.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the try remove saga instance outcome.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument does not satisfy the operation contract.</exception>
    public static async Task<bool> TryRemoveSagaInstanceAsync<T>(this ITestHarness harness, Guid correlationId, CancellationToken cancellationToken = default)
        where T : class, ISaga
    {
        var dictionary = harness.Provider.GetService<IndexedSagaDictionary<T>>();
        if (dictionary == null)
            throw new ArgumentException("In-memory saga repository not found", nameof(T));

        await dictionary.MarkInUseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SagaInstance<T>? existingSaga = dictionary[correlationId];
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
