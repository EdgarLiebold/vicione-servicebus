using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Scans assembly metadata.</summary>
public class AssemblyScanner :
    IAssemblyScanner
{
    readonly List<Assembly> _assemblies = new List<Assembly>();
    readonly CompositeFilter<string> _assemblyFilter = new CompositeFilter<string>();
    readonly CompositeFilter<Type> _filter = new CompositeFilter<Type>();

    /// <summary>Gets the count.</summary>
    public int Count => _assemblies.Count;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = null!;
    /// <summary>Adds the supplied assembly to the scan.</summary>
    /// <param name="assembly">The assembly.</param>
    public void Assembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);
    }

    /// <summary>Adds the supplied assembly to the scan.</summary>
    /// <param name="assemblyName">The assembly name.</param>
    public void Assembly(string assemblyName)
    {
        var asm = System.Reflection.Assembly.Load(assemblyName);
        Assembly(asm);
    }

    /// <summary>Adds the assembly containing the specified type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void AssemblyContainingType<T>()
    {
        AssemblyContainingType(typeof(T));
    }

    /// <summary>Adds the assembly containing the specified type.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    public void AssemblyContainingType(Type type)
    {
        _assemblies.Add(type.Assembly);
    }

    /// <summary>Excludes the selected value.</summary>
    /// <param name="exclude">The exclude.</param>
    public void Exclude(Func<Type, bool> exclude)
    {
        _filter.Excludes.Add(exclude);
    }

    /// <summary>Excludes namespace.</summary>
    /// <param name="nameSpace">The name space.</param>
    public void ExcludeNamespace(string? nameSpace)
    {
        Exclude(type => type.IsInNamespace(nameSpace));
    }

    /// <summary>Excludes namespace containing type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void ExcludeNamespaceContainingType<T>()
    {
        ExcludeNamespace(typeof(T).Namespace);
    }

    /// <summary>Includes the selected value.</summary>
    /// <param name="predicate">The predicate used to select matching values.</param>
    public void Include(Func<Type, bool> predicate)
    {
        _filter.Includes.Add(predicate);
    }

    /// <summary>Includes namespace.</summary>
    /// <param name="nameSpace">The name space.</param>
    public void IncludeNamespace(string? nameSpace)
    {
        Include(type => type.IsInNamespace(nameSpace));
    }

    /// <summary>Includes namespace containing type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void IncludeNamespaceContainingType<T>()
    {
        IncludeNamespace(typeof(T).Namespace);
    }

    /// <summary>Excludes type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    public void ExcludeType<T>()
    {
        Exclude(type => type == typeof(T));
    }

    /// <summary>Adds assemblies from the application base directory.</summary>
    public void AssembliesFromApplicationBaseDirectory()
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(OnAssemblyLoadFailure, false, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Adds assemblies and executables from the specified path.</summary>
    /// <param name="path">The path.</param>
    public void AssembliesAndExecutablesFromPath(string path)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, true, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Adds assemblies from the specified path.</summary>
    /// <param name="path">The path.</param>
    public void AssembliesFromPath(string path)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, false, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Adds assemblies and executables from the specified path.</summary>
    /// <param name="path">The path.</param>
    /// <param name="assemblyFilter">The assembly filter.</param>
    public void AssembliesAndExecutablesFromPath(string path, Func<Assembly, bool> assemblyFilter)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, true, _assemblyFilter.Matches)
            .Where(assemblyFilter);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Adds assemblies from the specified path.</summary>
    /// <param name="path">The path.</param>
    /// <param name="assemblyFilter">The assembly filter.</param>
    public void AssembliesFromPath(string path, Func<Assembly, bool> assemblyFilter)
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(path, OnAssemblyLoadFailure, false, _assemblyFilter.Matches)
            .Where(assemblyFilter);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Excludes file name starts with.</summary>
    /// <param name="startsWith">The starts with.</param>
    public void ExcludeFileNameStartsWith(params string[] startsWith)
    {
        for (var i = 0; i < startsWith.Length; i++)
        {
            var value = startsWith[i];

            _assemblyFilter.Excludes.Add(name => name.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Includes file name starts with.</summary>
    /// <param name="startsWith">The starts with.</param>
    public void IncludeFileNameStartsWith(params string[] startsWith)
    {
        for (var i = 0; i < startsWith.Length; i++)
        {
            var value = startsWith[i];

            _assemblyFilter.Includes.Add(name => name.StartsWith(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>Adds assemblies and executables from the application base directory.</summary>
    public void AssembliesAndExecutablesFromApplicationBaseDirectory()
    {
        IEnumerable<Assembly> assemblies = AssemblyFinder.FindAssemblies(OnAssemblyLoadFailure, true, _assemblyFilter.Matches);

        foreach (var assembly in assemblies)
            Assembly(assembly);
    }

    /// <summary>Scans for types.</summary>
    /// <returns>The type set produced by the operation.</returns>
    public TypeSet ScanForTypes()
    {
        return AssemblyTypeCache.FindTypes(_assemblies, _filter.Matches);
    }

    /// <summary>Adds the calling assembly to the scan.</summary>
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

    /// <summary>Determines whether the current collection contains the supplied value.</summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Contains(string assemblyName)
    {
        return _assemblies
            .Select(assembly => assembly.GetName())
            .Any(aName => aName.Name == assemblyName);
    }

    /// <summary>Determines whether the current value has assemblies.</summary>
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
