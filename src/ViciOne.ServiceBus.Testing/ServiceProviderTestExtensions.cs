using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Testing;

public static class ServiceProviderTestExtensions
{
    public static Task<T> GetTaskAsync<T>(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<T>(cancellationToken); var taskCompletionSource = provider.GetRequiredService<TaskCompletionSource<T>>();
        return taskCompletionSource.Task;
    }

    public static Task<T>[] GetTasks<T>(this IServiceProvider provider)
    {
        return provider.GetServices<TaskCompletionSource<T>>().Select(x => x.Task).ToArray();
    }
}
