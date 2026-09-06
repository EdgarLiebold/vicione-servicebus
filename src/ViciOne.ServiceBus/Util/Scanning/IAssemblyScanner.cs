using System;
using System.Reflection;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Defines the operations required by assembly scanner.</summary>
public interface IAssemblyScanner
{
    /// <summary>Optional user-supplied diagnostic description of this scanning operation.</summary>
    string Description { get; set; }

    /// <summary>Add an Assembly to the scanning operation.</summary>
    /// <param name="assembly">The assembly.</param>
    void Assembly(Assembly assembly);

    /// <summary>Add an Assembly by name to the scanning operation.</summary>
    /// <param name="assemblyName">The assembly name.</param>
    void Assembly(string assemblyName);

    /// <summary>Add the Assembly that contains type T to the scanning operation.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void AssemblyContainingType<T>();

    /// <summary>Add the Assembly that contains type to the scanning operation.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    void AssemblyContainingType(Type type);

    /// <summary>Exclude types that match the Predicate from being scanned.</summary>
    /// <param name="exclude">The exclude.</param>
    void Exclude(Func<Type, bool> exclude);

    /// <summary>Exclude all types in this nameSpace or its children from the scanning operation.</summary>
    /// <param name="nameSpace">The name space.</param>
    void ExcludeNamespace(string nameSpace);

    /// <summary>Exclude all types in this nameSpace or its children from the scanning operation.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void ExcludeNamespaceContainingType<T>();

    /// <summary>
    /// Only include types matching the Predicate in the scanning operation. You can
    /// use multiple Include() calls in a single scanning operation.
    /// </summary>
    /// <param name="predicate">The predicate used to select matching values.</param>
    void Include(Func<Type, bool> predicate);

    /// <summary>
    /// Only include types from this nameSpace or its children in the scanning operation.  You can
    /// use multiple Include() calls in a single scanning operation.
    /// </summary>
    /// <param name="nameSpace">The name space.</param>
    void IncludeNamespace(string nameSpace);

    /// <summary>
    /// Only include types from this nameSpace or its children in the scanning operation.  You can
    /// use multiple Include() calls in a single scanning operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    void IncludeNamespaceContainingType<T>();

    /// <summary>Exclude this specific type from the scanning operation.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    void ExcludeType<T>();

    /// <summary>Adds the calling assembly to the scan.</summary>
    void TheCallingAssembly();

    /// <summary>Adds assemblies from the application base directory.</summary>
    void AssembliesFromApplicationBaseDirectory();

    /// <summary>Adds assemblies and executables from the specified path.</summary>
    /// <param name="path">The path.</param>
    void AssembliesAndExecutablesFromPath(string path);
    /// <summary>Adds assemblies from the specified path.</summary>
    /// <param name="path">The path.</param>
    void AssembliesFromPath(string path);

    /// <summary>Adds assemblies and executables from the specified path.</summary>
    /// <param name="path">The path.</param>
    /// <param name="assemblyFilter">The assembly filter.</param>
    void AssembliesAndExecutablesFromPath(string path, Func<Assembly, bool> assemblyFilter);

    /// <summary>Adds assemblies from the specified path.</summary>
    /// <param name="path">The path.</param>
    /// <param name="assemblyFilter">The assembly filter.</param>
    void AssembliesFromPath(string path, Func<Assembly, bool> assemblyFilter);
    /// <summary>Excludes file name starts with.</summary>
    /// <param name="startsWith">The starts with.</param>
    void ExcludeFileNameStartsWith(params string[] startsWith);
    /// <summary>Includes file name starts with.</summary>
    /// <param name="startsWith">The starts with.</param>
    void IncludeFileNameStartsWith(params string[] startsWith);
    /// <summary>Adds assemblies and executables from the application base directory.</summary>
    void AssembliesAndExecutablesFromApplicationBaseDirectory();
    /// <summary>Scans for types.</summary>
    /// <returns>The type set produced by the operation.</returns>
    TypeSet ScanForTypes();
}
