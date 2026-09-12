using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts between <see cref="Version"/> values and their text representation.</summary>
internal sealed class VersionTypeConverter :
    ITypeConverter<string, Version>,
    ITypeConverter<Version, string>,
    ITypeConverter<Version, object>
{
    /// <inheritdoc />
    public bool TryConvert(Version? input, out string? result)
    {
        result = input?.ToString();

        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out Version? result)
    {
        switch (input)
        {
            case Version version:
                result = version;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                return Version.TryParse(text, out result);

            default:
                result = default;
                return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out Version? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        return Version.TryParse(input, out result);
    }
}
