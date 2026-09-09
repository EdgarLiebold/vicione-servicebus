namespace ViciOne.ServiceBus.Contracts;

/// <summary>Requests a new operation limit for each configured rate-limiting interval.</summary>
public interface SetRateLimit
{
    /// <summary>Gets the positive number of operations admitted per interval.</summary>
    int RateLimit { get; }
}
