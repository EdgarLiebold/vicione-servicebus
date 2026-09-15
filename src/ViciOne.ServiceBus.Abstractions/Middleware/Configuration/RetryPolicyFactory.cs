namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates a retry policy from the exception filter configured for a pipeline.</summary>
/// <param name="filter">The immutable exception filter for the policy.</param>
/// <returns>The configured retry policy.</returns>
public delegate IRetryPolicy RetryPolicyFactory(IExceptionFilter filter);
