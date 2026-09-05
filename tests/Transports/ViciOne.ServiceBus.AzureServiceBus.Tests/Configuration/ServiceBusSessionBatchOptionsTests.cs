using System.Reflection;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class ServiceBusSessionBatchOptionsTests
{
    [Theory]
    [InlineData(InvalidOption.MessageLimit, "MessageLimitPerSession")]
    [InlineData(InvalidOption.SessionConcurrency, "MaxConcurrentSessions")]
    [InlineData(InvalidOption.IdleTimeout, "SessionIdleTimeout")]
    [InlineData(InvalidOption.TimeLimit, "TimeLimit")]
    [InlineData(InvalidOption.TimeLimitStart, "TimeLimitStart")]
    public void SessionBatching_RejectsEveryInvalidInvariantBeforeEndpointMaterialization(
        InvalidOption invalid,
        string property)
    {
        ServiceBusSessionBatchOptions options = Options(invalid);
        MethodInfo validate = typeof(ServiceBusSessionBatchOptions)
            .GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        TargetInvocationException wrapper = Assert.Throws<TargetInvocationException>(() => validate.Invoke(options, null));
        ConfigurationException exception = Assert.IsType<ConfigurationException>(wrapper.InnerException);

        Assert.Contains("Azure Service Bus session batching for bus 'default':", exception.Message, StringComparison.Ordinal);
        Assert.Contains(property, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionBatching_AcceptsCoherentDefaults()
    {
        ServiceBusSessionBatchOptions options = Options(null);
        MethodInfo validate = typeof(ServiceBusSessionBatchOptions)
            .GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic)!;

        validate.Invoke(options, null);
    }

    static ServiceBusSessionBatchOptions Options(InvalidOption? invalid)
    {
        var options = new ServiceBusSessionBatchOptions();
        switch (invalid)
        {
            case InvalidOption.MessageLimit: options.MessageLimitPerSession = 0; break;
            case InvalidOption.SessionConcurrency: options.MaxConcurrentSessions = 0; break;
            case InvalidOption.IdleTimeout: options.SessionIdleTimeout = TimeSpan.Zero; break;
            case InvalidOption.TimeLimit: options.TimeLimit = TimeSpan.Zero; break;
            case InvalidOption.TimeLimitStart: options.TimeLimitStart = (BatchTimeLimitStart)42; break;
        }

        return options;
    }

    public enum InvalidOption
    {
        MessageLimit,
        SessionConcurrency,
        IdleTimeout,
        TimeLimit,
        TimeLimitStart,
    }

}
