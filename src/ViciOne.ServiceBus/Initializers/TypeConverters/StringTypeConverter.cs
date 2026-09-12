using System.Globalization;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Formats runtime values as culture-independent text.</summary>
internal sealed class StringTypeConverter :
    ITypeConverter<string, object>
{
    /// <inheritdoc />
    public bool TryConvert(object? input, out string? result)
    {
        if (input != null)
        {
            result = input is IFormattable formattable
                ? formattable.ToString(format: null, CultureInfo.InvariantCulture)
                : input.ToString();
            return result != null;
        }

        result = null;
        return false;
    }
}
