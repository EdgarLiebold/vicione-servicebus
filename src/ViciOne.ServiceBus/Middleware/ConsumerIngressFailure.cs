using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

internal static class ConsumerIngressFailure
{
    public static bool IsUnexpectedCancellation(Exception exception, CancellationToken deliveryToken)
    {
        if (deliveryToken.IsCancellationRequested || exception is ConsumerCanceledException)
            return false;

        return IsCancellation(exception, false);
    }

    public static bool IsCancellation(Exception exception) => IsCancellation(exception, true);

    static bool IsCancellation(Exception exception, bool includeClassified)
    {
        var pending = new Stack<Exception>();
        var visited = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        bool foundCancellation = false;
        pending.Push(exception);

        while (pending.Count > 0)
        {
            Exception current = pending.Pop();
            if (!visited.Add(current))
                continue;

            if (current is AggregateException aggregate)
            {
                if (aggregate.InnerExceptions.Count == 0)
                    return false;

                foreach (Exception inner in aggregate.InnerExceptions)
                    pending.Push(inner);

                continue;
            }

            if (current is ConsumerCanceledException)
            {
                if (!includeClassified)
                    return false;

                foundCancellation = true;
                continue;
            }

            if (current is OperationCanceledException)
            {
                foundCancellation = true;
                continue;
            }

            Exception? innerException = current.InnerException;
            Exception? baseException;
            try
            {
                baseException = current.GetBaseException();
            }
            catch
            {
                baseException = null;
            }
            bool hasBase = baseException is not null && !ReferenceEquals(baseException, current);
            if (innerException is not null)
                pending.Push(innerException);
            if (baseException is not null && hasBase && !ReferenceEquals(baseException, innerException))
                pending.Push(baseException);

            if (innerException is not null || hasBase)
                continue;

            return false;
        }

        return foundCancellation;
    }

    public static async Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context,
        TimeSpan duration, string consumerType, Exception originalFailure, Exception operationFailure)
        where TMessage : class
    {
        try
        {
            await context.NotifyFaultedAsync(duration, consumerType, originalFailure).ConfigureAwait(false);
        }
        catch (Exception notificationFailure)
        {
            throw new AggregateException(operationFailure, notificationFailure);
        }
    }
}
