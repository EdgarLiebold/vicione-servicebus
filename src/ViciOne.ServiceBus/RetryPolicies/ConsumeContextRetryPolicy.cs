using System;
using System.Threading;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Adds consume-specific cancellation and deferred-fault state to a retry policy.</summary>
internal sealed class ConsumeContextRetryPolicy :
    IRetryPolicy
{
    readonly CancellationToken _cancellationToken;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a consume-aware wrapper for a retry policy.</summary>
    /// <param name="retryPolicy">The retry policy to wrap.</param>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
    public ConsumeContextRetryPolicy(IRetryPolicy retryPolicy, CancellationToken cancellationToken)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _cancellationToken = cancellationToken;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context that receives the nested policy scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("retry-consumeContext");

        _retryPolicy.Probe(scope);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is ConsumeContext consumeContext)
        {
            RetryPolicyContext<ConsumeContext> retryPolicyContext = _retryPolicy.CreatePolicyContext(consumeContext)
                ?? throw new InvalidOperationException("The retry policy returned a null consume policy context.");
            ConsumeContextRetryPolicyContext? consumePolicyContext = null;
            try
            {
                if (retryPolicyContext.Context == null)
                    throw new InvalidOperationException("The retry policy returned a policy context without a consume context.");

                var retryConsumeContext = new RetryConsumeContext(consumeContext, _retryPolicy, null);
                consumePolicyContext = new ConsumeContextRetryPolicyContext(retryPolicyContext, retryConsumeContext, _cancellationToken);
                return consumePolicyContext as RetryPolicyContext<T>
                    ?? throw new InvalidOperationException($"The retry policy context cannot be represented as {TypeCache<T>.ShortName}.");
            }
            catch (Exception primaryFailure)
            {
                RetryPolicyExecution.DisposeAfterFactoryFailure(consumePolicyContext ?? (IDisposable)retryPolicyContext,
                    primaryFailure);
                throw;
            }
        }

        throw new ArgumentException("The argument must be a ConsumeContext", nameof(context));
    }

    /// <summary>Delegates failure classification to the wrapped policy.</summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns><see langword="true" /> when the failure is eligible for retry; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _retryPolicy.IsHandled(exception);
    }
}


/// <summary>Adds a specialized consume context projection to a retry policy.</summary>
/// <typeparam name="TFilter">The pipeline contract exposed to the retry filter.</typeparam>
/// <typeparam name="TContext">The consume-retry context implementation.</typeparam>
internal sealed class ConsumeContextRetryPolicy<TFilter, TContext> :
    IRetryPolicy
    where TFilter : class, PipeContext
    where TContext : class, TFilter, ConsumeRetryContext
{
    readonly CancellationToken _cancellationToken;
    readonly Func<TFilter, IRetryPolicy, RetryContext?, TContext> _contextFactory;
    readonly IRetryPolicy _retryPolicy;

    /// <summary>Creates a consume-aware wrapper with a context projection.</summary>
    /// <param name="retryPolicy">The retry policy to wrap.</param>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
    /// <param name="contextFactory">Creates consume-aware state for the initial attempt.</param>
    public ConsumeContextRetryPolicy(IRetryPolicy retryPolicy, CancellationToken cancellationToken,
        Func<TFilter, IRetryPolicy, RetryContext?, TContext> contextFactory)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _cancellationToken = cancellationToken;
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context that receives the nested policy scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("retry-consumeContext");

        _retryPolicy.Probe(scope);
    }

    RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var filterContext = context as TFilter;
        if (filterContext == null)
            throw new ArgumentException($"The argument must be a {typeof(TFilter).Name}", nameof(context));

        RetryPolicyContext<TFilter> retryPolicyContext = _retryPolicy.CreatePolicyContext(filterContext)
            ?? throw new InvalidOperationException("The retry policy returned a null consume policy context.");
        ConsumeContextRetryPolicyContext<TFilter, TContext>? consumePolicyContext = null;
        try
        {
            if (retryPolicyContext.Context == null)
                throw new InvalidOperationException("The retry policy returned a policy context without a consume context.");

            var retryConsumeContext = _contextFactory(filterContext, _retryPolicy, null)
                ?? throw new InvalidOperationException("The consume retry context factory returned null.");

            consumePolicyContext = new ConsumeContextRetryPolicyContext<TFilter, TContext>(retryPolicyContext, retryConsumeContext,
                _cancellationToken);
            return consumePolicyContext as RetryPolicyContext<T>
                ?? throw new InvalidOperationException($"The retry policy context cannot be represented as {TypeCache<T>.ShortName}.");
        }
        catch (Exception primaryFailure)
        {
            RetryPolicyExecution.DisposeAfterFactoryFailure(consumePolicyContext ?? (IDisposable)retryPolicyContext,
                primaryFailure);
            throw;
        }
    }

    /// <summary>Delegates failure classification to the wrapped policy.</summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns><see langword="true" /> when the failure is eligible for retry; otherwise, <see langword="false" />.</returns>
    public bool IsHandled(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return _retryPolicy.IsHandled(exception);
    }
}
