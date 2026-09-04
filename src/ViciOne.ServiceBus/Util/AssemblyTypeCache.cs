using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ViciOne.ServiceBus.Util.Scanning;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Caches the deterministic result of synchronous assembly type discovery.
/// Reflection type discovery is synchronous by contract; no ThreadPool hop or sync-over-async bridge is used.
/// </summary>
public static class AssemblyTypeCache
{
    /// <summary>
    /// Remove all cached assemblies, forcing subsequent scans to discover the current exported type set again.
    /// </summary>
    public static void Clear()
    {
        Cached.Assemblies.Clear();
    }

    /// <summary>
    /// Throws an aggregate exception when one or more cached assembly scans could not load exported types.
    /// </summary>
    public static void ThrowIfAnyTypeScanFailures()
    {
        Exception[] exceptions = FailedAssemblies()
            .Select(x => x.Record.LoadException)
            .Where(x => x is not null)
            .Cast<Exception>()
            .ToArray();

        if (exceptions.Length > 0)
            throw new AggregateException(exceptions);
    }

    public static IEnumerable<AssemblyScanTypeInfo> FailedAssemblies()
    {
        return Cached.Assemblies.Values
            .Select(x => x.Value)
            .Where(x => x.Record.LoadException is not null);
    }

    public static AssemblyScanTypeInfo ForAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Cached.Assemblies.GetOrAdd(assembly,
            static value => new Lazy<AssemblyScanTypeInfo>(() => new AssemblyScanTypeInfo(value), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    public static TypeSet FindTypes(IEnumerable<Assembly> assemblies, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        return new TypeSet(assemblies.Select(ForAssembly).ToArray(), filter);
    }

    public static IEnumerable<Type> FindTypes(IEnumerable<Assembly> assemblies, TypeClassification classification, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var query = new TypeQuery(classification, filter);
        return assemblies.SelectMany(assembly => query.Find(ForAssembly(assembly)));
    }

    public static IEnumerable<Type> FindTypes(Assembly assembly, TypeClassification classification, Func<Type, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var query = new TypeQuery(classification, filter);
        return query.Find(ForAssembly(assembly));
    }

    public static IEnumerable<Type> FindTypesInNamespace(Type type, Func<Type, bool> typeFilter, TypeClassification typeClassification)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(typeFilter);

        if (type.Namespace == null)
            throw new ArgumentException("The type must have a valid namespace", nameof(type));

        var dottedNamespace = type.Namespace + ".";

        bool Filter(Type candidate)
        {
            return typeFilter(candidate)
                && candidate.Namespace != null
                && (candidate.Namespace.StartsWith(dottedNamespace, StringComparison.OrdinalIgnoreCase)
                    || candidate.Namespace.Equals(type.Namespace, StringComparison.OrdinalIgnoreCase));
        }

        return FindTypes(type.Assembly, typeClassification, Filter);
    }


    static class Cached
    {
        internal static readonly ConcurrentDictionary<Assembly, Lazy<AssemblyScanTypeInfo>> Assemblies = new();
    }
}
