using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides extension methods for cancellation token.
/// </summary>
public static class CancellationTokenExtensions
{
    /// <summary>
    /// Completes a task when the token is canceled. This models cancellation as a signal and does not
    /// create a canceled Task, which is useful for Task.WhenAny coordination.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="cancelTask">The cancel task used by the operation.</param>
    public static CancellationTokenRegistration RegisterTask(this CancellationToken cancellationToken, out Task cancelTask)
    {
        if (!cancellationToken.CanBeCanceled)
            throw new ArgumentException("The cancellation token must support cancellation.", nameof(cancellationToken));

        TaskCompletionSource source = TaskCompletionSources.Create();
        cancelTask = source.Task;
        return cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), source);
    }

    /// <summary>
    /// Performs the register if can be canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="source">The source value.</param>
    /// <returns>The result of the operation.</returns>
    public static CancellationTokenRegistration RegisterIfCanBeCanceled(this CancellationToken cancellationToken, CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state => ((CancellationTokenSource)state!).Cancel(), source)
            : default;
    }
}
