using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Resolves test-owned completion tasks from a dependency-injection service provider.</summary>
public static class ServiceProviderTestExtensions
{
    /// <summary>Waits for the last registered harness completion without canceling its shared source.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="provider">The service provider that owns the task registration.</param>
    /// <param name="cancellationToken">The token used to cancel this caller's wait without canceling the shared task.</param>
    /// <returns>The last registered task for <typeparamref name="T"/>.</returns>
    public static Task<T> WaitForCompletionAsync<T>(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);

        Task<T> task = provider.GetRequiredService<TaskCompletionSource<T>>().Task;
        return cancellationToken.CanBeCanceled
            ? task.WaitAsync(cancellationToken)
            : task;
    }

    /// <summary>Gets every registered harness completion task in dependency-injection registration order.</summary>
    /// <typeparam name="T">The task result type.</typeparam>
    /// <param name="provider">The service provider that owns the task registrations.</param>
    /// <returns>The registered tasks in resolution order.</returns>
    public static Task<T>[] GetCompletionTasks<T>(this IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider.GetServices<TaskCompletionSource<T>>().Select(x => x.Task).ToArray();
    }
}
