namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// Provides a prefix entity name formatter implementation.
/// </summary>
public class PrefixEntityNameFormatter :
    IEntityNameFormatter
{
    readonly IEntityNameFormatter _entityNameFormatter;
    readonly string _prefix;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entityNameFormatter">The entity name formatter value.</param>
    /// <param name="prefix">The prefix value.</param>
    public PrefixEntityNameFormatter(IEntityNameFormatter entityNameFormatter, string prefix)
    {
        _entityNameFormatter = entityNameFormatter;
        _prefix = prefix;
    }

    /// <summary>
    /// Performs the format entity name operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public string FormatEntityName<T>()
    {
        var name = _entityNameFormatter.FormatEntityName<T>();

        return $"{_prefix}{name}";
    }
}
