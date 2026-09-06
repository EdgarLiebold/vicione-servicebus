using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures execution, retry, progress, concurrency, and distribution metadata for a job type.</summary>
/// <typeparam name="TJob">The job contract type.</typeparam>
public sealed class JobOptions<TJob> :
    IOptions,
    ISpecification
    where TJob : class
{
    readonly JobPropertyCollection _instanceProperties;
    readonly JobPropertyCollection _jobTypeProperties;

    /// <summary>Creates options with bounded execution, cancellation, and progress-buffer defaults.</summary>
    public JobOptions()
    {
        ConcurrentJobLimit = 1;
        JobTimeout = TimeSpan.FromMinutes(5);
        JobCancellationTimeout = TimeSpan.FromSeconds(30);

        RetryPolicy = Retry.None;

        ProgressBuffer = new JobProgressBufferOptions();

        _jobTypeProperties = new JobPropertyCollection();
        _instanceProperties = new JobPropertyCollection();
    }

    /// <summary>Gets or sets the maximum execution time of one job attempt.</summary>
    public TimeSpan JobTimeout { get; set; }

    /// <summary>
    /// Gets or sets the grace period allowed for a consumer to stop after cancellation is requested.
    /// </summary>
    public TimeSpan JobCancellationTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent jobs executed by each consumer instance.
    /// </summary>
    public int ConcurrentJobLimit { get; set; }

    /// <summary>Gets the retry policy applied after a job attempt faults.</summary>
    public IRetryPolicy RetryPolicy { get; private set; }

    /// <summary>Gets or sets the optional diagnostic job-type name advertised to the coordinator.</summary>
    public string? JobTypeName { get; set; }

    /// <summary>Gets the batching limits applied to progress notifications.</summary>
    public JobProgressBufferOptions ProgressBuffer { get; }

    /// <summary>Gets mutable metadata shared by all instances of this job type.</summary>
    public ISetPropertyCollection JobTypeProperties => _jobTypeProperties;

    /// <summary>Gets mutable metadata of this consumer instance, such as region or tenant.</summary>
    public ISetPropertyCollection InstanceProperties => _instanceProperties;

    /// <summary>Gets the serialized job-type metadata used by job-service messages.</summary>
    internal Dictionary<string, object> JobTypePropertyValues => _jobTypeProperties.Values;

    /// <summary>Gets the serialized consumer-instance metadata used by job-service messages.</summary>
    internal Dictionary<string, object> InstancePropertyValues => _instanceProperties.Values;

    /// <summary>Gets or sets the optional concurrency limit shared by all instances of this job type.</summary>
    public int? GlobalConcurrentJobLimit { get; set; }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (ConcurrentJobLimit <= 0)
            yield return this.Failure("JobOptions", "ConcurrentJobLimit", "Must be > 0");
        if (JobTimeout <= TimeSpan.Zero)
            yield return this.Failure("JobOptions", "JobTimeout", "Must be > TimeSpan.Zero");
        if (JobCancellationTimeout <= TimeSpan.Zero)
            yield return this.Failure("JobOptions", "JobCancellationTimeout", "Must be > TimeSpan.Zero");
        if (GlobalConcurrentJobLimit is <= 0)
            yield return this.Failure("JobOptions", "GlobalConcurrentJobLimit", "Must be > 0 when specified");
        if (JobTypeName is not null && string.IsNullOrWhiteSpace(JobTypeName))
            yield return this.Failure("JobOptions", "JobTypeName", "Must not be empty when specified");
        if (ProgressBuffer.UpdateLimit <= 0)
            yield return this.Failure("JobOptions", "ProgressBuffer.UpdateLimit", "Must be > 0");
        if (ProgressBuffer.TimeLimit <= TimeSpan.Zero)
            yield return this.Failure("JobOptions", "ProgressBuffer.TimeLimit", "Must be > TimeSpan.Zero");
    }

    /// <summary>Configures the retry policy applied after a job attempt faults.</summary>
    /// <param name="configure">The callback that defines the retry intervals and exception filters.</param>
    /// <returns>This job-options instance.</returns>
    public JobOptions<TJob> ConfigureRetry(Action<IRetryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var specification = new RetrySpecification();
        configure(specification);

        specification.Validate().ThrowIfContainsFailure($"The retry policy was not properly configured: JobOptions<{TypeCache<TJob>.ShortName}");

        RetryPolicy = specification.Build();

        return this;
    }

    sealed class RetrySpecification :
        ExceptionSpecification,
        IRetryConfigurator,
        ISpecification
    {
        readonly RetryObservable _observers;
        RetryPolicyFactory? _policyFactory;

        public RetrySpecification()
        {
            _observers = new RetryObservable();
        }

        public void SetRetryPolicy(RetryPolicyFactory factory)
        {
            _policyFactory = factory;
        }

        ConnectHandle IRetryObserverConnector.ConnectRetryObserver(IRetryObserver observer)
        {
            return _observers.Connect(observer);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_policyFactory == null)
                yield return this.Failure("RetryPolicy", "must not be null");
        }

        public IRetryPolicy Build()
        {
            if (_policyFactory == null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Retry", "unknown", $"The retry policy was not properly configured: JobOptions<{TypeCache<TJob>.ShortName}", "Correct the named configuration before starting the host"));

            return _policyFactory(Filter);
        }
    }
}
