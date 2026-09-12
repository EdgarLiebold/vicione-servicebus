using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts between relative-or-absolute <see cref="Uri"/> values and text.</summary>
internal sealed class UriTypeConverter :
    ITypeConverter<string, Uri>,
    ITypeConverter<Uri, string>,
    ITypeConverter<Uri, object>
{
    /// <inheritdoc />
    public bool TryConvert(Uri? input, out string? result)
    {
        result = input?.ToString();

        return true;
    }

    /// <inheritdoc />
    public bool TryConvert(object? input, out Uri? result)
    {
        switch (input)
        {
            case Uri uri:
                result = uri;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                return Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out result);

            default:
                result = default;
                return false;
        }
    }

    /// <inheritdoc />
    public bool TryConvert(string? input, out Uri? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        return Uri.TryCreate(input, UriKind.RelativeOrAbsolute, out result);
    }
}
