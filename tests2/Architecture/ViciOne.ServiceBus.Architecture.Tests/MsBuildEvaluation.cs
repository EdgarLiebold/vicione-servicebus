using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ViciOne.ServiceBus.Architecture.Tests;

/// <summary>
/// Evaluates a real project with MSBuild and exposes the result.
/// </summary>
/// <remarks>
/// This asks MSBuild what a project actually evaluates to, rather than reading the XML and
/// believing it. The difference matters: imports, the SDK, Directory.Build files, central package
/// management and a GlobalPackageReference all change the answer, and none of them are visible in
/// the project file itself.
/// <para>
/// Only <c>-getProperty</c> and <c>-getItem</c> are used, so evaluation runs but no target executes
/// and nothing is written into the checkout. Results are cached per project because the evaluation
/// costs seconds and every test asks about the same two projects.
/// </para>
/// </remarks>
internal static class MsBuildEvaluation
{
    private static readonly Dictionary<string, JsonDocument> Cache = [];
    private static readonly Lock Gate = new();
    private static readonly TimeSpan Budget = TimeSpan.FromMinutes(3);

    private const string Properties =
        "TargetFramework;IsTestProject;IsPackable;IsTestingPlatformApplication;UseMicrosoftTestingPlatformRunner;OutputType;DebugType;LangVersion;ArtifactsPath;ViciOneProjectIdentity;ViciOneNativeTestTree;UserSecretsId";

    private const string Items =
        "PackageReference;ProjectReference;Content;ViciOneForbiddenNativeTestPackage";

    /// <summary>Evaluates a project once and returns the parsed MSBuild output.</summary>
    internal static JsonElement Evaluate(string projectPath)
    {
        lock (Gate)
        {
            if (Cache.TryGetValue(projectPath, out var cached))
            {
                return cached.RootElement;
            }

            var document = JsonDocument.Parse(Run(projectPath));
            Cache[projectPath] = document;
            return document.RootElement;
        }
    }

    /// <summary>Reads one evaluated property value.</summary>
    internal static string PropertyOf(string projectPath, string name) =>
        Evaluate(projectPath).GetProperty("Properties").GetProperty(name).GetString() ?? string.Empty;

    /// <summary>Reads the <c>Identity</c> of every item of one type.</summary>
    internal static IReadOnlyList<string> ItemIdentities(string projectPath, string itemType) =>
        ItemMetadata(projectPath, itemType, "Identity");

    /// <summary>Reads one metadata value from every item of one type.</summary>
    internal static IReadOnlyList<string> ItemMetadata(string projectPath, string itemType, string metadata)
    {
        var items = Evaluate(projectPath).GetProperty("Items");

        if (!items.TryGetProperty(itemType, out var ofType))
        {
            return [];
        }

        return ofType.EnumerateArray()
            .Select(item => item.TryGetProperty(metadata, out var value) ? value.GetString() ?? string.Empty : string.Empty)
            .Where(value => value.Length > 0)
            .ToArray();
    }

    private static string Run(string projectPath)
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
