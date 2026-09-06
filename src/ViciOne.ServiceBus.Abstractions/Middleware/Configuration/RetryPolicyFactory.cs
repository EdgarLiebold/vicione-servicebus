namespace ViciOne.ServiceBus.Configuration;

/// <summary>Represents the method that handles retry policy factory.</summary>
/// <param name="filter">The filter to add to the pipeline.</param>
/// <returns>The value produced by the operation.</returns>
public delegate IRetryPolicy RetryPolicyFactory(IExceptionFilter filter);
