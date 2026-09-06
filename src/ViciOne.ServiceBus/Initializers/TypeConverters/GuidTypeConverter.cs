using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts guid type values.</summary>
public class GuidTypeConverter :
    ITypeConverter<string, Guid>,
    ITypeConverter<Guid, string>,
    ITypeConverter<Guid, NewId>,
    ITypeConverter<Guid, object>
{
    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(NewId input, out Guid result)
    {
        result = input.ToGuid();

        return true;
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(object? input, out Guid result)
    {
        switch (input)
        {
            case Guid guid:
                result = guid;
                return true;

            case NewId newId:
                result = newId.ToGuid();
                return true;

            case string text when !string.IsNullOrWhiteSpace(text):
                return TryConvert(text, out result);

            default:
                result = default;
                return false;
        }
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(string? input, out Guid result)
    {
        return Guid.TryParse(input, out result);
    }

    /// <summary>Attempts to convert the supplied value.</summary>
    /// <param name="input">The input.</param>
    /// <param name="result">Receives the result produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryConvert(Guid input, out string result)
    {
        result = input.ToString("D");
        return true;
    }
}
