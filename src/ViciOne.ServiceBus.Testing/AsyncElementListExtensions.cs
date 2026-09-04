using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

public static class AsyncElementListExtensions
{
    public static async Task<TElement> FirstObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        throw new InvalidOperationException("Message List was empty, or timed out");
    }

    public static async Task<int> CountObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class
    {
        var count = 0;
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            count++;

        return count;
    }

    public static int Count<TElement>(this IAsyncElementList<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class, IAsyncListElement
    {
        return elements.Select(x => true, cancellationToken).Count();
    }


    public static async Task<TElement?> FirstObservedOrDefaultAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        return default;
    }

    public static async Task<bool> AnyObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        try
        {
            await foreach (var _ in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
                return true;
        }
        catch (OperationCanceledException)
        {
        }

        return false;
    }

    public static async IAsyncEnumerable<TResult> Select<TElement, TResult>(this IAsyncEnumerable<TElement> elements)
        where TElement : class
        where TResult : class
    {
        await foreach (var entry in elements.ConfigureAwait(false))
        {
            if (entry is TResult result)
                yield return result;
        }
    }

    public static void Deconstruct(this ISentMessage sent, out object message, out SendContext context)
    {
        context = sent.Context;
        message = sent.MessageObject;
    }

    public static void Deconstruct<TMessage>(this ISentMessage<TMessage> sent, out TMessage message, out SendContext context)
        where TMessage : class
    {
        context = sent.Context;
        message = sent.Context.Message;
    }
}
