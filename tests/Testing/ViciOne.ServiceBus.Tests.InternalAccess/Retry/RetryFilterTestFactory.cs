using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Retry;

/// <summary>Creates retry filters without exposing their implementation types as product API.</summary>
public static class RetryFilterTestFactory
{
    /// <summary>Creates a retry filter and optionally connects one lifecycle observer.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    /// <param name="retryPolicy">The retry policy to execute.</param>
    /// <param name="observer">The optional observer to connect.</param>
    /// <returns>The retry filter through its pipeline contract.</returns>
    public static IFilter<TContext> Create<TContext>(IRetryPolicy retryPolicy, IRetryObserver? observer = null)
        where TContext : class, PipeContext
    {
        var observers = new RetryObservable();
        if (observer != null)
            observers.Connect(observer);

        return new RetryFilter<TContext>(retryPolicy, observers);
    }

    /// <summary>Adds untyped consume state and bus-lifetime cancellation to a retry policy.</summary>
    /// <param name="retryPolicy">The retry policy to decorate.</param>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
    /// <returns>The consume-aware policy through its public contract.</returns>
    public static IRetryPolicy CreateConsumeContextPolicy(IRetryPolicy retryPolicy, CancellationToken cancellationToken = default)
    {
        return new ConsumeContextRetryPolicy(retryPolicy, cancellationToken);
    }

    /// <summary>Adds typed consume state and bus-lifetime cancellation to a retry policy.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="retryPolicy">The retry policy to decorate.</param>
    /// <param name="cancellationToken">The token that cancels pending retry delays.</param>
    /// <returns>The consume-aware policy through its public contract.</returns>
    public static IRetryPolicy CreateTypedConsumeContextPolicy<TMessage>(
        IRetryPolicy retryPolicy,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        new ConsumeContextRetryPolicy<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(
            retryPolicy, cancellationToken,
            (context, policy, retryContext) => new RetryConsumeContext<TMessage>(context, policy, retryContext));

    /// <summary>Creates a typed policy with a caller-supplied initial consume projection.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="retryPolicy">The policy to decorate.</param>
    /// <param name="projection">Projects the initial context, or supplies invalid output for admission tests.</param>
    /// <param name="cancellationToken">The token that cancels retry processing.</param>
    /// <returns>The consume-aware policy through its public contract.</returns>
    public static IRetryPolicy CreateProjectedConsumeContextPolicy<TMessage>(IRetryPolicy retryPolicy,
        Func<ConsumeContext<TMessage>, ConsumeContext<TMessage>?> projection,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(projection);
        return new ConsumeContextRetryPolicy<ConsumeContext<TMessage>, RetryConsumeContext<TMessage>>(
            retryPolicy, cancellationToken, (context, policy, retryContext) =>
            {
                ConsumeContext<TMessage>? projected = projection(context);
                return projected == null ? null! : new RetryConsumeContext<TMessage>(projected, policy, retryContext);
            });
    }

    /// <summary>Invokes the retry-filter constructor without a policy.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    public static void ConstructWithoutPolicy<TContext>()
        where TContext : class, PipeContext
    {
        _ = new RetryFilter<TContext>(null!, new RetryObservable());
    }

    /// <summary>Invokes the retry-filter constructor without an observer collection.</summary>
    /// <typeparam name="TContext">The pipeline context type.</typeparam>
    public static void ConstructWithoutObservers<TContext>()
        where TContext : class, PipeContext
    {
        _ = new RetryFilter<TContext>(Advanced.Retry.None, null!);
    }
}
