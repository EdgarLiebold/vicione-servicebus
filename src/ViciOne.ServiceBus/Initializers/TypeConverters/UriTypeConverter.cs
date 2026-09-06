using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts uri type values.</summary>
public class UriTypeConverter :
    ITypeConverter<string, Uri>,
    ITypeConverter<Uri, string>,
    ITypeConverter<Uri, object>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Uri? input, out string? result)
    {
        result = input?.ToString();

        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out Uri? result)
    {
        switch (input)
        {
            case Uri uri:
                result = uri;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                try
                {
                    result = new Uri(text);
                    return true;
                }
                catch (FormatException)
                {
                    result = default;
                    return false;
                }

            default:
                result = default;
                return false;
        }
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out Uri? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        try
        {
            result = new Uri(input);

            return true;
        }
        catch (FormatException)
        {
            result = default;
            return false;
        }
    }
}
