using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util.Scanning;

public class TypeQuery
{
    readonly TypeClassification _classification;

    public readonly Func<Type, bool> Filter;

    public TypeQuery(TypeClassification classification, Func<Type, bool> filter = null)
    {
        Filter = filter ?? (t => true);
        _classification = classification;
    }

    public IEnumerable<Type> Find(AssemblyScanTypeInfo assembly)
    {
        return assembly.FindTypes(_classification).Where(Filter);
    }
}
