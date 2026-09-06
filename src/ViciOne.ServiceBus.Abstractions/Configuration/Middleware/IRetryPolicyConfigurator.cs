namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures exception selection and timing for a retry policy.</summary>
public interface IRetryPolicyConfigurator :
    IExceptionConfigurator
{
    /// <summary>Sets the factory that creates the retry policy from the configured exception filter.</summary>
    /// <param name="factory">Creates the retry policy.</param>
    void SetRetryPolicy(RetryPolicyFactory factory);
}
