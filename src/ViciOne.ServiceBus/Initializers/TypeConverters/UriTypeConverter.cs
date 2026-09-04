using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides an uri type converter implementation.
/// </summary>
public class UriTypeConverter :
    ITypeConverter<string, Uri>,
    ITypeConverter<Uri, string>,
    ITypeConverter<Uri, object>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Uri? input, out string? result)
    {
        result = input?.ToString();

        return true;
    }

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
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

    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
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
