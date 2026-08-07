// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;


    public interface IReadOnlyPropertyCache<T> :
        IEnumerable<ReadOnlyProperty<T>>
    {
        bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value);
    }
}
