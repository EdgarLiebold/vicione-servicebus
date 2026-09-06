using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;
/// <summary>Projects an asynchronous initializer result and applies an optional fallback.</summary>
public static class TaskInitializerExtensions
{
    /// <summary>Awaits the source and projects its value, or returns no value for a null source.</summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the selected value.</returns>
    public static async Task<TResult?> SelectAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);

        return await ProjectAsync(source, selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the source and projects its value, using <paramref name="fallback"/> when either the
    /// source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallback">The fallback used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the selected value.</returns>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback, CancellationToken cancellationToken = default)
        where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallback);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result is null ? fallback : result;
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},TResult,CancellationToken)"/>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallback">The fallback used by the operation.</param>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result ?? fallback;
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking <paramref name="fallbackFactory"/> only
    /// when either the source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallbackFactory">The fallback factory used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the selected value.</returns>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        if (result is not null)
            return result;

        return fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{TResult},CancellationToken)"/>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallbackFactory">The fallback factory used by the operation.</param>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result ?? fallbackFactory();
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking and awaiting
    /// <paramref name="fallbackFactory"/> only when either the source or the selected value is null.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallbackFactory">The fallback factory used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the selected value.</returns>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : class
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        if (result is not null)
            return result;

        Task<TResult> fallbackTask = fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a Task.");

        return await fallbackTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The fallbackFactory task must produce a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{Task{TResult}},CancellationToken)"/>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="source">The source used by the operation.</param>
    /// <param name="selector">The selector used by the operation.</param>
    /// <param name="fallbackFactory">The fallback factory used by the operation.</param>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory, CancellationToken cancellationToken = default)
        where TResult : struct
    {
        cancellationToken.ThrowIfCancellationRequested(); ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        if (result is not null)
            return result.Value;

        Task<TResult> fallbackTask = fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a Task.");

        return await fallbackTask.ConfigureAwait(false);
    }

    private static async Task<TResult?> ProjectAsync<TSource, TResult>(
        Task<TSource> source,
        Func<TSource, TResult?> selector)
    {
        TSource sourceValue = await source.ConfigureAwait(false);
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
