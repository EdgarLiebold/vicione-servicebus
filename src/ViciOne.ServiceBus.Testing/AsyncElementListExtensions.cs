using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides extension methods for async element list.</summary>
public static class AsyncElementListExtensions
{
    /// <summary>Returns the first observed matching message.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the first observed outcome.</returns>
    public static async Task<TElement> FirstObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        throw new InvalidOperationException("Message List was empty, or timed out");
    }

    /// <summary>Returns the number of observed matching messages.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the count observed outcome.</returns>
    public static async Task<int> CountObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class
    {
        var count = 0;
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            count++;

        return count;
    }

    /// <summary>Returns the number of matching values.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The int produced by the operation.</returns>
    public static int Count<TElement>(this IAsyncElementList<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class, IAsyncListElement
    {
        return elements.Select(x => true, cancellationToken).Count();
    }


    /// <summary>Returns the first observed matching message, or the default value.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the first observed or default outcome.</returns>
    public static async Task<TElement?> FirstObservedOrDefaultAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        return default;
    }

    /// <summary>Determines whether any matching message was observed.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the any observed outcome.</returns>
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

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="TElement">The element type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="elements">The elements.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence containing the selected value.</returns>
    public static async IAsyncEnumerable<TResult> SelectAsync<TElement, TResult>(this IAsyncEnumerable<TElement> elements,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TElement : class
        where TResult : class
    {
        await foreach (var entry in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (entry is TResult result)
                yield return result;
        }
    }

    /// <summary>Deconstructs this value into its components.</summary>
    /// <param name="sent">The sent.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <param name="context">The context associated with the operation.</param>
    public static void Deconstruct(this ISentMessage sent, out object message, out SendContext context)
    {
        context = sent.Context;
        message = sent.MessageObject;
    }

    /// <summary>Deconstructs this value into its components.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="sent">The sent.</param>
    /// <param name="message">Receives the message produced by the operation.</param>
    /// <param name="context">The context associated with the operation.</param>
    public static void Deconstruct<TMessage>(this ISentMessage<TMessage> sent, out TMessage message, out SendContext context)
        where TMessage : class
    {
        context = sent.Context;
        message = sent.Context.Message;
    }
}
