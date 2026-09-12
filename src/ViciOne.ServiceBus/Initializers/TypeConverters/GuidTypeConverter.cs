using System;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Converts GUID values from text, objects, and time-sortable identifiers.</summary>
internal sealed class GuidTypeConverter :
    ITypeConverter<string, Guid>,
    ITypeConverter<Guid, string>,
    ITypeConverter<Guid, NewId>,
    ITypeConverter<Guid, object>
{
    /// <inheritdoc />
    public bool TryConvert(NewId input, out Guid result)
    {
        result = input.ToGuid();

        return true;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public bool TryConvert(string? input, out Guid result)
    {
        return Guid.TryParse(input, out result);
    }

    /// <inheritdoc />
    public bool TryConvert(Guid input, out string result)
    {
        result = input.ToString("D");
        return true;
    }
}
