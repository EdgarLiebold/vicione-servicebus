using System;
using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.NewIdParsers;

/// <summary>Parses base32 values.</summary>
public class Base32Parser :
    INewIdParser
{
    const string ConvertChars = "abcdefghijklmnopqrstuvwxyz234567ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    const string HexChars = "0123456789ABCDEF";
    const string InvalidInputString = "The input string contains invalid characters";

    readonly string _chars;

    /// <summary>Initializes a new instance.</summary>
    public Base32Parser()
        : this(ConvertChars)
    {
    }

    /// <summary>Creates a parser with one or more 32-character alphabets.</summary>
    /// <param name="chars">The accepted alphabets, concatenated in 32-character groups.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chars" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="chars" /> is not a multiple of 32 characters.</exception>
    public Base32Parser(string chars)
    {
        ArgumentNullException.ThrowIfNull(chars);
        if (chars.Length % 32 != 0)
            throw new ArgumentException("The characters must be a multiple of 32", nameof(chars));

        _chars = chars;
    }

    /// <summary>Parses the supplied Base32 representation.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentException"><paramref name="text" /> is not a valid 26-character representation.</exception>
    public NewId Parse(ReadOnlySpan<char> text)
    {
        if (text.Length != 26)
            throw new ArgumentException("The input string must be 26 characters", nameof(text));

        Span<char> buffer = stackalloc char[32];

        var bufferOffset = 0;
        var offset = 0;
        long number;
        for (var i = 0; i < 6; ++i)
        {
            number = 0;
            for (var j = 0; j < 4; j++)
            {
                var index = _chars.IndexOf(text[offset + j]);
                if (index < 0)
                    throw new ArgumentException(InvalidInputString);

                number = number * 32 + index % 32;
            }

            ConvertLongToBase16(buffer, bufferOffset, number, 5);

            offset += 4;
            bufferOffset += 5;
        }

        number = 0;
        for (var j = 0; j < 2; j++)
        {
            var index = _chars.IndexOf(text[offset + j]);
            if (index < 0)
                throw new ArgumentException(InvalidInputString);

            number = number * 32 + index % 32;
        }

        ConvertLongToBase16(buffer, bufferOffset, number, 2);

        return new NewId(new string(buffer));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void ConvertLongToBase16(Span<char> buffer, int offset, long value, int count)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            var index = (int)(value % 16);
            buffer[offset + i] = HexChars[index];
            value /= 16;
        }
    }
}
