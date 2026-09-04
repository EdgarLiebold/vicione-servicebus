using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Provides a type query implementation.
/// </summary>
public class TypeQuery
{
    readonly TypeClassification _classification;

    /// <summary>
    /// Defines the filter value.
    /// </summary>
    public readonly Func<Type, bool> Filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="classification">The classification value.</param>
    /// <param name="filter">The filter value.</param>
    public TypeQuery(TypeClassification classification, Func<Type, bool>? filter = null)
    {
        Filter = filter ?? (t => true);
        _classification = classification;
    }

    /// <summary>
    /// Performs the find operation.
    /// </summary>
    /// <param name="assembly">The assembly value.</param>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<Type> Find(AssemblyScanTypeInfo assembly)
    {
        return assembly.FindTypes(_classification).Where(Filter);
    }
}
