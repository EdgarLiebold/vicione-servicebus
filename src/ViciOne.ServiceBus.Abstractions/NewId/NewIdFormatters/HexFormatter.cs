using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ViciOne.ServiceBus.NewIdFormatters;

/// <summary>Formats hex values.</summary>
public class HexFormatter :
    INewIdFormatter
{
    readonly uint _alpha;
    const uint LowerCaseUInt = 0x2020U;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="upperCase">The upper case.</param>
    public HexFormatter(bool upperCase = false)
    {
        _alpha = upperCase ? 0 : LowerCaseUInt;
    }

    /// <summary>Formats a canonical identifier representation.</summary>
    /// <param name="bytes">The bytes.</param>
    /// <returns>The formatted value.</returns>
    /// <exception cref="ArgumentException"><paramref name="bytes" /> does not contain exactly 16 bytes.</exception>
    public string Format(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 16)
            throw new ArgumentException("Exactly 16 bytes are required.", nameof(bytes));

        Span<char> result = stackalloc char[32];
        if (Avx2.IsSupported && BitConverter.IsLittleEndian)
        {
            var isUpperCase = _alpha != LowerCaseUInt;
            var inputVec = MemoryMarshal.Read<Vector128<byte>>(bytes);
            var hexVec = IntrinsicsHelper.EncodeBytesHex(inputVec, isUpperCase);
            var byteSpan = MemoryMarshal.Cast<char, byte>(result);
            IntrinsicsHelper.Vector256ToCharUtf16(hexVec, byteSpan);
            return new string(result);
        }

        for (int pos = 0; pos < bytes.Length; pos++)
        {
            HexToChar(bytes[pos], result, pos * 2, _alpha);
        }

        return new string(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void HexToChar(byte value, Span<char> buffer, int startingIndex, uint casing)
    {
        uint difference = (((uint)value & 0xF0U) << 4) + ((uint)value & 0x0FU) - 0x8989U;
        uint packedResult = ((((uint)(-(int)difference) & 0x7070U) >> 4) + difference + 0xB9B9U) | (uint)casing;

        buffer[startingIndex + 1] = (char)(packedResult & 0xFF);
        buffer[startingIndex] = (char)(packedResult >> 8);
    }
}
