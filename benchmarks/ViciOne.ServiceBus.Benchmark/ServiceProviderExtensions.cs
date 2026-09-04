using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ViciOneServiceBusBenchmark;

public static class ServiceProviderExtensions
{
    public static async Task StartHostedServicesAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        IHostedService[] services = provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services)
            await service.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task StopHostedServicesAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        IHostedService[] services = provider.GetServices<IHostedService>().ToArray();

        foreach (var service in services.Reverse())
            await service.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
