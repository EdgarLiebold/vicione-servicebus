using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Settings used by the job service sagas.</summary>
public interface JobSagaSettings
{
    /// <summary>Gets the job attempt saga endpoint address.</summary>
    Uri JobAttemptSagaEndpointAddress { get; }
    /// <summary>Gets the job saga endpoint address.</summary>
    Uri JobSagaEndpointAddress { get; }
    /// <summary>Gets the job type saga endpoint address.</summary>
    Uri JobTypeSagaEndpointAddress { get; }

    /// <summary>Gets the status check interval.</summary>
    TimeSpan StatusCheckInterval { get; }

    /// <summary>Gets the suspect job retry count.</summary>
    int SuspectJobRetryCount { get; }
    /// <summary>Gets the suspect job retry delay.</summary>
    TimeSpan? SuspectJobRetryDelay { get; }

    /// <summary>Gets the slot wait time.</summary>
    TimeSpan SlotWaitTime { get; }

    /// <summary>Gets the heartbeat timeout.</summary>
    TimeSpan HeartbeatTimeout { get; }

    /// <summary>Gets the finalize completed.</summary>
    bool FinalizeCompleted { get; }

    /// <summary>Gets the time zone resolver.</summary>
    Func<string, TimeZoneInfo> TimeZoneResolver { get; }
}
