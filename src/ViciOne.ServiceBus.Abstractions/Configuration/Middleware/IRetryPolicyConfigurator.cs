namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures exception selection and timing for a retry policy.</summary>
public interface IRetryPolicyConfigurator :
    IExceptionConfigurator
{
    /// <summary>Sets the factory that combines the configured exception filter with retry timing.</summary>
    /// <param name="factory">The retry-policy factory.</param>
    void SetRetryPolicy(RetryPolicyFactory factory);
}
