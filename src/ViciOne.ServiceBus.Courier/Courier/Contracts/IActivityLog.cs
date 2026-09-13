using System;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Records a successfully executed activity in the routing slip.</summary>
public interface IActivityLog
{
    /// <summary>The identifier of the activity execution.</summary>
    Guid ExecutionId { get; }

    /// <summary>The name of the activity that was completed.</summary>
    string Name { get; }

    /// <summary>The timestamp when the activity started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration of the activity execution.</summary>
    TimeSpan Duration { get; }

    /// <summary>The host that executed the activity.</summary>
    HostInfo Host { get; }
}
