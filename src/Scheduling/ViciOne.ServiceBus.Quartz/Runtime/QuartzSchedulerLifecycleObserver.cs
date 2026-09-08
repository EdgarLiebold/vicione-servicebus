using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Observers;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Coordinates one bus-bound Quartz scheduler with its bus lifecycle.</summary>
internal sealed class QuartzSchedulerLifecycleObserver<TBus> : IBusObserver
    where TBus : class, IBus
{
    readonly QuartzSchedulerBinding<TBus> _binding;

    /// <summary>Initializes the observer with the scheduler binding owned by the same bus registration.</summary>
    /// <param name="binding">The bus-specific scheduler binding.</param>
    public QuartzSchedulerLifecycleObserver(QuartzSchedulerBinding<TBus> binding)
    {
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        await busReady.ConfigureAwait(false);
        await _binding.StartAsync(bus).ConfigureAwait(false);
    }

    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return _binding.StandbyAndDetachAsync();
    }

    public Task PreStopAsync(IBus bus)
    {
        return _binding.StandbyAndDetachAsync();
    }

    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return _binding.StandbyAndDetachAsync();
    }
}
