#nullable enable
namespace ViciOne.ServiceBus.Util;

using System;
using System.Threading;
using System.Threading.Tasks;


public static class CancellationTokenExtensions
{
    /// <summary>
    /// Completes a task when the token is canceled. This models cancellation as a signal and does not
    /// create a canceled Task, which is useful for Task.WhenAny coordination.
    /// </summary>
    public static CancellationTokenRegistration RegisterTask(this CancellationToken cancellationToken, out Task cancelTask)
    {
        if (!cancellationToken.CanBeCanceled)
            throw new ArgumentException("The cancellation token must support cancellation.", nameof(cancellationToken));

        TaskCompletionSource source = TaskCompletionSources.Create();
        cancelTask = source.Task;
        return cancellationToken.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), source);
    }

    public static CancellationTokenRegistration RegisterIfCanBeCanceled(this CancellationToken cancellationToken, CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return cancellationToken.CanBeCanceled
            ? cancellationToken.Register(static state => ((CancellationTokenSource)state!).Cancel(), source)
            : default;
    }
}
