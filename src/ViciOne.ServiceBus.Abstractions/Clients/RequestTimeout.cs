using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// A timeout, which can be a default (none) or a valid TimeSpan > 0, includes factory methods to make it "cute"
/// </summary>
public readonly struct RequestTimeout :
    IEquatable<RequestTimeout>
{
    readonly TimeSpan? _timeout;

    RequestTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be > TimeSpan.Zero");

        _timeout = timeout;
    }

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => _timeout.HasValue && _timeout.Value > TimeSpan.Zero;

    /// <summary>
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TimeSpan Value => _timeout ?? throw new InvalidOperationException("RequestTimeout does not have a value");

    /// <summary>
    /// Gets the none value.
    /// </summary>
    public static RequestTimeout None { get; } = new RequestTimeout();
    /// <summary>
    /// Gets the default value.
    /// </summary>
    public static RequestTimeout Default { get; } = new RequestTimeout(TimeSpan.FromSeconds(30));

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(RequestTimeout other)
    {
        return Nullable.Equals(_timeout, other._timeout);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool Equals(object? obj)
    {
        return obj is RequestTimeout other && Equals(other);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return _timeout.GetHashCode();
    }

    /// <summary>
    /// Applies the <c>==</c> operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(RequestTimeout left, RequestTimeout right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Applies the <c>!=</c> operator.
    /// </summary>
    /// <param name="left">The left value.</param>
    /// <param name="right">The right value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(RequestTimeout left, RequestTimeout right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Converts a value to <see cref="RequestTimeout" />.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator RequestTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Must be > TimeSpan.Zero");

        return new RequestTimeout(timeout);
    }

    /// <summary>
    /// Converts a value to <see cref="RequestTimeout" />.
    /// </summary>
    /// <param name="milliseconds">The milliseconds value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator RequestTimeout(int milliseconds)
    {
        if (milliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Must be > 0");

        return After(ms: milliseconds);
    }

    /// <summary>
    /// If this timeout has a value, return it, otherwise, return the other timeout
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public RequestTimeout Or(RequestTimeout other)
    {
        if (HasValue)
            return this;

        return other;
    }

    /// <summary>
    /// Create a timeout using optional arguments to build it up
    /// </summary>
    /// <param name="d">days</param>
    /// <param name="h">hours</param>
    /// <param name="m">minutes</param>
    /// <param name="s">seconds</param>
    /// <param name="ms">milliseconds</param>
    /// <returns>The timeout value</returns>
    /// <exception cref="ArgumentException"></exception>
    public static RequestTimeout After(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null)
    {
        var timeSpan = new TimeSpan(d ?? 0, h ?? 0, m ?? 0, s ?? 0, ms ?? 0);
        if (timeSpan <= TimeSpan.Zero)
            throw new ArgumentException("The timeout must be > 0");

        return new RequestTimeout(timeSpan);
    }
}
