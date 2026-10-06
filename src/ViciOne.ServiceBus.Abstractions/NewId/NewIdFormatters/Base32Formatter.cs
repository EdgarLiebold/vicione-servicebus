using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ViciOne.ServiceBus.NewIdFormatters;

/// <summary>Formats base32 values.</summary>
public class Base32Formatter :
    INewIdFormatter
{
    const string LowerCaseChars = "abcdefghijklmnopqrstuvwxyz234567";
    const string UpperCaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    readonly string _chars;
    readonly bool _isUpperCase;
    readonly bool _isCustom;
    readonly bool _useVectorEncoding = true;
    readonly Vector256<byte> _lower;
    readonly Vector256<byte> _upper;
    /// <summary>Initializes a new instance.</summary>
    /// <param name="upperCase">The upper case.</param>
    public Base32Formatter(bool upperCase = false)
    {
        _chars = upperCase ? UpperCaseChars : LowerCaseChars;
        _isUpperCase = upperCase;
    }

    /// <summary>Creates a formatter with a custom 32-character alphabet.</summary>
    /// <param name="chars">The chars.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chars" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="chars" /> does not contain exactly 32 characters.</exception>
    public Base32Formatter(string chars)
    {
        ArgumentNullException.ThrowIfNull(chars);
        if (chars.Length != 32)
            throw new ArgumentException("The character string must be exactly 32 characters", nameof(chars));

        _chars = chars;
        _useVectorEncoding = BitConverter.IsLittleEndian;
        foreach (char value in chars)
        {
            if (value > byte.MaxValue)
            {
                _useVectorEncoding = false;
                break;
            }
        }

        if (_useVectorEncoding && Avx2.IsSupported)
        {
            _isCustom = true;
            var bytes = MemoryMarshal.Cast<char, byte>(chars);
            var lower = MemoryMarshal.Read<Vector256<byte>>(bytes);
            var upper = MemoryMarshal.Read<Vector256<byte>>(bytes[32..]);

            _lower = IntrinsicsHelper.GetByteLutFromChar(lower);
            _upper = IntrinsicsHelper.GetByteLutFromChar(upper);
        }
    }

    /// <summary>Formats a canonical identifier representation.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes" /> does not contain exactly 16 bytes.</exception>
    public string Format(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("Exactly 16 bytes are required.", nameof(bytes));

        Span<char> result = stackalloc char[26];
        if (_useVectorEncoding && Avx2.IsSupported)
        {
            if (_isCustom)
                IntrinsicsHelper.EncodeBase32(bytes, result, _lower, _upper);
            else
                EncodeKnown(bytes, result, _isUpperCase);

            return new string(result);
        }

        var offset = 0;
        for (var i = 0; i < 3; i++)
        {
            var indexed = i * 5;
            long number = (bytes[indexed] << 12) | (bytes[indexed + 1] << 4) | (bytes[indexed + 2] >> 4);
            ConvertLongToBase32(result, offset, number, 4, _chars);

            offset += 4;

            number = ((bytes[indexed + 2] & 0xf) << 16) | (bytes[indexed + 3] << 8) | bytes[indexed + 4];
            ConvertLongToBase32(result, offset, number, 4, _chars);

            offset += 4;
        }

        ConvertLongToBase32(result, offset, bytes[15], 2, _chars);

        return new string(result);
    }

    static void ConvertLongToBase32(Span<char> buffer, int offset, long value, int count, string chars)
    {
        for (var i = count - 1; i >= 0; i--)
        {
            var index = (int)(value % 32);
            buffer[offset + i] = chars[index];
            value /= 32;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EncodeKnown(ReadOnlySpan<byte> source, Span<char> destination, bool isUpperCase)
    {
        var lowerCaseLow = Vector256.Create((byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p', (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p');

        var lowerCaseHigh = Vector256.Create((byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7');

        var upperCaseLow = Vector256.Create((byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N', (byte)'O', (byte)'P');

        var upperCaseHigh = Vector256.Create((byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U', (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7');
        if (isUpperCase)
        {
            IntrinsicsHelper.EncodeBase32(source, destination, upperCaseLow, upperCaseHigh);
        }
        else
        {
            IntrinsicsHelper.EncodeBase32(source, destination, lowerCaseLow, lowerCaseHigh);
        }
    }
}
