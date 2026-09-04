using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides extension methods for task completion source.
/// </summary>
public static class TaskCompletionSourceExtensions
{
    /// <summary>
    /// Sets completed.
    /// </summary>
    /// <param name="source">The source value.</param>
    public static void SetCompleted(this TaskCompletionSource<bool> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.TrySetResult(true);
    }
}
