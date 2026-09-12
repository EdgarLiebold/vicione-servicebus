namespace ViciOne.ServiceBus.Advanced;

/// <summary>Specifies the condition that completed a batch for delivery.</summary>
public enum BatchCompletionMode
{
    /// <summary>The time limit for receiving messages in the batch was reached.</summary>
    Time = 0,

    /// <summary>The maximum number of messages in the batch was reached.</summary>
    Size = 1,

    /// <summary>Delivery was forced before the configured time or size limit was reached.</summary>
    Forced = 2
}
