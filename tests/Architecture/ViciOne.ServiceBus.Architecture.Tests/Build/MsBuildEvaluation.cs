using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ViciOne.ServiceBus.Architecture.Tests.Repository;

namespace ViciOne.ServiceBus.Architecture.Tests.Build;

/// <summary>
/// Evaluates a real project with MSBuild and exposes the result.
/// </summary>
/// <remarks>
/// This asks MSBuild what a project actually evaluates to, rather than reading the XML and
/// believing it. The difference matters: imports, the SDK, Directory.Build files, central package
/// management and a GlobalPackageReference all change the answer, and none of them are visible in
/// the project file itself.
/// <para>
/// Ordinary reads use only <c>-getProperty</c> and <c>-getItem</c>, so evaluation runs but no target
/// executes. The one explicit exception is <c>AddImplicitDefineConstants</c>: the SDK composes
/// framework symbols such as <c>NET10_0</c> in that property-only target, and source analysis must
/// see the same conditional-compilation branches as the compiler. Neither path writes into the
/// checkout. Results are cached per project, explicit configuration and target because evaluation
/// costs seconds, several rules revisit the same projects, and source-layout validation traverses
/// the complete native test-project set.
/// </para>
/// </remarks>
internal static class MsBuildEvaluation
{
    private static readonly Dictionary<EvaluationKey, JsonDocument> Cache = [];
    private static readonly Lock Gate = new();
    private static readonly TimeSpan Budget = TimeSpan.FromMinutes(3);

    private const string Properties =
        "Configuration;TargetPath;AssemblyName;TargetFramework;RootNamespace;IsTestProject;IsPackable;IsTestingPlatformApplication;UseMicrosoftTestingPlatformRunner;OutputType;DebugType;_DebugSymbolsProduced;LangVersion;DefineConstants;ArtifactsPath;ArtifactsProjectName;MSBuildProjectExtensionsPath;ViciOneProjectIdentity;ViciOneNativeTestTree;ViciOnePackageConsumer;UserSecretsId";

    private const string Items =
        "Compile;Using;PackageReference;ProjectReference;Content;ViciOneForbiddenNativeTestPackage";

    /// <summary>Evaluates a project once and returns the parsed MSBuild output.</summary>
    internal static JsonElement Evaluate(string projectPath, string? configuration = null)
    {
        return EvaluateCore(projectPath, configuration, target: null);
    }

