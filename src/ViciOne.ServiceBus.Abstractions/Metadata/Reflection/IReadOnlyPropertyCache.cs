using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

public interface IReadOnlyPropertyCache<T> : IEnumerable<ReadOnlyProperty<T>>
{
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value);
}
