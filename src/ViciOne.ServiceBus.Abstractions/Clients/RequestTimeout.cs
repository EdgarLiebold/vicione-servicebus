using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Represents an optional, strictly positive request timeout.</summary>
public readonly struct RequestTimeout :
    IEquatable<RequestTimeout>
{
    readonly TimeSpan? _timeout;

    /// <summary>Initializes a timeout with an explicit duration.</summary>
    /// <param name="timeout">The maximum time to wait for the request to complete.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout" /> is not positive.</exception>
    public RequestTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The request timeout must be greater than zero.");

        _timeout = timeout;
    }

    /// <summary>Gets whether an explicit timeout was supplied.</summary>
    public bool HasValue => _timeout.HasValue;

    /// <summary>Gets the explicit timeout duration.</summary>
    /// <exception cref="InvalidOperationException">No explicit timeout was supplied.</exception>
    public TimeSpan Value => _timeout ?? throw new InvalidOperationException("No explicit request timeout was supplied.");

    /// <summary>Gets a value that leaves timeout selection to the configured request client.</summary>
    public static RequestTimeout None { get; } = default;

    /// <summary>Gets the built-in request timeout used when no application default is configured.</summary>
    public static RequestTimeout Default { get; } = new RequestTimeout(TimeSpan.FromSeconds(30));

    /// <summary>Determines whether this value and <paramref name="other" /> represent the same timeout.</summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns><see langword="true" /> when both values contain the same duration or are both unspecified.</returns>
    public bool Equals(RequestTimeout other)
    {
        return Nullable.Equals(_timeout, other._timeout);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RequestTimeout other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _timeout.GetHashCode();
    }

    /// <summary>Determines whether two request-timeout values are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the values are equal.</returns>
    public static bool operator ==(RequestTimeout left, RequestTimeout right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether two request-timeout values differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true" /> when the values differ.</returns>
    public static bool operator !=(RequestTimeout left, RequestTimeout right)
    {
        return !left.Equals(right);
    }

    /// <summary>Returns this value when it is explicit; otherwise returns <paramref name="fallback" />.</summary>
    /// <param name="fallback">The timeout to use when this value is unspecified.</param>
    /// <returns>This value or <paramref name="fallback" />.</returns>
    public RequestTimeout Or(RequestTimeout fallback)
    {
        if (HasValue)
            return this;

        return fallback;
    }
}
