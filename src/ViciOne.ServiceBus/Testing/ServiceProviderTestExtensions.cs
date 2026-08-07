// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;


    public static class ServiceProviderTestExtensions
    {
        public static Task<T> GetTask<T>(this IServiceProvider provider)
        {
            var taskCompletionSource = provider.GetRequiredService<TaskCompletionSource<T>>();
            return taskCompletionSource.Task;
        }

        public static Task<T>[] GetTasks<T>(this IServiceProvider provider)
        {
            return provider.GetServices<TaskCompletionSource<T>>().Select(x => x.Task).ToArray();
        }
    }
}
