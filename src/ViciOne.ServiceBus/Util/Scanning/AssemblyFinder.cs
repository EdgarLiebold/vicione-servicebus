using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Finds assembly values.</summary>
public class AssemblyFinder
{
    /// <summary>Represents the method that handles assembly filter.</summary>
    /// <param name="filename">The filename.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public delegate bool AssemblyFilter(string filename);


    /// <summary>Represents the method that handles assembly load failure.</summary>
    /// <param name="assemblyName">The assembly name.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public delegate void AssemblyLoadFailure(string assemblyName, Exception exception);


    /// <summary>Finds assemblies.</summary>
    /// <param name="loadFailure">The load failure.</param>
    /// <param name="includeExeFiles">The include exe files.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The matching assemblies.</returns>
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

    /// <summary>Finds assemblies.</summary>
    /// <param name="assemblyPath">The assembly path.</param>
    /// <param name="loadFailure">The load failure.</param>
    /// <param name="includeExeFiles">The include exe files.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The matching assemblies.</returns>
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
                loadedAssembly = Assembly.LoadFrom(file);
            }
            catch (BadImageFormatException exception)
            {
                LogContext.Warning?.Log(exception, "Assembly Scan failed: {Name}", name);

                continue;
            }
            catch (Exception exception)
            {
                loadFailure(file, exception);
            }

            if (loadedAssembly != null)
                yield return loadedAssembly;
        }
    }
}
