namespace ViciOne.ServiceBus.Util;

using System;
using System.Threading.Tasks;


public static class TaskCompletionSourceExtensions
{
    public static void SetCompleted(this TaskCompletionSource<bool> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.TrySetResult(true);
    }
}
