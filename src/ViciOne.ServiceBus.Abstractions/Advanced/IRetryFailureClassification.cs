namespace ViciOne.ServiceBus.Advanced;

/// <summary>Allows an exception to declare its technical retry behavior.</summary>
public interface IRetryFailureClassification
{
    /// <summary>Gets the retry behavior assigned to the exception.</summary>
    RetryFailureKind RetryFailureKind { get; }
}
