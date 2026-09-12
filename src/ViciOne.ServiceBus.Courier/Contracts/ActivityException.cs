using System;

namespace ViciOne.ServiceBus.Courier.Contracts;

/// <summary>Captures an exception raised while executing a routing-slip activity.</summary>
public interface ActivityException
{
    /// <summary>The identifier of the activity execution that faulted.</summary>
    Guid ExecutionId { get; }

    /// <summary>The time when the activity execution started.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The duration before the activity execution faulted.</summary>
    TimeSpan Elapsed { get; }

    /// <summary>The name of the activity that caused the exception.</summary>
    string Name { get; }

    /// <summary>The host where the exception was thrown.</summary>
    HostInfo Host { get; }

    /// <summary>The exception details.</summary>
    ExceptionInfo ExceptionInfo { get; }
}
