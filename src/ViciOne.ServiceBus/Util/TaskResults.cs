using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Shared completed task results used on allocation-sensitive paths.
/// </summary>
public static class TaskResults
{
    public static Task Completed => Task.CompletedTask;
    public static Task<bool> False => Cached.False;
    public static Task<bool> True => Cached.True;

    public static Task<T?> Default<T>() => Cached<T>.Default;

    public static Task<T> Faulted<T>(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Task.FromException<T>(exception);
    }

    public static Task<T> Canceled<T>() => Cached<T>.Canceled;


    static class Cached
    {
        public static readonly Task<bool> True = Task.FromResult(true);
        public static readonly Task<bool> False = Task.FromResult(false);
    }


    static class Cached<T>
    {
        public static readonly Task<T?> Default = Task.FromResult<T?>(default);
        public static readonly Task<T> Canceled = Task.FromCanceled<T>(new CancellationToken(canceled: true));
    }
}
