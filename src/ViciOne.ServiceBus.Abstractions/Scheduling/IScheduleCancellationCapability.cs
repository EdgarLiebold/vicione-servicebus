namespace ViciOne.ServiceBus.Advanced;

/// <summary>Describes how a scheduler identifies an accepted send for later cancellation.</summary>
public enum ScheduleCancellationMode
{
    /// <summary>The provider has not declared how cancellation tokens are assigned.</summary>
    Unknown,

    /// <summary>The scheduler cannot cancel an accepted send by token.</summary>
    Unsupported,

    /// <summary>The scheduler accepts a caller-selected token and cancels with that same token.</summary>
    CallerSpecifiedToken,

    /// <summary>The scheduler assigns a token only after accepting the send.</summary>
    ProviderAssignedToken
}

/// <summary>Exposes the scheduling-token behavior needed by request lifecycles.</summary>
public interface IScheduleCancellationCapability
{
    /// <summary>Gets the token mode supported by the scheduler or provider.</summary>
    ScheduleCancellationMode CancellationMode { get; }
}
