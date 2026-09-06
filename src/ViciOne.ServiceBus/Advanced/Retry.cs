using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.RetryPolicies.ExceptionFilters;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates retry policies and exception filters for message pipelines.</summary>
public static class Retry
{
    static readonly IExceptionFilter _all = new AllExceptionFilter();

    /// <summary>Gets a policy that never retries an operation.</summary>
    public static IRetryPolicy None { get; } = new NoRetryPolicy(new AllExceptionFilter());

    /// <summary>Creates a policy with the specified number of immediate retries.</summary>
    /// <param name="retryLimit">The number of retries to attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Immediate(int retryLimit)
    {
        return new ImmediateRetryPolicy(All(), retryLimit);
    }

    /// <summary>Creates a filtered policy with the specified number of immediate retries.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryLimit">The number of retries to attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Immediate(this IExceptionFilter filter, int retryLimit)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return new ImmediateRetryPolicy(filter, retryLimit);
    }

    /// <summary>Creates a policy whose retry count and delays are defined by an explicit schedule.</summary>
    /// <param name="intervals">The delay before each retry attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Intervals(params TimeSpan[] intervals)
    {
        return new IntervalRetryPolicy(All(), intervals);
    }

    /// <summary>Creates a filtered policy whose retry count and delays are defined by an explicit schedule.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="intervals">The delay before each retry attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Intervals(this IExceptionFilter filter, params TimeSpan[] intervals)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return new IntervalRetryPolicy(filter, intervals);
    }

    /// <summary>Creates an interval retry policy with the specified number of retries at a fixed interval.</summary>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="interval">The interval between each retry attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Interval(int retryCount, TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryCount);

        return new IntervalRetryPolicy(All(), Enumerable.Repeat(interval, retryCount).ToArray());
    }

    /// <summary>Creates an interval retry policy with the specified number of retries at a fixed interval.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryCount">The number of retry attempts.</param>
    /// <param name="interval">The interval between each retry attempt.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Interval(this IExceptionFilter filter, int retryCount, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryCount);

        return new IntervalRetryPolicy(filter, Enumerable.Repeat(interval, retryCount).ToArray());
    }

    /// <summary>Creates a policy with bounded exponentially increasing jittered delays.</summary>
    /// <param name="retryLimit">The maximum number of retry attempts.</param>
    /// <param name="minInterval">The minimum retry delay.</param>
    /// <param name="maxInterval">The maximum retry delay.</param>
    /// <param name="intervalDelta">The base exponential delay increment.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Exponential(int retryLimit, TimeSpan minInterval, TimeSpan maxInterval,
        TimeSpan intervalDelta)
    {
        return new ExponentialRetryPolicy(All(), retryLimit, minInterval, maxInterval, intervalDelta);
    }

    /// <summary>Creates a policy that continues retrying with bounded exponentially increasing jittered delays.</summary>
    /// <param name="minInterval">The minimum retry delay.</param>
    /// <param name="maxInterval">The maximum retry delay.</param>
    /// <param name="intervalDelta">The base exponential delay increment.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Exponential(TimeSpan minInterval, TimeSpan maxInterval, TimeSpan intervalDelta)
    {
        return new ExponentialRetryPolicy(All(), int.MaxValue, minInterval, maxInterval, intervalDelta);
    }

    /// <summary>Creates a filtered policy with bounded exponentially increasing jittered delays.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryLimit">The maximum number of retry attempts.</param>
    /// <param name="minInterval">The minimum retry delay.</param>
    /// <param name="maxInterval">The maximum retry delay.</param>
    /// <param name="intervalDelta">The base exponential delay increment.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Exponential(this IExceptionFilter filter, int retryLimit,
        TimeSpan minInterval, TimeSpan maxInterval,
        TimeSpan intervalDelta)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return new ExponentialRetryPolicy(filter, retryLimit, minInterval, maxInterval, intervalDelta);
    }

    /// <summary>Creates a policy with a linearly increasing delay between retry attempts.</summary>
    /// <param name="retryLimit">The number of retry attempts.</param>
    /// <param name="initialInterval">The initial retry interval.</param>
    /// <param name="intervalIncrement">The interval to add to the retry interval with each subsequent retry.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Incremental(int retryLimit, TimeSpan initialInterval,
        TimeSpan intervalIncrement)
    {
        return new IncrementalRetryPolicy(All(), retryLimit, initialInterval, intervalIncrement);
    }

    /// <summary>Creates a filtered policy with a linearly increasing delay between retry attempts.</summary>
    /// <param name="filter">Determines which exceptions are retried.</param>
    /// <param name="retryLimit">The number of retry attempts.</param>
    /// <param name="initialInterval">The initial retry interval.</param>
    /// <param name="intervalIncrement">The interval to add to the retry interval with each subsequent retry.</param>
    /// <returns>The retry policy.</returns>
    public static IRetryPolicy Incremental(this IExceptionFilter filter, int retryLimit,
        TimeSpan initialInterval,
        TimeSpan intervalIncrement)
    {
        ArgumentNullException.ThrowIfNull(filter);

        return new IncrementalRetryPolicy(filter, retryLimit, initialInterval, intervalIncrement);
    }

    /// <summary>Creates a retry policy through the retry configurator.</summary>
    /// <param name="configure">Configures exception selection and retry timing.</param>
    /// <returns>The validated retry policy.</returns>
    public static IRetryPolicy CreatePolicy(Action<IRetryPolicyConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new RetryConfigurator();

        configure(configurator);

        return configurator.Build();
    }

    /// <summary>Creates a filter that retries every exception except the specified types.</summary>
    /// <param name="exceptionTypes">The exception types excluded from retries.</param>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Except(params Type[] exceptionTypes)
    {
        return new IgnoreExceptionFilter(exceptionTypes);
    }

    /// <summary>Creates a filter that retries every exception except the specified type.</summary>
    /// <typeparam name="T1">The exception type excluded from retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Except<T1>()
        where T1 : Exception
    {
        return new IgnoreExceptionFilter(typeof(T1));
    }

    /// <summary>Creates a filter that retries every exception except the specified types.</summary>
    /// <typeparam name="T1">The first exception type excluded from retries.</typeparam>
    /// <typeparam name="T2">The second exception type excluded from retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Except<T1, T2>()
        where T1 : Exception
        where T2 : Exception
    {
        return new IgnoreExceptionFilter(typeof(T1), typeof(T2));
    }

    /// <summary>Creates a filter that retries every exception except the specified types.</summary>
    /// <typeparam name="T1">The first exception type excluded from retries.</typeparam>
    /// <typeparam name="T2">The second exception type excluded from retries.</typeparam>
    /// <typeparam name="T3">The third exception type excluded from retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Except<T1, T2, T3>()
        where T1 : Exception
        where T2 : Exception
        where T3 : Exception
    {
        return new IgnoreExceptionFilter(typeof(T1), typeof(T2), typeof(T3));
    }

    /// <summary>Creates a filter that retries only the specified exception types.</summary>
    /// <param name="exceptionTypes">The exception types included in retries.</param>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Selected(params Type[] exceptionTypes)
    {
        return new HandleExceptionFilter(exceptionTypes);
    }

    /// <summary>Creates a filter that retries only the specified exception type.</summary>
    /// <typeparam name="T1">The exception type included in retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Selected<T1>()
        where T1 : Exception
    {
        return new HandleExceptionFilter(typeof(T1));
    }

    /// <summary>Creates a filter that retries only the specified exception types.</summary>
    /// <typeparam name="T1">The first exception type included in retries.</typeparam>
    /// <typeparam name="T2">The second exception type included in retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Selected<T1, T2>()
        where T1 : Exception
        where T2 : Exception
    {
        return new HandleExceptionFilter(typeof(T1), typeof(T2));
    }

    /// <summary>Creates a filter that retries only the specified exception types.</summary>
    /// <typeparam name="T1">The first exception type included in retries.</typeparam>
    /// <typeparam name="T2">The second exception type included in retries.</typeparam>
    /// <typeparam name="T3">The third exception type included in retries.</typeparam>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Selected<T1, T2, T3>()
        where T1 : Exception
        where T2 : Exception
        where T3 : Exception
    {
        return new HandleExceptionFilter(typeof(T1), typeof(T2), typeof(T3));
    }

    /// <summary>Gets a filter that retries every exception.</summary>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter All()
    {
        return _all;
    }

    /// <summary>Applies a predicate when the exception chain contains the selected type.</summary>
    /// <typeparam name="T">The exception type.</typeparam>
    /// <param name="filter">Determines whether a matching exception is retried.</param>
    /// <returns>The exception filter.</returns>
    public static IExceptionFilter Filter<T>(Func<T, bool> filter)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(filter);

        return new FilterExceptionFilter<T>(filter);
    }


    sealed class RetryConfigurator :
        ExceptionSpecification,
        IRetryPolicyConfigurator,
        ISpecification
    {
        RetryPolicyFactory? _policyFactory;

        public void SetRetryPolicy(RetryPolicyFactory factory)
        {
            _policyFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            if (_policyFactory == null)
                yield return this.Failure("RetryPolicy", "must not be null");
        }

        public IRetryPolicy Build()
        {
            IReadOnlyList<ValidationResult> result = Validate().ThrowIfContainsFailure("The retry configuration is invalid:");
            RetryPolicyFactory policyFactory = _policyFactory
                ?? throw new InvalidOperationException("A retry policy must be configured before it is built.");

            try
            {
                return policyFactory(Filter)
                    ?? throw new InvalidOperationException("The retry policy factory returned null.");
            }
            catch (Exception ex)
            {
                throw new ConfigurationException(result, global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Retry", "unknown", "An exception occurred during retry policy creation", "Correct the named configuration before starting the host"), ex);
            }
        }
    }
}
