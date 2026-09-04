namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Represents the method that handles retry policy factory.
/// </summary>
/// <param name="filter">The filter value.</param>
/// <returns>The result of the operation.</returns>
public delegate IRetryPolicy RetryPolicyFactory(IExceptionFilter filter);
