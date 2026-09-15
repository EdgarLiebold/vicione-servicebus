using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>Coordinates cancellation tokens with completion signals and dependent cancellation sources.</summary>
public static class CancellationTokenExtensions
{
    /// <summary>
    /// Completes a task when the token is canceled. This models cancellation as a signal and does not
    /// create a canceled Task, which is useful for Task.WhenAny coordination.
    /// </summary>
    /// <param name="cancellationToken">The cancellable token whose cancellation completes the signal.</param>
    /// <param name="cancelTask">Receives the task that completes successfully when cancellation is requested.</param>
    /// <returns>The registration to dispose when the cancellation signal is no longer needed.</returns>
    public static CancellationTokenRegistration RegisterTask(this CancellationToken cancellationToken, out Task cancelTask)
    {
        if (!cancellationToken.CanBeCanceled)
            throw new ArgumentException("The cancellation token must support cancellation.", nameof(cancellationToken));

        TaskCompletionSource source = TaskCompletionSources.Create();
        cancelTask = source.Task;
        return cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), source);
    }

    /// <summary>Cancels a dependent source when the supplied token is canceled.</summary>
    /// <param name="cancellationToken">The token whose cancellation is forwarded, if it supports cancellation.</param>
    /// <param name="source">The dependent cancellation source, which must remain alive until the registration is disposed.</param>
    /// <returns>The forwarding registration, or an empty registration when the token cannot be canceled.</returns>
    public static CancellationTokenRegistration RegisterIfCanBeCanceled(this CancellationToken cancellationToken, CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state => ((CancellationTokenSource)state!).Cancel(), source)
            : default;
    }
}
