using System.Security.Cryptography;
using System.Text;
using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Topology;

internal static class EntityNameShortener
{
    internal const int HashLength = 13;
    internal const int MinimumMaximumLength = HashLength + 2;

    internal static string Shorten(string value, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumLength, MinimumMaximumLength);

        if (value.Length <= maximumLength)
            return value;

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        string hash = ZBase32Formatter.LowerCase.Format(digest.AsSpan(0, 16))[..HashLength];
        int prefixLength = maximumLength - HashLength - 1;

        if (char.IsHighSurrogate(value[prefixLength - 1]) && char.IsLowSurrogate(value[prefixLength]))
            prefixLength--;

        return $"{value[..prefixLength]}-{hash}";
    }
}
