using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides validated endpoint and timing settings to job-service state machines.</summary>
internal interface JobSagaSettings
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

    /// <summary>Gets whether completed jobs are removed from persistence automatically.</summary>
    bool FinalizeCompleted { get; }

    /// <summary>Gets the optional fallback used when the platform cannot resolve a time-zone identifier.</summary>
    Func<string, TimeZoneInfo?>? TimeZoneResolver { get; }
}
