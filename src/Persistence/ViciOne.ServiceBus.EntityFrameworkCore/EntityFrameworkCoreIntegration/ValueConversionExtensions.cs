using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for value conversion.
/// </summary>
public static class ValueConversionExtensions
{
    /// <summary>
    /// Determines whether the current value has json conversion.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="builder">The builder value.</param>
    /// <returns>The result of the operation.</returns>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> builder)
        where T : class?
    {
        var converter = new JsonValueConverter<T>();
        var comparer = new JsonValueComparer<T>();

        builder.HasConversion(converter);
        builder.Metadata.SetValueConverter(converter);
        builder.Metadata.SetValueComparer(comparer);

        return builder;
    }
}
