using System;
using System.Runtime.InteropServices;

namespace ViciOne.ServiceBus.Internals;

static class StringExtensions
{
    /// <summary>Trims a string while preserving a <see langword="null" /> receiver.</summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string, or <see langword="null" /> when <paramref name="value" /> is <see langword="null" />.</returns>
    internal static string? NullSafeTrim(this string? value)
    {
        return value?.Trim();
    }

    /// <summary>Trims a string and normalizes an empty result to <see langword="null" />.</summary>
    /// <param name="value">The string to normalize.</param>
    /// <returns>The nonempty trimmed string, or <see langword="null" /> when no characters remain.</returns>
    internal static string? TrimEmptyToNull(this string? value)
    {
        if (value is null)
            return null;

        value = value.Trim();

        if (value.Length == 0)
            return null;

        return value;
    }

    internal static StringSplitEnumerator SpanSplit(this string value, char separator, char alternateSeparator = char.MinValue)
    {
        return SpanSplit(value.AsSpan(), separator, alternateSeparator);
    }

    internal static StringSplitEnumerator SpanSplit(this ReadOnlySpan<char> span, char separator, char alternateSeparator = char.MinValue)
    {
        return new StringSplitEnumerator(span, separator, alternateSeparator);
    }

    // A ref struct confines the enumerator's ReadOnlySpan<char> to the stack.
    [StructLayout(LayoutKind.Auto)]
    internal ref struct StringSplitEnumerator
    {
        readonly char _alternateSeparator;
        ReadOnlySpan<char> _remaining;
        readonly char _separator;

        public StringSplitEnumerator(ReadOnlySpan<char> value, char separator, char alternateSeparator)
        {
            _remaining = value;
            _separator = separator;
            _alternateSeparator = alternateSeparator;
            Current = default;
        }

        // Supports compiler pattern-based foreach enumeration.
        public StringSplitEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            ReadOnlySpan<char> span = _remaining;
            if (span.Length == 0)
                return false;

            int index = _alternateSeparator != char.MinValue
                ? span.IndexOfAny(_separator, _alternateSeparator)
                : span.IndexOf(_separator);

            if (index < 0)
            {
                _remaining = ReadOnlySpan<char>.Empty;
                Current = new StringSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            Current = new StringSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _remaining = span.Slice(index + 1);
            return true;
        }

        public StringSplitEntry Current { get; private set; }
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly ref struct StringSplitEntry
    {
        public StringSplitEntry(ReadOnlySpan<char> token, ReadOnlySpan<char> separator)
        {
            Token = token;
            Separator = separator;
        }

        public ReadOnlySpan<char> Token { get; }
        public ReadOnlySpan<char> Separator { get; }

        // Exposes token and separator through tuple deconstruction.
        public void Deconstruct(out ReadOnlySpan<char> line, out ReadOnlySpan<char> separator)
        {
            line = Token;
            separator = Separator;
        }

        // Projects an entry to its token span.
        public static implicit operator ReadOnlySpan<char>(StringSplitEntry entry)
        {
            return entry.Token;
        }
    }
}
