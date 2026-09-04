#:package Microsoft.Build.Locator
#:package Microsoft.CodeAnalysis.CSharp.Workspaces
#:package Microsoft.CodeAnalysis.Workspaces.MSBuild
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false
#:property DisableMSBuildAssemblyCopyCheck=true
#:property NuGetLockFilePath=RenameAsync.packages.lock.json

using System.Text.Json;

if (args.Length != 3)
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/api-conventions/SynchronizeAsyncTestReferences.cs -- <rename-report.json> <repository-root> <output-report.json>");
    return 2;
}

string renameReportPath = Path.GetFullPath(args[0]);
string repositoryRoot = Path.GetFullPath(args[1]);
string outputReportPath = Path.GetFullPath(args[2]);

using JsonDocument renameReport = JsonDocument.Parse(File.ReadAllText(renameReportPath));
Rename[] renames = renameReport.RootElement.GetProperty("changes")
    .EnumerateArray()
    .Where(change => change.GetProperty("Candidate").GetProperty("IsTestScenario").GetBoolean())
    .Where(change => change.GetProperty("Status").GetString() is "renamed" or "already-renamed")
    .Select(change => change.GetProperty("Candidate"))
    .Select(candidate => new Rename(
        candidate.GetProperty("ContainingType").GetString()!,
        candidate.GetProperty("OldName").GetString()!,
        candidate.GetProperty("NewName").GetString()!))
    .Distinct()
    .ToArray();

var changes = new List<ReferenceChange>();
string obligationMapRoot = Path.Combine(repositoryRoot, "evidence", "native-tests", "obligation-maps");
foreach (string path in Directory.EnumerateFiles(obligationMapRoot, "*.tsv", SearchOption.TopDirectoryOnly))
{
    string[] lines = File.ReadAllLines(path);
    bool changed = false;
    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
    {
        string[] fields = lines[lineIndex].Split('\t');
        if (fields.Length == 0)
            continue;

        string target = fields[^1];
        foreach (Rename rename in renames)
        {
            string fullIdentity = rename.ContainingType + "." + rename.OldName;
            string shortType = rename.ContainingType[(rename.ContainingType.LastIndexOf('.') + 1)..];
            string shortIdentity = shortType + "." + rename.OldName;
            if (target != fullIdentity &&
                target != shortIdentity &&
                !target.EndsWith("." + shortIdentity, StringComparison.Ordinal))
                continue;

            fields[^1] = target[..^rename.OldName.Length] + rename.NewName;
            lines[lineIndex] = string.Join('\t', fields);
            changes.Add(new ReferenceChange(
                Path.GetRelativePath(repositoryRoot, path),
                lineIndex + 1,
                target,
                fields[^1]));
            changed = true;
            break;
        }
    }

    if (changed)
        File.WriteAllLines(path, lines);
}

string testsRoot = Path.Combine(repositoryRoot, "tests");
foreach (string path in Directory.EnumerateFiles(testsRoot, "*.json", SearchOption.AllDirectories)
             .Where(path => !HasGeneratedDirectory(path)))
{
    string content = File.ReadAllText(path);
    bool changed = false;
    foreach (Rename rename in renames)
    {
        string oldIdentity = rename.ContainingType + "." + rename.OldName;
        string newIdentity = rename.ContainingType + "." + rename.NewName;
        string oldJsonString = JsonSerializer.Serialize(oldIdentity);
        if (!content.Contains(oldJsonString, StringComparison.Ordinal))
            continue;

        int searchIndex = 0;
        while ((searchIndex = content.IndexOf(oldJsonString, searchIndex, StringComparison.Ordinal)) >= 0)
        {
            int line = content.AsSpan(0, searchIndex).Count('\n') + 1;
            changes.Add(new ReferenceChange(
                Path.GetRelativePath(repositoryRoot, path),
                line,
                oldIdentity,
                newIdentity));
            searchIndex += oldJsonString.Length;
        }

        content = content.Replace(oldJsonString, JsonSerializer.Serialize(newIdentity), StringComparison.Ordinal);
        changed = true;
    }

    if (changed)
        File.WriteAllText(path, content);
}

Directory.CreateDirectory(Path.GetDirectoryName(outputReportPath)!);
File.WriteAllText(
    outputReportPath,
    JsonSerializer.Serialize(
        new
        {
            renameReport = renameReportPath,
            renameCount = renames.Length,
            changedReferenceCount = changes.Count,
            changes,
        },
        new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Test renames: {renames.Length}");
Console.WriteLine($"Updated obligation-map references: {changes.Count}");
Console.WriteLine($"Report: {outputReportPath}");
return 0;

static bool HasGeneratedDirectory(string path)
{
    return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(segment => segment is "bin" or "obj");
}

sealed record Rename(string ContainingType, string OldName, string NewName);

sealed record ReferenceChange(string File, int Line, string OldIdentity, string NewIdentity);
