using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Stores a list of assembly type values.</summary>
public class AssemblyTypeList
{
    /// <summary>Exposes the abstract used by the containing type.</summary>
    public readonly List<Type> Abstract = new List<Type>();
    /// <summary>Exposes the concrete used by the containing type.</summary>
    public readonly List<Type> Concrete = new List<Type>();
    /// <summary>Exposes the interface used by the containing type.</summary>
    public readonly List<Type> Interface = new List<Type>();

    /// <summary>Selects types.</summary>
    /// <param name="classification">The classification.</param>
    /// <returns>The selected types.</returns>
    public IEnumerable<IList<Type>> SelectTypes(TypeClassification classification)
    {
        var interfaces = classification.HasFlag(TypeClassification.Interface);
        var concretes = classification.HasFlag(TypeClassification.Concrete);
        var abstracts = classification.HasFlag(TypeClassification.Abstract);

        if (interfaces || concretes || abstracts)
        {
            if (interfaces)
                yield return Interface;
            if (abstracts)
                yield return Abstract;
            if (concretes)
                yield return Concrete;
        }
        else
        {
            yield return Interface;
            yield return Abstract;
            yield return Concrete;
        }
    }

    /// <summary>Returns every discovered type.</summary>
    /// <returns>The enumerable produced by the operation.</returns>
    public IEnumerable<Type> AllTypes()
    {
        return Interface.Concat(Concrete).Concat(Abstract);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    public void Add(Type type)
    {
        if (type.IsInterface)
            Interface.Add(type);
        else if (type.IsAbstract)
            Abstract.Add(type);
        else if (type.IsClass)
            Concrete.Add(type);
    }
}
