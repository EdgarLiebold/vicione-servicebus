namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Formats prefix entity name values.</summary>
public class PrefixEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IEntityNameFormatter _entityNameFormatter;
    readonly string _prefix;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="entityNameFormatter">The entity name formatter.</param>
    /// <param name="prefix">The prefix.</param>
    public PrefixEntityNameFormatter(IEntityNameFormatter entityNameFormatter, string prefix)
    {
        _entityNameFormatter = entityNameFormatter;
        _prefix = prefix;
    }

    /// <summary>Formats entity name.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The formatted entity name.</returns>
    public string FormatEntityName<T>()
    {
        var name = _entityNameFormatter.FormatEntityName<T>();

        return $"{_prefix}{name}";
    }
}
