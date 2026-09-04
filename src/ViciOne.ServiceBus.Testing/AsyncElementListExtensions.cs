using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides extension methods for async element list.
/// </summary>
public static class AsyncElementListExtensions
{
    /// <summary>
    /// Performs the first observed operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<TElement> FirstObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        throw new InvalidOperationException("Message List was empty, or timed out");
    }

    /// <summary>
    /// Performs the count observed operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<int> CountObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class
    {
        var count = 0;
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            count++;

        return count;
    }

    /// <summary>
    /// Performs the count operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static int Count<TElement>(this IAsyncElementList<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class, IAsyncListElement
    {
        return elements.Select(x => true, cancellationToken).Count();
    }


    /// <summary>
    /// Performs the first observed or default operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<TElement?> FirstObservedOrDefaultAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        return default;
    }

    /// <summary>
    /// Performs the any observed operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="TElement">The t element type.</typeparam>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="elements">The elements value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <param name="sent">The sent value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="context">The operation context.</param>
    public static void Deconstruct(this ISentMessage sent, out object message, out SendContext context)
    {
        context = sent.Context;
        message = sent.MessageObject;
    }

    /// <summary>
    /// Deconstructs this value into its components.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="sent">The sent value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="context">The operation context.</param>
    public static void Deconstruct<TMessage>(this ISentMessage<TMessage> sent, out TMessage message, out SendContext context)
        where TMessage : class
    {
        context = sent.Context;
        message = sent.Context.Message;
    }
}
