using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;

public static class TaskCompletionSourceExtensions
{
    public static void SetCompleted(this TaskCompletionSource<bool> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.TrySetResult(true);
    }
}
