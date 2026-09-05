using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Provides an assembly scanner implementation.
/// </summary>
public class AssemblyScanner :
    IAssemblyScanner
{
    readonly List<Assembly> _assemblies = new List<Assembly>();
    readonly CompositeFilter<string> _assemblyFilter = new CompositeFilter<string>();
    readonly CompositeFilter<Type> _filter = new CompositeFilter<Type>();

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _assemblies.Count;

    /// <summary>
    /// Gets or sets the description value.
    /// </summary>
    public string Description { get; set; } = null!;
    /// <summary>
    /// Performs the assembly operation.
    /// </summary>
    /// <param name="assembly">The assembly value.</param>
    public void Assembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }

    /// <summary>
    /// Performs the assembly operation.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    public void Assembly(string assemblyName)
    {
        var asm = System.Reflection.Assembly.Load(assemblyName);
        Assembly(asm);
    }

    /// <summary>
    /// Performs the assembly containing type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void AssemblyContainingType<T>()
    {
        AssemblyContainingType(typeof(T));
    }

    /// <summary>
    /// Performs the assembly containing type operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    public void AssemblyContainingType(Type type)
    {
        _assemblies.Add(type.Assembly);
    }

    /// <summary>
    /// Performs the exclude operation.
    /// </summary>
    /// <param name="exclude">The exclude value.</param>
    public void Exclude(Func<Type, bool> exclude)
    {
        _filter.Excludes.Add(exclude);
    }

    /// <summary>
    /// Performs the exclude namespace operation.
    /// </summary>
    /// <param name="nameSpace">The name space value.</param>
    public void ExcludeNamespace(string? nameSpace)
    {
        Exclude(type => type.IsInNamespace(nameSpace));
    }

    /// <summary>
    /// Performs the exclude namespace containing type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void ExcludeNamespaceContainingType<T>()
    {
        ExcludeNamespace(typeof(T).Namespace);
    }

    /// <summary>
    /// Performs the include operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    public void Include(Func<Type, bool> predicate)
    {
        _filter.Includes.Add(predicate);
    }

    /// <summary>
    /// Performs the include namespace operation.
    /// </summary>
    /// <param name="nameSpace">The name space value.</param>
    public void IncludeNamespace(string? nameSpace)
    {
        Include(type => type.IsInNamespace(nameSpace));
    }

    /// <summary>
    /// Performs the include namespace containing type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void IncludeNamespaceContainingType<T>()
    {
        IncludeNamespace(typeof(T).Namespace);
    }

    /// <summary>
    /// Performs the exclude type operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    public void ExcludeType<T>()
    {
        Exclude(type => type == typeof(T));
    }

    /// <summary>
    /// Performs the assemblies from application base directory operation.
    /// </summary>
    public void AssembliesFromApplicationBaseDirectory()
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(OnAssemblyLoadFailure, false, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the assemblies and executables from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    public void AssembliesAndExecutablesFromPath(string path)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, true, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the assemblies from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    public void AssembliesFromPath(string path)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, false, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the assemblies and executables from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="assemblyFilter">The assembly filter value.</param>
    public void AssembliesAndExecutablesFromPath(string path, Func<Assembly, bool> assemblyFilter)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, true, _assemblyFilter.Matches)
            .Where(assemblyFilter);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the assemblies from path operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="assemblyFilter">The assembly filter value.</param>
    public void AssembliesFromPath(string path, Func<Assembly, bool> assemblyFilter)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, false, _assemblyFilter.Matches)
            .Where(assemblyFilter);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the exclude file name starts with operation.
    /// </summary>
    /// <param name="startsWith">The starts with value.</param>
    public void ExcludeFileNameStartsWith(params string[] startsWith)
    {
        for (var i = 0; i < startsWith.Length; i++)
        {
            var value = startsWith[i];

            _assemblyFilter.Excludes.Add(name => name.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Performs the include file name starts with operation.
    /// </summary>
    /// <param name="startsWith">The starts with value.</param>
    public void IncludeFileNameStartsWith(params string[] startsWith)
    {
        for (var i = 0; i < startsWith.Length; i++)
        {
            var value = startsWith[i];

            _assemblyFilter.Includes.Add(name => name.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Performs the assemblies and executables from application base directory operation.
    /// </summary>
    public void AssembliesAndExecutablesFromApplicationBaseDirectory()
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(OnAssemblyLoadFailure, true, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>
    /// Performs the scan for types operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public TypeSet ScanForTypes()
    {
        return AssemblyTypeCache.FindTypes(_assemblies, _filter.Matches);
    }

    /// <summary>
    /// Performs the the calling assembly operation.
    /// </summary>
    public void TheCallingAssembly()
    {
        var callingAssembly = FindTheCallingAssembly();

        if (callingAssembly != null)
            Assembly(callingAssembly);
        else
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Assembly Scanner", "unknown", "Could not determine the calling assembly, you may need to explicitly call IAssemblyScanner.Assembly()", "Correct the named configuration before starting the host"));
    }

    static void OnAssemblyLoadFailure(string assemblyName, Exception exception)
    {
        Console.WriteLine("ViciOne.ServiceBus could not load assembly from " + assemblyName);
    }

    /// <summary>
    /// Performs the contains operation.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Contains(string assemblyName)
    {
        return _assemblies
            .Select(assembly => assembly.GetName())
            .Any(aName => aName.Name == assemblyName);
    }

    /// <summary>
    /// Determines whether the current value has assemblies.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasAssemblies()
    {
        return _assemblies.Any();
    }

    static Assembly? FindTheCallingAssembly()
    {
        var trace = new StackTrace(false);
        var thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
        var viciOneServiceBusAssembly = typeof(IBus).Assembly;

        Assembly? callingAssembly = null;
        for (var i = 0; i < trace.FrameCount; i++)
        {
            var frame = trace.GetFrame(i);
            var declaringType = frame?.GetMethod()?.DeclaringType;
            if (declaringType != null)
            {
                var assembly = declaringType.Assembly;
                if (assembly != thisAssembly && assembly != viciOneServiceBusAssembly)
                {
                    callingAssembly = assembly;
                    break;
                }
            }
        }

        return callingAssembly;
    }
}
