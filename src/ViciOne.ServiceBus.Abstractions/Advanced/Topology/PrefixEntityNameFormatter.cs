namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Prepends a fixed prefix to entity names produced by another formatter.</summary>
public sealed class PrefixEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IEntityNameFormatter _entityNameFormatter;
    readonly string _prefix;

    /// <summary>Creates a prefixing formatter.</summary>
    /// <param name="entityNameFormatter">The formatter that produces the unprefixed name.</param>
    /// <param name="prefix">The prefix prepended verbatim.</param>
    public PrefixEntityNameFormatter(IEntityNameFormatter entityNameFormatter, string prefix)
    {
        _entityNameFormatter = entityNameFormatter ?? throw new ArgumentNullException(nameof(entityNameFormatter));
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _prefix = prefix;
    }

    /// <summary>Formats and prefixes an entity name.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The prefixed entity name.</returns>
    public string FormatEntityName<T>()
    {
        string name = _entityNameFormatter.FormatEntityName<T>();
        ArgumentException.ThrowIfNullOrWhiteSpace(name, "entityNameFormatter");

        return $"{_prefix}{name}";
    }
}
