using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util.Scanning;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class AssemblyFinderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-BASE", "application-base-contains-own-assembly")]
    public void FindAssemblies_ResolvesAFilteredAssemblyFromTheApplicationBaseDirectory()
    {
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        string fileName = Path.GetFileName(expected.Location);
        var failures = new List<(string File, Exception Error)>();

        Assembly[] found = AssemblyFinder.FindAssemblies(
            (name, error) => failures.Add((name, error)),
            includeExeFiles: false,
            name => name == fileName).ToArray();

        Assert.Empty(failures);
        Assert.Contains(found, assembly => assembly.FullName == expected.FullName);
        Assert.All(found, assembly => Assert.Equal(expected.FullName, assembly.FullName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-PATH", "renamed-assembly-manifest-identity")]
    public void FindAssemblies_UsesTheManifestIdentityOfARenamedAssemblyFile()
    {
        using var directory = new TemporaryAssemblyDirectory();
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        string file = directory.Copy(expected, "renamed-scanned-assembly.dll");
        var failures = new List<(string File, Exception Error)>();

        Assembly[] found = AssemblyFinder.FindAssemblies(
            directory.FullName,
            (name, error) => failures.Add((name, error)),
            includeExeFiles: false,
            name => name == Path.GetFileName(file)).ToArray();

        Assert.Empty(failures);
        Assert.Single(found);
        Assert.Equal(expected.FullName, found[0].FullName);
        Assert.NotNull(found[0].GetType(typeof(AssemblyFinderTests).FullName!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-IDENTITY", "filename-collision-uses-file-manifest-identity")]
    public void FindAssemblies_DoesNotSubstituteAnAlreadyLoadedAssemblyNamedLikeTheFile()
    {
        using var directory = new TemporaryAssemblyDirectory();
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        string file = directory.Copy(expected, "ViciOne.ServiceBus.dll");
        var failures = new List<(string File, Exception Error)>();

        Assembly[] found = AssemblyFinder.FindAssemblies(
            directory.FullName,
            (name, error) => failures.Add((name, error)),
            includeExeFiles: false,
            name => name == Path.GetFileName(file)).ToArray();

        Assert.Empty(failures);
        Assert.Single(found);
        Assert.Same(expected, found[0]);
        Assert.NotSame(typeof(AssemblyFinder).Assembly, found[0]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-FILTER", "recursive-file-name-filter")]
    public void FindAssemblies_AppliesTheFileNameFilterRecursively()
    {
        using var directory = new TemporaryAssemblyDirectory();
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        directory.Copy(expected, "selected.dll");
        directory.Copy(expected, Path.Combine("nested", "ignored.dll"));
        var inspectedNames = new List<string>();

        Assembly[] found = AssemblyFinder.FindAssemblies(
            directory.FullName,
            (_, error) => Assert.Fail($"Valid assembly was rejected: {error}"),
            includeExeFiles: false,
            name =>
            {
                inspectedNames.Add(name);
                return name == "selected.dll";
            }).ToArray();

        Assert.Single(found);
        Assert.Equal(expected.FullName, found[0].FullName);
        Assert.Contains("selected.dll", inspectedNames);
        Assert.Contains("ignored.dll", inspectedNames);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-EXECUTABLE", "exe-files-only-on-request")]
    public void FindAssemblies_IncludesExecutableFilesOnlyWhenRequested()
    {
        using var directory = new TemporaryAssemblyDirectory();
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        directory.Copy(expected, "selected.dll");
        directory.Copy(expected, Path.Combine("nested", "selected-program.exe"));
        var failures = new List<(string File, Exception Error)>();

        Assembly[] dllOnly = AssemblyFinder.FindAssemblies(
            directory.FullName, (name, error) => failures.Add((name, error)),
            includeExeFiles: false, name => name == "selected-program.exe").ToArray();
        Assembly[] withExecutable = AssemblyFinder.FindAssemblies(
            directory.FullName, (name, error) => failures.Add((name, error)),
            includeExeFiles: true, name => name == "selected-program.exe").ToArray();

        Assert.Empty(failures);
        Assert.Empty(dllOnly);
        Assert.Single(withExecutable);
        Assert.Same(expected, withExecutable[0]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-FAILURE", "disappearing-file-reports-actual-path-failure")]
    public void FindAssemblies_ReportsTheActualPathFailureWhenASelectedFileDisappears()
    {
        using var directory = new TemporaryAssemblyDirectory();
        string file = directory.Copy(typeof(AssemblyFinderTests).Assembly, "disappearing-scanned-assembly.dll");
        var failures = new List<(string File, Exception Error)>();
        bool selected = false;

        Assembly[] found = AssemblyFinder.FindAssemblies(
            directory.FullName,
            (name, error) => failures.Add((name, error)),
            includeExeFiles: false,
            _ =>
            {
                selected = true;
                File.Delete(file);
                return true;
            }).ToArray();

        Assert.True(selected);
        Assert.Empty(found);
        var failure = Assert.Single(failures);
        Assert.Equal(file, failure.File);
        var fileError = Assert.IsType<FileNotFoundException>(failure.Error);
        Assert.Equal(file, fileError.FileName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASSEMBLY-SCAN-IMAGE", "invalid-image-skipped-without-substituting-named-assembly")]
    public void FindAssemblies_SkipsAnInvalidImageAndStillReturnsTheOtherSelectedAssembly()
    {
        using var directory = new TemporaryAssemblyDirectory();
        Assembly expected = typeof(AssemblyFinderTests).Assembly;
        File.WriteAllText(Path.Combine(directory.FullName, "ViciOne.ServiceBus.dll"), "not a managed assembly");
        directory.Copy(expected, "valid-scanned-assembly.dll");
        var failures = new List<(string File, Exception Error)>();

        Assembly[] found = AssemblyFinder.FindAssemblies(
            directory.FullName,
            (name, error) => failures.Add((name, error)),
            includeExeFiles: false,
            _ => true).ToArray();

        Assert.Empty(failures);
        Assert.Single(found);
        Assert.Same(expected, found[0]);
    }

    sealed class TemporaryAssemblyDirectory : IDisposable
    {
        readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("vsb-assembly-scan-");

        public string FullName => _directory.FullName;

        public string Copy(Assembly assembly, string relativeName)
        {
            string file = Path.Combine(FullName, relativeName);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.Copy(assembly.Location, file);
            return file;
        }

        public void Dispose() => _directory.Delete(recursive: true);
    }
}
