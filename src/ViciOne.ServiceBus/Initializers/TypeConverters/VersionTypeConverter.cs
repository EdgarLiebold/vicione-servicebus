using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Provides a version type converter implementation.
/// </summary>
public class VersionTypeConverter :
    ITypeConverter<string, Version>,
    ITypeConverter<Version, string>,
    ITypeConverter<Version, object>
{
    /// <summary>
    /// Performs the try convert operation.
    /// </summary>
    /// <param name="input">The input value.</param>
    /// <param name="result">The result value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Version? input, out string? result)
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
    public bool TryConvert(object? input, out Version? result)
    {
        switch (input)
        {
            case Version version:
                result = version;
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                try
                {
                    result = new Version(text);
                    return true;
                }
                catch (Exception)
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
    public bool TryConvert(string? input, out Version? result)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            result = null;
            return true;
        }

        try
        {
            result = new Version(input);

            return true;
        }
        catch (Exception)
        {
            result = default;
            return false;
        }
    }
}
