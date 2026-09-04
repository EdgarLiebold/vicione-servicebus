using System;
using System.Reflection;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Defines the contract for assembly scanner.
/// </summary>
public interface IAssemblyScanner
{
    /// <summary>
    /// Optional user-supplied diagnostic description of this scanning operation
    /// </summary>
    string Description { get; set; }

    /// <summary>
    /// Add an Assembly to the scanning operation
    /// </summary>
    /// <param name="assembly"></param>
    void Assembly(Assembly assembly);

    /// <summary>
    /// Add an Assembly by name to the scanning operation
    /// </summary>
    /// <param name="assemblyName"></param>
    void Assembly(string assemblyName);

    /// <summary>
    /// Add the Assembly that contains type T to the scanning operation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    void AssemblyContainingType<T>();

    /// <summary>
    /// Add the Assembly that contains type to the scanning operation
    /// </summary>
    /// <param name="type"></param>
    void AssemblyContainingType(Type type);

    /// <summary>
    /// Exclude types that match the Predicate from being scanned
    /// </summary>
    /// <param name="exclude"></param>
    void Exclude(Func<Type, bool> exclude);

    /// <summary>
    /// Exclude all types in this nameSpace or its children from the scanning operation
    /// </summary>
    /// <param name="nameSpace"></param>
    void ExcludeNamespace(string nameSpace);

    /// <summary>
    /// Exclude all types in this nameSpace or its children from the scanning operation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    void ExcludeNamespaceContainingType<T>();

    /// <summary>
    /// Only include types matching the Predicate in the scanning operation. You can
    /// use multiple Include() calls in a single scanning operation
    /// </summary>
    /// <param name="predicate"></param>
    void Include(Func<Type, bool> predicate);

    /// <summary>
    /// Only include types from this nameSpace or its children in the scanning operation.  You can
    /// use multiple Include() calls in a single scanning operation
    /// </summary>
    /// <param name="nameSpace"></param>
    void IncludeNamespace(string nameSpace);

    /// <summary>
    /// Only include types from this nameSpace or its children in the scanning operation.  You can
    /// use multiple Include() calls in a single scanning operation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    void IncludeNamespaceContainingType<T>();

    /// <summary>
    /// Exclude this specific type from the scanning operation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    void ExcludeType<T>();

    /// <summary>
    /// Performs the the calling assembly operation.
    /// </summary>
    void TheCallingAssembly();

    /// <summary>
    /// Performs the assemblies from application base directory operation.
    /// </summary>
    void AssembliesFromApplicationBaseDirectory();

    /// <summary>
    /// Performs the assemblies and executables from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    void AssembliesAndExecutablesFromPath(string path);
    /// <summary>
    /// Performs the assemblies from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    void AssembliesFromPath(string path);

    /// <summary>
    /// Performs the assemblies and executables from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="assemblyFilter">The assembly filter value.</param>
    void AssembliesAndExecutablesFromPath(string path, Func<Assembly, bool> assemblyFilter);

    /// <summary>
    /// Performs the assemblies from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="assemblyFilter">The assembly filter value.</param>
    void AssembliesFromPath(string path, Func<Assembly, bool> assemblyFilter);
    /// <summary>
    /// Performs the exclude file name starts with operation.
    /// </summary>
    /// <param name="startsWith">The starts with value.</param>
    void ExcludeFileNameStartsWith(params string[] startsWith);
    /// <summary>
    /// Performs the include file name starts with operation.
    /// </summary>
    /// <param name="startsWith">The starts with value.</param>
    void IncludeFileNameStartsWith(params string[] startsWith);
    /// <summary>
    /// Performs the assemblies and executables from application base directory operation.
    /// </summary>
    void AssembliesAndExecutablesFromApplicationBaseDirectory();
    /// <summary>
    /// Performs the scan for types operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    TypeSet ScanForTypes();
}
