using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Provides an assembly finder implementation.
/// </summary>
public class AssemblyFinder
{
    /// <summary>
    /// Represents the method that handles assembly filter.
    /// </summary>
    /// <param name="filename">The filename value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public delegate bool AssemblyFilter(string filename);


    /// <summary>
    /// Represents the method that handles assembly load failure.
    /// </summary>
    /// <param name="assemblyName">The assembly name value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public delegate void AssemblyLoadFailure(string assemblyName, Exception exception);


    /// <summary>
    /// Performs the find assemblies operation.
    /// </summary>
    /// <param name="loadFailure">The load failure value.</param>
    /// <param name="includeExeFiles">The include exe files value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    public static IEnumerable<Assembly> FindAssemblies(AssemblyLoadFailure loadFailure, bool includeExeFiles, AssemblyFilter filter)
    {
        var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
        var binPath = string.Empty;

        if (string.IsNullOrEmpty(binPath))
            return FindAssemblies(assemblyPath, loadFailure, includeExeFiles, filter);

        if (Path.IsPathRooted(binPath))
            return FindAssemblies(binPath, loadFailure, includeExeFiles, filter);

        var binPaths = binPath.Split(';');
        return binPaths.SelectMany(bin =>
        {
            var path = Path.Combine(assemblyPath, bin);
            return FindAssemblies(path, loadFailure, includeExeFiles, filter);
        });
    }

    /// <summary>
    /// Performs the find assemblies operation.
    /// </summary>
    /// <param name="assemblyPath">The assembly path value.</param>
    /// <param name="loadFailure">The load failure value.</param>
    /// <param name="includeExeFiles">The include exe files value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    public static IEnumerable<Assembly> FindAssemblies(string assemblyPath, AssemblyLoadFailure loadFailure, bool includeExeFiles, AssemblyFilter filter)
    {
        LogContext.Debug?.Log("Scanning assembly directory: {Path}", assemblyPath);

        IEnumerable<string> dllFiles = Directory.EnumerateFiles(assemblyPath, "*.dll", SearchOption.AllDirectories).ToList();
        IEnumerable<string> files = dllFiles;

        if (includeExeFiles)
        {
            IEnumerable<string> exeFiles = Directory.EnumerateFiles(assemblyPath, "*.exe", SearchOption.AllDirectories).ToList();
            files = dllFiles.Concat(exeFiles);
        }

        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var filterName = Path.GetFileName(file);
            if (!filter(filterName))
            {
                LogContext.Debug?.Log("Filtered assembly: {File}", file);

                continue;
            }

            Assembly? loadedAssembly = null;
            try
            {
                loadedAssembly = Assembly.Load(name);
            }
            catch (BadImageFormatException exception)
            {
                LogContext.Warning?.Log(exception, "Assembly Scan failed: {Name}", name);

                continue;
            }
            catch (Exception originalException)
            {
                try
                {
                    loadedAssembly = Assembly.Load(file);
                }
                catch (Exception)
                {
                    loadFailure(file, originalException);
                }
            }

            if (loadedAssembly != null)
                yield return loadedAssembly;
        }
    }
}
