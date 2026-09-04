using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>
/// Provides a consume context retry policy implementation.
/// </summary>
public class ConsumeContextRetryPolicy :
    IRetryPolicy
{
    readonly CancellationToken _cancellationToken;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public ConsumeContextRetryPolicy(IRetryPolicy retryPolicy, CancellationToken cancellationToken)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry-consumeContext");

        _retryPolicy.Probe(scope);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        if (context is ConsumeContext consumeContext)
        {
            RetryPolicyContext<ConsumeContext> retryPolicyContext = _retryPolicy.CreatePolicyContext(consumeContext)
                ?? throw new InvalidOperationException("The retry policy returned a null consume policy context.");

            var retryConsumeContext = new RetryConsumeContext(consumeContext, _retryPolicy, null);

            return new ConsumeContextRetryPolicyContext(retryPolicyContext, retryConsumeContext, _cancellationToken) as RetryPolicyContext<T>
                ?? throw new InvalidOperationException($"The retry policy context cannot be represented as {TypeCache<T>.ShortName}.");
        }

        throw new ArgumentException("The argument must be a ConsumeContext", nameof(context));
    }

    /// <summary>
    /// Determines whether handled.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        return _retryPolicy.IsHandled(exception);
    }
}


/// <summary>
/// Provides a consume context retry policy implementation.
/// </summary>
/// <typeparam name="TFilter">The t filter type.</typeparam>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ConsumeContextRetryPolicy<TFilter, TContext> :
    IRetryPolicy
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly CancellationToken _cancellationToken;
    readonly Func<TFilter, IRetryPolicy, RetryContext?, TContext> _contextFactory;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="retryPolicy">The retry policy value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="contextFactory">The context factory value.</param>
    public ConsumeContextRetryPolicy(IRetryPolicy retryPolicy, CancellationToken cancellationToken,
        Func<TFilter, IRetryPolicy, RetryContext?, TContext> contextFactory)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _cancellationToken = cancellationToken;
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("retry-consumeContext");

        _retryPolicy.Probe(scope);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        var filterContext = context as TFilter;
        if (filterContext == null)
            throw new ArgumentException($"The argument must be a {typeof(TFilter).Name}", nameof(context));

        RetryPolicyContext<TFilter> retryPolicyContext = _retryPolicy.CreatePolicyContext(filterContext)
            ?? throw new InvalidOperationException("The retry policy returned a null consume policy context.");

        var retryConsumeContext = _contextFactory(filterContext, _retryPolicy, null)
            ?? throw new InvalidOperationException("The consume retry context factory returned null.");

        return new ConsumeContextRetryPolicyContext<TFilter, TContext>(retryPolicyContext, retryConsumeContext,
                _cancellationToken) as RetryPolicyContext<T>
            ?? throw new InvalidOperationException($"The retry policy context cannot be represented as {TypeCache<T>.ShortName}.");
    }

    /// <summary>
    /// Determines whether handled.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        return _retryPolicy.IsHandled(exception);
    }
}
