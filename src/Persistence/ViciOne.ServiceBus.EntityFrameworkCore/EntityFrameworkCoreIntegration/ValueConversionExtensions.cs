using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Configures JSON conversion and structural change tracking for reference-type properties.</summary>
public static class ValueConversionExtensions
{
    /// <summary>Stores a property as JSON and installs a comparer that snapshots and compares serialized values.</summary>
    /// <typeparam name="T">The reference type stored in the property.</typeparam>
    /// <param name="builder">The property builder to configure.</param>
    /// <returns>The same property builder.</returns>
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
