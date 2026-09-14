using System;
using System.Runtime.InteropServices;

namespace ViciOne.ServiceBus.JobService.Scheduling;

internal static class SpanSplitExtensions
{
    internal static StringSplitEnumerator SpanSplit(
        this ReadOnlySpan<char> value,
        char separator,
        char alternateSeparator = char.MinValue)
    {
        return new StringSplitEnumerator(value, separator, alternateSeparator);
    }

    [StructLayout(LayoutKind.Auto)]
    internal ref struct StringSplitEnumerator
    {
        readonly char _alternateSeparator;
        bool _completed;
        ReadOnlySpan<char> _remaining;
        readonly char _separator;

        public StringSplitEnumerator(ReadOnlySpan<char> value, char separator, char alternateSeparator)
        {
            _remaining = value;
            _separator = separator;
            _alternateSeparator = alternateSeparator;
            _completed = false;
            Current = default;
        }

        public readonly StringSplitEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            if (_completed)
                return false;

            ReadOnlySpan<char> span = _remaining;
            int index = _alternateSeparator != char.MinValue
                ? span.IndexOfAny(_separator, _alternateSeparator)
                : span.IndexOf(_separator);

            if (index < 0)
            {
                _remaining = ReadOnlySpan<char>.Empty;
                _completed = true;
                Current = new StringSplitEntry(span, ReadOnlySpan<char>.Empty);
                return true;
            }

            Current = new StringSplitEntry(span[..index], span.Slice(index, 1));
            _remaining = span[(index + 1)..];
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

        public void Deconstruct(out ReadOnlySpan<char> token, out ReadOnlySpan<char> separator)
        {
            token = Token;
            separator = Separator;
        }

        public static implicit operator ReadOnlySpan<char>(StringSplitEntry entry)
        {
            return entry.Token;
        }
    }
}
