using System;
using System.Collections.Generic;
using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.Quartz.Runtime;

internal sealed class QuartzSchedulerClaimRegistry
{
    readonly Dictionary<object, string> _owners = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<string, string> _schedulerNames = new(StringComparer.OrdinalIgnoreCase);
    readonly object _sync = new();

    public void ClaimFactory(ISchedulerFactory schedulerFactory, string busKey)
    {
        ArgumentNullException.ThrowIfNull(schedulerFactory);
        ClaimReference(schedulerFactory, busKey, "scheduler factory");
    }

    public void ClaimScheduler(IScheduler scheduler, string busKey)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ClaimReference(scheduler, busKey, "scheduler");

        lock (_sync)
        {
            if (_schedulerNames.TryGetValue(scheduler.SchedulerName, out string? owner)
                && !string.Equals(owner, busKey, StringComparison.Ordinal))
            {
                throw DuplicateOwner("scheduler name", scheduler.SchedulerName, owner, busKey);
            }

            _schedulerNames[scheduler.SchedulerName] = busKey;
        }
    }

    private void ClaimReference(object value, string busKey, string component)
    {
        lock (_sync)
        {
            if (_owners.TryGetValue(value, out string? owner)
                && !string.Equals(owner, busKey, StringComparison.Ordinal))
            {
                throw DuplicateOwner(component, value.GetType().FullName ?? value.GetType().Name, owner, busKey);
            }

            _owners[value] = busKey;
        }
    }

    private static ConfigurationException DuplicateOwner(string component, string identity, string owner, string contender)
    {
        return new ConfigurationException(ConfigurationMessages.Create(
            "Quartz scheduling",
            contender,
            $"The {component} '{identity}' is already owned by bus '{owner}'",
            $"Provide a distinct {component} for this bus"));
    }
}
