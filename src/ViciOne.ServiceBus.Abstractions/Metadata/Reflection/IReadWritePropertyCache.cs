using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

public interface IReadWritePropertyCache<T> : IEnumerable<ReadWriteProperty<T>>
{
    ReadWriteProperty<T> this[string name] { get; }

    bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value);

    bool TryGetProperty(string propertyName, [NotNullWhen(true)] out ReadWriteProperty<T>? property);
}
