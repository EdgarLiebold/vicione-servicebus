using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides terminal and projection operations for asynchronous test-observation sequences.</summary>
public static class AsyncElementListExtensions
{
    /// <summary>Returns the first observation in a sequence.</summary>
    /// <typeparam name="TElement">The observed element type.</typeparam>
    /// <param name="elements">The observation sequence.</param>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>The first observation.</returns>
    public static async Task<TElement> FirstObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        ArgumentNullException.ThrowIfNull(elements);

        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        throw new InvalidOperationException("The observation sequence completed without an element.");
    }

    /// <summary>Counts the observations produced before a sequence completes.</summary>
    /// <typeparam name="TElement">The observed element type.</typeparam>
    /// <param name="elements">The observation sequence.</param>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>The number of observations produced.</returns>
    public static async Task<int> CountObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements, CancellationToken cancellationToken = default)
        where TElement : class
    {
        ArgumentNullException.ThrowIfNull(elements);

        var count = 0;
        await foreach (var _ in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            count++;

        return count;
    }

    /// <summary>Returns the first observation in a sequence, or <see langword="null"/> when the sequence completes empty.</summary>
    /// <typeparam name="TElement">The observed element type.</typeparam>
    /// <param name="elements">The observation sequence.</param>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>The first observation, or <see langword="null"/>.</returns>
    public static async Task<TElement?> FirstObservedOrDefaultAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        ArgumentNullException.ThrowIfNull(elements);

        await foreach (var element in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return element;

        return default;
    }

    /// <summary>Determines whether a sequence produces at least one observation.</summary>
    /// <typeparam name="TElement">The observed element type.</typeparam>
    /// <param name="elements">The observation sequence.</param>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns><see langword="true"/> when an observation is produced; otherwise, <see langword="false"/>.</returns>
    public static async Task<bool> AnyObservedAsync<TElement>(this IAsyncEnumerable<TElement> elements,
        CancellationToken cancellationToken = default)
        where TElement : class
    {
        ArgumentNullException.ThrowIfNull(elements);

        await foreach (var _ in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
            return true;

        return false;
    }

    /// <summary>Returns observations assignable to a specified result type.</summary>
    /// <typeparam name="TResult">The observation type to return.</typeparam>
    /// <param name="elements">The observation sequence.</param>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>The observations assignable to <typeparamref name="TResult"/>.</returns>
    public static async IAsyncEnumerable<TResult> OfTypeAsync<TResult>(this IAsyncEnumerable<object> elements,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(elements);

        await foreach (var entry in elements.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (entry is TResult result)
                yield return result;
        }
    }

    /// <summary>Deconstructs a send observation into its message and context.</summary>
    /// <param name="sent">The send observation.</param>
    /// <param name="message">Receives the sent message.</param>
    /// <param name="context">Receives the send context.</param>
    public static void Deconstruct(this ISentMessage sent, out object message, out SendContext context)
    {
        ArgumentNullException.ThrowIfNull(sent);
        context = sent.Context;
        message = sent.MessageObject;
    }

    /// <summary>Deconstructs a typed send observation into its message and context.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="sent">The send observation.</param>
    /// <param name="message">Receives the sent message.</param>
    /// <param name="context">Receives the typed send context.</param>
    public static void Deconstruct<TMessage>(this ISentMessage<TMessage> sent, out TMessage message, out SendContext<TMessage> context)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(sent);
        context = sent.Context;
        message = sent.Context.Message;
    }
}
