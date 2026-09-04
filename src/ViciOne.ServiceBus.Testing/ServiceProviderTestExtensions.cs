using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for service provider test.
/// </summary>
public static class ServiceProviderTestExtensions
{
    /// <summary>
    /// Gets task.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T> GetTaskAsync<T>(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken); var taskCompletionSource = provider.GetRequiredService<TaskCompletionSource<T>>();
        return taskCompletionSource.Task;
    }

    /// <summary>
    /// Gets tasks.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="provider">The service provider.</param>
    /// <returns>The result of the operation.</returns>
    public static Task<T>[] GetTasks<T>(this IServiceProvider provider)
    {
        return provider.GetServices<TaskCompletionSource<T>>().Select(x => x.Task).ToArray();
    }
}
