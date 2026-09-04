using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Provides an assembly type list implementation.
/// </summary>
public class AssemblyTypeList
{
    /// <summary>
    /// Defines the abstract value.
    /// </summary>
    public readonly List<Type> Abstract = new List<Type>();
    /// <summary>
    /// Defines the concrete value.
    /// </summary>
    public readonly List<Type> Concrete = new List<Type>();
    /// <summary>
    /// Defines the interface value.
    /// </summary>
    public readonly List<Type> Interface = new List<Type>();

    /// <summary>
    /// Performs the select types operation.
    /// </summary>
    /// <param name="classification">The classification value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the all types operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<Type> AllTypes()
    {
        return Interface.Concat(Concrete).Concat(Abstract);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="type">The type value.</param>
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
