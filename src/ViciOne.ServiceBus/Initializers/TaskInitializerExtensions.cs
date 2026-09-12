using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Projects an asynchronous initializer result and applies an optional fallback.</summary>
public static class TaskInitializerExtensions
{
    /// <summary>Awaits a value and projects it unless the awaited value is <see langword="null" />.</summary>
    /// <typeparam name="TSource">The awaited value type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="source">The task whose result is projected.</param>
    /// <param name="selector">The projection applied to a non-null source result.</param>
    /// <param name="cancellationToken">The token that stops waiting for <paramref name="source" />.</param>
    /// <returns>The projected value, or the default result when the source result is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> is canceled.</exception>
    public static async Task<TResult?> SelectAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector, CancellationToken cancellationToken = default)
    {
        ValidateProjectionArguments(source, selector);
        cancellationToken.ThrowIfCancellationRequested();

        return await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the source and projects its value, using <paramref name="fallback"/> when either the
    /// source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The awaited value type.</typeparam>
    /// <typeparam name="TResult">The projected reference type.</typeparam>
    /// <param name="source">The task whose result is projected.</param>
    /// <param name="selector">The projection applied to a non-null source result.</param>
    /// <param name="fallback">The non-null value returned when no projected value is available.</param>
    /// <param name="cancellationToken">The token that stops waiting for <paramref name="source" />.</param>
    /// <returns>The projected value when available; otherwise, <paramref name="fallback" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="selector" />, or <paramref name="fallback" /> is null.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> is canceled.</exception>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback, CancellationToken cancellationToken = default)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallback);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        return result is null ? fallback : result;
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},TResult,CancellationToken)"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        return result ?? fallback;
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking <paramref name="fallbackFactory"/> only
    /// when either the source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The awaited value type.</typeparam>
    /// <typeparam name="TResult">The projected reference type.</typeparam>
    /// <param name="source">The task whose result is projected.</param>
    /// <param name="selector">The projection applied to a non-null source result.</param>
    /// <param name="fallbackFactory">The factory invoked only when no projected value is available.</param>
    /// <param name="cancellationToken">The token that stops waiting for <paramref name="source" />.</param>
    /// <returns>The projected value when available; otherwise, the factory result.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="selector" />, or <paramref name="fallbackFactory" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException"><paramref name="fallbackFactory" /> returns null.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> is canceled.</exception>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        if (result is not null)
            return result;

        return fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{TResult},CancellationToken)"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        return result ?? fallbackFactory();
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking and awaiting
    /// <paramref name="fallbackFactory"/> only when either the source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The awaited value type.</typeparam>
    /// <typeparam name="TResult">The projected reference type.</typeparam>
    /// <param name="source">The task whose result is projected.</param>
    /// <param name="selector">The projection applied to a non-null source result.</param>
    /// <param name="fallbackFactory">The asynchronous factory invoked only when no projected value is available.</param>
    /// <param name="cancellationToken">The token that stops waiting for either task.</param>
    /// <returns>The projected value when available; otherwise, the awaited factory result.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source" />, <paramref name="selector" />, or <paramref name="fallbackFactory" /> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="fallbackFactory" /> returns a null task or its task completes with null.
    /// </exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken" /> is canceled.</exception>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        if (result is not null)
            return result;

        Task<TResult> fallbackTask = fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a Task.");

        return await fallbackTask.WaitAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The fallbackFactory task must produce a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{Task{TResult}},CancellationToken)"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        cancellationToken.ThrowIfCancellationRequested();

        TResult? result = await ProjectAsync(source, selector, cancellationToken).ConfigureAwait(false);
        if (result is not null)
            return result.Value;

        Task<TResult> fallbackTask = fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a Task.");

        return await fallbackTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<TResult?> ProjectAsync<TSource, TResult>(
        Task<TSource> source,
        Func<TSource, TResult?> selector,
        CancellationToken cancellationToken)
    {
        TSource sourceValue = await source.WaitAsync(cancellationToken).ConfigureAwait(false);
        return sourceValue is null ? default : selector(sourceValue);
    }

    private static void ValidateProjectionArguments<TSource, TResult>(
        Task<TSource> source,
        Func<TSource, TResult?> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
    }
}
