#nullable enable
namespace ViciOne.ServiceBus.Initializers;

using System;
using System.Threading.Tasks;

/// <summary>Projects an asynchronous initializer result and applies an optional fallback.</summary>
public static class TaskInitializerExtensions
{
    /// <summary>Awaits the source and projects its value, or returns no value for a null source.</summary>
    public static async Task<TResult?> SelectAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector)
    {
        ValidateProjectionArguments(source, selector);

        return await ProjectAsync(source, selector).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the source and projects its value, using <paramref name="fallback"/> when either the
    /// source or the selected value is null.
    /// </summary>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallback);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result is null ? fallback : result;
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},TResult)"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        TResult fallback)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result ?? fallback;
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking <paramref name="fallbackFactory"/> only
    /// when either the source or the selected value is null.
    /// </summary>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        if (result is not null)
            return result;

        return fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{TResult})"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<TResult> fallbackFactory)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        return result ?? fallbackFactory();
    }

    /// <summary>
    /// Awaits the source and projects its value, invoking and awaiting
    /// <paramref name="fallbackFactory"/> only when either the source or the selected value is null.
    /// </summary>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory)
        where TResult : class
    {
        ValidateProjectionArguments(source, selector);
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        TResult? result = await ProjectAsync(source, selector).ConfigureAwait(false);
        if (result is not null)
            return result;

        Task<TResult> fallbackTask = fallbackFactory()
            ?? throw new InvalidOperationException("The fallbackFactory must return a Task.");

        return await fallbackTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The fallbackFactory task must produce a value.");
    }

    /// <inheritdoc cref="SelectOrFallbackAsync{TSource,TResult}(Task{TSource},Func{TSource,TResult},Func{Task{TResult}})"/>
    public static async Task<TResult> SelectOrFallbackAsync<TSource, TResult>(
        this Task<TSource> source,
        Func<TSource, TResult?> selector,
        Func<Task<TResult>> fallbackFactory)
        where TResult : struct
    {
        ValidateProjectionArguments(source, selector);
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
