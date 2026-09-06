using System;
using System.Runtime.InteropServices;

namespace ViciOne.ServiceBus.Internals;

static class StringExtensions
{
    /// <summary>Allows null-safe trimming of string.</summary>
    /// <param name="s">The <c>s</c> value.</param>
    /// <returns>The string produced by the operation.</returns>
    internal static string? NullSafeTrim(this string? s)
    {
        return s?.Trim();
    }

    /// <summary>Trims string and if resulting string is empty, null is returned.</summary>
    /// <param name="s">The <c>s</c> value.</param>
    /// <returns>The string produced by the operation.</returns>
    internal static string? TrimEmptyToNull(this string? s)
    {
        if (s is null)
            return null;

        s = s.Trim();

        if (s.Length == 0)
            return null;

        return s;
    }

    internal static StringSplitEnumerator SpanSplit(this string str, char ch1, char ch2 = char.MinValue)
    {
        return SpanSplit(str.AsSpan(), ch1, ch2);
    }

    internal static StringSplitEnumerator SpanSplit(this ReadOnlySpan<char> span, char ch1, char ch2 = char.MinValue)
    {
        return new StringSplitEnumerator(span, ch1, ch2);
    }


    // A ref struct confines the enumerator's ReadOnlySpan<char> to the stack.
    [StructLayout(LayoutKind.Auto)]
    internal ref struct StringSplitEnumerator
    {
        ReadOnlySpan<char> _str;
        readonly char ch1;
        readonly char ch2;

        public StringSplitEnumerator(ReadOnlySpan<char> str, char ch1, char ch2)
        {
            _str = str;
            this.ch1 = ch1;
            this.ch2 = ch2;
            Current = default;
        }

        // Supports compiler pattern-based foreach enumeration.
        public StringSplitEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            ReadOnlySpan<char> span = _str;
            if (span.Length == 0) // Reach the end of the string
                return false;

            var index = ch2 != char.MinValue
                ? span.IndexOfAny(ch1, ch2)
                : span.IndexOf(ch1);

            if (index == -1) // The string is composed of only token
            {
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                Current = new StringSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            Current = new StringSplitEntry(span.Slice(0, index), span.Slice(index, 1));
            _str = span.Slice(index + 1);
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
