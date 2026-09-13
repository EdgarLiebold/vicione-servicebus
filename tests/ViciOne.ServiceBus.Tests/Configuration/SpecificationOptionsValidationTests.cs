using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class SpecificationOptionsValidationTests
{
    [Theory]
    [InlineData(InvalidBatchOption.MessageLimit, "MessageLimit")]
    [InlineData(InvalidBatchOption.ConcurrencyLimit, "ConcurrencyLimit")]
    [InlineData(InvalidBatchOption.TimeLimit, "TimeLimit")]
    [InlineData(InvalidBatchOption.TimeLimitStart, "TimeLimitStart")]
    [InlineData(InvalidBatchOption.CapacityOverflow, "ConcurrencyLimit")]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "each-invariant-rejected-before-pipe-build")]
    public void BatchOptions_RejectEveryInvalidInvariant(InvalidBatchOption invalid, string property)
    {
        var options = new BatchOptions();
        switch (invalid)
        {
            case InvalidBatchOption.MessageLimit: options.MessageLimit = 0; break;
            case InvalidBatchOption.ConcurrencyLimit: options.ConcurrencyLimit = 0; break;
            case InvalidBatchOption.TimeLimit: options.TimeLimit = TimeSpan.Zero; break;
            case InvalidBatchOption.TimeLimitStart: options.TimeLimitStart = (BatchTimeLimitStart)42; break;
            case InvalidBatchOption.CapacityOverflow:
                options.ConcurrencyLimit = int.MaxValue;
                options.MessageLimit = 2;
                break;
        }

        ValidationResult[] failures = options.Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "defaults-are-coherent")]
    public void BatchOptions_DefaultsAreCoherent()
    {
        var options = new BatchOptions();

        Assert.Empty(options.Validate());
        Assert.Equal(10, options.MessageLimit);
        Assert.Equal(1, options.ConcurrencyLimit);
        Assert.Equal(TimeSpan.FromSeconds(1), options.TimeLimit);
        Assert.Equal(BatchTimeLimitStart.FromFirst, options.TimeLimitStart);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "capacity-callback-raises-but-never-lowers-limits")]
    public void BatchOptions_DefaultCapacityCallbackRaisesButNeverLowersEndpointLimits()
    {
        var options = new BatchOptions
        {
            MessageLimit = 5,
            ConcurrencyLimit = 3,
        };
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointConfiguratorProxy>();
        var proxy = (EndpointConfiguratorProxy)(object)endpoint;

        options.Configure("orders", endpoint);

        Assert.Equal(15, proxy.PrefetchCount);
        Assert.Equal(15, proxy.ConcurrentMessageLimit);

        proxy.PrefetchCount = 20;
        proxy.ConcurrentMessageLimit = 20;
        options.Configure("orders", endpoint);

        Assert.Equal(20, proxy.PrefetchCount);
        Assert.Equal(20, proxy.ConcurrentMessageLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "fluent-values-and-custom-capacity-callback")]
    public void BatchOptions_FluentConfigurationPreservesExactValuesAndCallbackInputs()
    {
        var options = new BatchOptions();
        string? observedName = null;
        IReceiveEndpointConfigurator? observedEndpoint = null;
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointConfiguratorProxy>();

        Assert.Same(options, options.SetMessageLimit(7));
        Assert.Same(options, options.SetConcurrencyLimit(3));
        Assert.Same(options, options.SetTimeLimit(TimeSpan.FromMinutes(2)));
        Assert.Same(options, options.SetTimeLimitStart(BatchTimeLimitStart.FromLast));
        Assert.Same(options, options.SetConfigurationCallback((name, configuredEndpoint) =>
        {
            observedName = name;
            observedEndpoint = configuredEndpoint;
        }));

        options.Configure("priority", endpoint);

        Assert.Equal(7, options.MessageLimit);
        Assert.Equal(3, options.ConcurrencyLimit);
        Assert.Equal(TimeSpan.FromMinutes(2), options.TimeLimit);
        Assert.Equal(BatchTimeLimitStart.FromLast, options.TimeLimitStart);
        Assert.Equal("priority", observedName);
        Assert.Same(endpoint, observedEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-OPTIONS", "callbacks-selectors-and-providers-reject-null")]
    public void BatchConfiguration_RejectsEveryMissingCallbackSelectorAndProviderInput()
    {
        var options = new BatchOptions();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() => options.Configure(null, null!)).ParamName);
        Assert.Equal("callback", Assert.Throws<ArgumentNullException>(() => options.SetConfigurationCallback(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            options.GroupBy<TestBatchMessage, int>((Func<ConsumeContext<TestBatchMessage>, int?>)null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            options.GroupBy<TestBatchMessage, string>((Func<ConsumeContext<TestBatchMessage>, string?>)null!)).ParamName);

    }

    [Theory]
    [InlineData(InvalidJobOption.Concurrency, "ConcurrentJobLimit")]
    [InlineData(InvalidJobOption.Timeout, "JobTimeout")]
    [InlineData(InvalidJobOption.CancellationTimeout, "JobCancellationTimeout")]
    [InlineData(InvalidJobOption.GlobalConcurrency, "GlobalConcurrentJobLimit")]
    [InlineData(InvalidJobOption.Name, "JobTypeName")]
    [InlineData(InvalidJobOption.ProgressCount, "ProgressBuffer.UpdateLimit")]
    [InlineData(InvalidJobOption.ProgressTime, "ProgressBuffer.TimeLimit")]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobOptions_RejectEveryInvalidInvariant(InvalidJobOption invalid, string property)
    {
        var options = new JobOptions<TestJob>();
        switch (invalid)
        {
            case InvalidJobOption.Concurrency: options.ConcurrentJobLimit = 0; break;
            case InvalidJobOption.Timeout: options.JobTimeout = TimeSpan.Zero; break;
            case InvalidJobOption.CancellationTimeout: options.JobCancellationTimeout = TimeSpan.Zero; break;
            case InvalidJobOption.GlobalConcurrency: options.GlobalConcurrentJobLimit = 0; break;
            case InvalidJobOption.Name: options.JobTypeName = " "; break;
            case InvalidJobOption.ProgressCount: options.ProgressBuffer.UpdateLimit = 0; break;
            case InvalidJobOption.ProgressTime: options.ProgressBuffer.TimeLimit = TimeSpan.Zero; break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "defaults-are-coherent")]
    public void JobOptions_DefaultsAreCoherent() =>
        Assert.Empty(((ISpecification)new JobOptions<TestJob>()).Validate());

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "retry-configuration-requires-callback-and-policy")]
    public void JobOptions_RetryConfigurationRejectsMissingInputs()
    {
        var options = new JobOptions<TestJob>();

        Assert.Throws<ArgumentNullException>(() => options.ConfigureRetry(null!));
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => options.ConfigureRetry(_ => { }));
        Assert.Contains($"JobOptions<{TypeCache<TestJob>.ShortName}>", exception.Message, StringComparison.Ordinal);
        Assert.Same(Retry.None, options.RetryPolicy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "retry-configuration-builds-policy-and-rejects-invalid-factories")]
    public void JobOptions_RetryConfigurationBuildsPolicyAndRejectsInvalidFactories()
    {
        var options = new JobOptions<TestJob>();

        Assert.Same(options, options.ConfigureRetry(retry => retry.Immediate(2)));
        Assert.NotSame(Retry.None, options.RetryPolicy);
        Assert.Contains("Immediate", options.RetryPolicy.GetType().Name, StringComparison.Ordinal);

        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            options.ConfigureRetry(retry => retry.SetRetryPolicy(null!))).ParamName);
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            options.ConfigureRetry(retry => retry.SetRetryPolicy(_ => null!)));
        Assert.Contains("factory returned null", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"JobOptions<{TypeCache<TestJob>.ShortName}>", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(InvalidJobConsumerOption.HeartbeatInterval, "HeartbeatInterval")]
    [InlineData(InvalidJobConsumerOption.RejectedJobDelay, "RejectedJobDelay")]
    [InlineData(InvalidJobConsumerOption.TimeProvider, "TimeProvider")]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "runtime-consumer-options-reject-each-invalid-invariant-in-isolation")]
    public void JobConsumerOptions_RejectEachInvalidInvariantInIsolation(
        InvalidJobConsumerOption invalid,
        string property)
    {
        var options = new JobConsumerOptions();
        switch (invalid)
        {
            case InvalidJobConsumerOption.HeartbeatInterval: options.HeartbeatInterval = TimeSpan.Zero; break;
            case InvalidJobConsumerOption.RejectedJobDelay: options.RejectedJobDelay = TimeSpan.Zero; break;
            case InvalidJobConsumerOption.TimeProvider: options.TimeProvider = null!; break;
        }

        ValidationResult failure = Assert.Single(((ISpecification)options).Validate());

        Assert.True(Identifies(failure, property));
    }

    [Theory]
    [InlineData(InvalidJobServiceOption.SlotWait, "SlotWaitTime")]
    [InlineData(InvalidJobServiceOption.StatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatInterval, "HeartbeatInterval")]
    [InlineData(InvalidJobServiceOption.HeartbeatTimeout, "HeartbeatTimeout")]
    [InlineData(InvalidJobServiceOption.RejectedJobDelay, "RejectedJobDelay")]
    [InlineData(InvalidJobServiceOption.TimeProvider, "TimeProvider")]
    [InlineData(InvalidJobServiceOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidJobServiceOption.RetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidJobServiceOption.RetryDelay, "SuspectJobRetryDelay")]
    [InlineData(InvalidJobServiceOption.TypeEndpoint, "JobTypeEndpointName")]
    [InlineData(InvalidJobServiceOption.StateEndpoint, "JobEndpointName")]
    [InlineData(InvalidJobServiceOption.AttemptEndpoint, "JobAttemptEndpointName")]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobServiceOptions_RejectEveryInvalidInvariant(InvalidJobServiceOption invalid, string property)
    {
        JobServiceOptions options = invalid is InvalidJobServiceOption.TypeEndpoint
            or InvalidJobServiceOption.StateEndpoint
            or InvalidJobServiceOption.AttemptEndpoint
                ? new JobServiceOptions()
                : ValidJobServiceOptions();
        switch (invalid)
        {
            case InvalidJobServiceOption.SlotWait: options.SlotWaitTime = TimeSpan.Zero; break;
            case InvalidJobServiceOption.StatusCheck: options.StatusCheckInterval = TimeSpan.FromSeconds(29); break;
            case InvalidJobServiceOption.HeartbeatInterval: options.HeartbeatInterval = TimeSpan.Zero; break;
            case InvalidJobServiceOption.HeartbeatTimeout: options.HeartbeatTimeout = TimeSpan.Zero; break;
            case InvalidJobServiceOption.RejectedJobDelay: options.RejectedJobDelay = TimeSpan.Zero; break;
            case InvalidJobServiceOption.TimeProvider: options.TimeProvider = null!; break;
            case InvalidJobServiceOption.Concurrency: options.ConcurrentMessageLimit = 0; break;
            case InvalidJobServiceOption.RetryCount: options.SuspectJobRetryCount = -1; break;
            case InvalidJobServiceOption.RetryDelay: options.SuspectJobRetryDelay = TimeSpan.Zero; break;
            case InvalidJobServiceOption.TypeEndpoint:
            case InvalidJobServiceOption.StateEndpoint:
            case InvalidJobServiceOption.AttemptEndpoint:
                break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SERVICE-OPTIONS", "coherent-materialized-values-pass")]
    public void JobServiceOptions_CoherentMaterializedValuesPass()
    {
        JobServiceOptions options = ValidJobServiceOptions();

        Assert.Empty(((ISpecification)options).Validate());
        Assert.True(options.FinalizeCompleted);
        Assert.Equal(16, options.ConcurrentMessageLimit);
        Assert.Equal(TimeSpan.FromMinutes(1), options.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromSeconds(3), options.RejectedJobDelay);
        Assert.Same(TimeProvider.System, options.TimeProvider);
    }

    [Theory]
    [InlineData(InvalidJobSagaOption.SlotWait, "SlotWaitTime")]
    [InlineData(InvalidJobSagaOption.StatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidJobSagaOption.HeartbeatTimeout, "HeartbeatTimeout")]
    [InlineData(InvalidJobSagaOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidJobSagaOption.RetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidJobSagaOption.RetryDelay, "SuspectJobRetryDelay")]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-OPTIONS", "each-invariant-rejected-before-endpoint-build")]
    public void JobSagaOptions_RejectEveryInvalidInvariant(InvalidJobSagaOption invalid, string property)
    {
        var options = new JobSagaOptions();
        switch (invalid)
        {
            case InvalidJobSagaOption.SlotWait: options.SlotWaitTime = TimeSpan.Zero; break;
            case InvalidJobSagaOption.StatusCheck: options.StatusCheckInterval = TimeSpan.FromSeconds(29); break;
            case InvalidJobSagaOption.HeartbeatTimeout: options.HeartbeatTimeout = TimeSpan.Zero; break;
            case InvalidJobSagaOption.Concurrency: options.ConcurrentMessageLimit = 0; break;
            case InvalidJobSagaOption.RetryCount: options.SuspectJobRetryCount = -1; break;
            case InvalidJobSagaOption.RetryDelay: options.SuspectJobRetryDelay = TimeSpan.Zero; break;
        }

        ValidationResult[] failures = ((ISpecification)options).Validate().ToArray();

        Assert.Contains(failures, failure => Identifies(failure, property));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-SAGA-OPTIONS", "defaults-are-coherent")]
    public void JobSagaOptions_DefaultsAreCoherent()
    {
        var options = new JobSagaOptions();

        Assert.Empty(((ISpecification)options).Validate());
        Assert.True(options.FinalizeCompleted);
        Assert.Equal(16, options.ConcurrentMessageLimit);
        Assert.Equal(TimeSpan.FromMinutes(5), options.HeartbeatTimeout);
    }

    static JobServiceOptions ValidJobServiceOptions() => new()
    {
        JobTypeEndpointName = "job-type",
        JobEndpointName = "job-state",
        JobAttemptEndpointName = "job-attempt",
    };

    static bool Identifies(ValidationResult failure, string property) =>
        failure.Key.Contains(property, StringComparison.Ordinal)
        || (failure.Value?.Contains(property, StringComparison.Ordinal) ?? false)
        || failure.Message.Contains(property, StringComparison.Ordinal);

    public sealed record TestJob;

    public enum InvalidBatchOption { MessageLimit, ConcurrencyLimit, TimeLimit, TimeLimitStart, CapacityOverflow }

    public sealed record TestBatchMessage;

    public enum InvalidJobOption { Concurrency, Timeout, CancellationTimeout, GlobalConcurrency, Name, ProgressCount, ProgressTime }

    public enum InvalidJobConsumerOption { HeartbeatInterval, RejectedJobDelay, TimeProvider }

    public enum InvalidJobServiceOption
    {
        SlotWait,
        StatusCheck,
        HeartbeatInterval,
        HeartbeatTimeout,
        RejectedJobDelay,
        TimeProvider,
        Concurrency,
        RetryCount,
        RetryDelay,
        TypeEndpoint,
        StateEndpoint,
        AttemptEndpoint,
    }

    public enum InvalidJobSagaOption { SlotWait, StatusCheck, HeartbeatTimeout, Concurrency, RetryCount, RetryDelay }

    private class EndpointConfiguratorProxy : DispatchProxy
    {
        public int PrefetchCount { get; set; }

        public int? ConcurrentMessageLimit { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            switch (targetMethod.Name)
            {
                case "get_PrefetchCount": return PrefetchCount;
                case "set_PrefetchCount": PrefetchCount = Assert.IsType<int>(args[0]); return null;
                case "get_ConcurrentMessageLimit": return ConcurrentMessageLimit;
                case "set_ConcurrentMessageLimit": ConcurrentMessageLimit = (int?)args[0]; return null;
                default: throw new NotSupportedException(targetMethod.Name);
            }
        }
    }
}