    private static JsonElement EvaluateCore(
        string projectPath,
        string? configuration,
        string? target)
    {
        var key = new EvaluationKey(projectPath, configuration, target);

        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached))
            {
                return cached.RootElement;
            }

            var document = JsonDocument.Parse(Run(projectPath, configuration, target));
            Cache[key] = document;
            return document.RootElement;
        }
    }

    /// <summary>Reads one evaluated property value.</summary>
    internal static string PropertyOf(string projectPath, string name) =>
        Evaluate(projectPath).GetProperty("Properties").GetProperty(name).GetString() ?? string.Empty;

    /// <summary>Reads one evaluated property value for an explicit build configuration.</summary>
    internal static string PropertyOf(string projectPath, string name, string configuration) =>
        Evaluate(projectPath, configuration).GetProperty("Properties").GetProperty(name).GetString() ?? string.Empty;

    /// <summary>Reads the compiler's SDK-composed framework and user preprocessor symbols.</summary>
    internal static string ImplicitDefineConstantsOf(string projectPath, string configuration) =>
        EvaluateCore(projectPath, configuration, "AddImplicitDefineConstants")
            .GetProperty("Properties")
            .GetProperty("DefineConstants")
            .GetString() ?? string.Empty;

    /// <summary>Reads the <c>Identity</c> of every item of one type.</summary>
    internal static IReadOnlyList<string> ItemIdentities(string projectPath, string itemType) =>
        ItemMetadata(projectPath, itemType, "Identity");

    /// <summary>Reads item identities for an explicit build configuration.</summary>
    internal static IReadOnlyList<string> ItemIdentities(
        string projectPath,
        string itemType,
        string configuration) =>
        ItemMetadata(projectPath, itemType, "Identity", configuration);

    /// <summary>Reads one metadata value from every item of one type.</summary>
    internal static IReadOnlyList<string> ItemMetadata(
        string projectPath,
        string itemType,
        string metadata,
        string? configuration = null)
    {
        var items = Evaluate(projectPath, configuration).GetProperty("Items");

        if (!items.TryGetProperty(itemType, out var ofType))
        {
            return [];
        }

        return ofType.EnumerateArray()
            .Select(item => item.TryGetProperty(metadata, out var value) ? value.GetString() ?? string.Empty : string.Empty)
            .Where(value => value.Length > 0)
            .ToArray();
    }

    private static string Run(string projectPath, string? configuration, string? target)
    {
        var start = new ProcessStartInfo(DotNetHost.Path)
        {
            WorkingDirectory = RepositoryLayout.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        start.ArgumentList.Add("msbuild");
        start.ArgumentList.Add(projectPath);
        start.ArgumentList.Add("-nologo");

        if (!string.IsNullOrWhiteSpace(configuration))
        {
            start.ArgumentList.Add($"-property:Configuration={configuration}");
        }

        if (!string.IsNullOrWhiteSpace(target))
        {
            start.ArgumentList.Add($"-target:{target}");
        }

        start.ArgumentList.Add($"-getProperty:{Properties}");
        start.ArgumentList.Add($"-getItem:{Items}");

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start {DotNetHost.Path}.");

        // Both streams are drained concurrently and the wait is what the deadline applies to.
        // Reading one stream to the end before waiting would deadlock as soon as the other stream
        // filled its pipe buffer, and the deadline would never be reached because the process would
        // be blocked writing rather than running.
        using var deadline = new CancellationTokenSource(Budget);

        var standardOutput = process.StandardOutput.ReadToEndAsync(deadline.Token);
        var standardError = process.StandardError.ReadToEndAsync(deadline.Token);

        try
        {
            process.WaitForExitAsync(deadline.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            KillQuietly(process);
            throw new InvalidOperationException(
                $"Evaluating {projectPath} exceeded {Budget.TotalSeconds:0} seconds and was aborted.");
        }

        var output = Drain(standardOutput);
        var errors = Drain(standardError);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Evaluating {projectPath} failed with exit code {process.ExitCode}.{Environment.NewLine}{output}{errors}");
        }

        return output;
    }

    private readonly record struct EvaluationKey(
        string ProjectPath,
        string? Configuration,
        string? Target);

    private static string Drain(Task<string> stream)
    {
        try
        {
            return stream.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(milliseconds: 10_000);
        }
        catch (InvalidOperationException)
        {
            // Already gone between the deadline and the kill.
        }
    }
}

/// <summary>
/// Resolves the .NET host that is actually running this test.
/// </summary>
/// <remarks>
/// Spawning a bare <c>dotnet</c> from PATH would introduce a second SDK truth: the answers would
/// come from whichever SDK the machine happens to prefer rather than from the one
/// <c>global.json</c> pinned for this repository. <c>DOTNET_HOST_PATH</c> is the host the current
/// build and test run were started with; the runtime directory is the same host's installation and
/// is used only when the variable is absent.
/// </remarks>
internal static class DotNetHost
{
    private static readonly Lazy<string> Resolved = new(Locate);

    internal static string Path => Resolved.Value;

    private static string Locate()
    {
        var declared = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");

        if (!string.IsNullOrWhiteSpace(declared) && File.Exists(declared))
        {
            return declared;
        }

        var executable = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

        // .../shared/Microsoft.NETCore.App/<version>/ -> up three levels is the installation root.
        var runtime = new DirectoryInfo(RuntimeEnvironment.GetRuntimeDirectory());
        var root = runtime.Parent?.Parent?.Parent;

        if (root is not null)
        {
            var candidate = System.IO.Path.Combine(root.FullName, executable);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "Could not resolve the .NET host that is running this test. Set DOTNET_HOST_PATH or run the suite through the pinned SDK.");
    }
}
