using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Queries type values.</summary>
public class TypeQuery
{
    readonly TypeClassification _classification;

    /// <summary>Exposes the filter used by the containing type.</summary>
    public readonly Func<Type, bool> Filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="classification">The classification.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public TypeQuery(TypeClassification classification, Func<Type, bool>? filter = null)
    {
        Filter = filter ?? (t => true);
        _classification = classification;
    }

    /// <summary>Finds the matching value.</summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns>The matching value.</returns>
    public IEnumerable<Type> Find(AssemblyScanTypeInfo assembly)
    {
        return assembly.FindTypes(_classification).Where(Filter);
    }
}
