using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Settings used by the job service sagas
/// </summary>
public interface JobSagaSettings
{
    /// <summary>
    /// Gets the job attempt saga endpoint address value.
    /// </summary>
    Uri JobAttemptSagaEndpointAddress { get; }
    /// <summary>
    /// Gets the job saga endpoint address value.
    /// </summary>
    Uri JobSagaEndpointAddress { get; }
    /// <summary>
    /// Gets the job type saga endpoint address value.
    /// </summary>
    Uri JobTypeSagaEndpointAddress { get; }

    /// <summary>
    /// Gets the status check interval value.
    /// </summary>
    TimeSpan StatusCheckInterval { get; }

    /// <summary>
    /// Gets the suspect job retry count value.
    /// </summary>
    int SuspectJobRetryCount { get; }
    /// <summary>
    /// Gets the suspect job retry delay value.
    /// </summary>
    TimeSpan? SuspectJobRetryDelay { get; }

    /// <summary>
    /// Gets the slot wait time value.
    /// </summary>
    TimeSpan SlotWaitTime { get; }

    /// <summary>
    /// Gets the heartbeat timeout value.
    /// </summary>
    TimeSpan HeartbeatTimeout { get; }

    /// <summary>
    /// Gets the finalize completed value.
    /// </summary>
    bool FinalizeCompleted { get; }

    /// <summary>
    /// Gets the time zone resolver value.
    /// </summary>
    Func<string, TimeZoneInfo> TimeZoneResolver { get; }
}
