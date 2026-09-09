namespace ViciOne.ServiceBus.Configuration;

/// <summary>Specifies which message arrival starts or restarts a batch collection timeout.</summary>
public enum BatchTimeLimitStart
{
    /// <summary>Starts the timeout when the first message enters an empty batch.</summary>
    FromFirst,
    /// <summary>Restarts the timeout whenever a message is added to the batch.</summary>
    FromLast
}
