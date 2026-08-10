// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-03, 2026-08-09.
//
// Dumps the public surface of every ViciOne.ServiceBus assembly under a directory.
//
// A package hash proves that an artifact did not change after it was built. It says nothing about
// what the artifact exposes, so it cannot answer the only question that matters when a work package
// touches types that ship: did the public surface move, and if so, where. This tool answers that by
// reading the metadata of the built assemblies and writing every public and protected member as one
// stable, sorted line. Two runs of it, one per tree, diff cleanly.
//
// Metadata only, so nothing is executed and no dependency has to resolve. That matters because the
// baseline and the current tree are read by the same process, and loading both for real would mean
// two versions of the same assembly identity in one context.
//
//   dotnet run tools/ci/api_surface.cs -- <assembly directory> <output.json>

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;


if (args.Length != 2)
{
    Console.Error.WriteLine("usage: api_surface <assembly directory> <output.json>");
    return 2;
}

var root = new DirectoryInfo(args[0]);
if (!root.Exists)
{
    Console.Error.WriteLine($"no such directory: {root.FullName}");
    return 2;
}

// One assembly may be built for several target frameworks. The public surface is the same contract
// in each of them, so the first one found wins and the name stays the key.
var assemblies = new SortedDictionary<string, string>(StringComparer.Ordinal);
foreach (var file in root.EnumerateFiles("ViciOne.ServiceBus*.dll", SearchOption.AllDirectories))
{
    if (file.FullName.Contains("/obj/", StringComparison.Ordinal))
        continue;
    var name = Path.GetFileNameWithoutExtension(file.Name);
    if (!assemblies.ContainsKey(name))
        assemblies[name] = file.FullName;
}

var surface = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
foreach (var (name, path) in assemblies)
    surface[name] = ReadSurface(path);

// Written by hand rather than through JsonSerializer: a file based app runs with reflection based
// serialization disabled, and a source generated context would be more machinery than one flat
// object of string lists deserves.
var memberCount = surface.Values.Sum(x => x.Count);
var text = new StringBuilder();
text.Append("{\n");
text.Append("  \"schemaVersion\": 1,\n");
text.Append("  \"kind\": \"PUBLIC_API_SURFACE\",\n");
text.Append($"  \"assemblyCount\": {surface.Count},\n");
text.Append($"  \"memberCount\": {memberCount},\n");
text.Append("  \"assemblies\": {\n");
var assemblyIndex = 0;
foreach (var (name, members) in surface)
{
    text.Append($"    {Quote(name)}: [\n");
    for (var i = 0; i < members.Count; i++)
        text.Append($"      {Quote(members[i])}{(i + 1 < members.Count ? "," : "")}\n");
    text.Append($"    ]{(++assemblyIndex < surface.Count ? "," : "")}\n");
}
text.Append("  }\n}\n");

File.WriteAllText(args[1], text.ToString());
Console.WriteLine($"{surface.Count} assemblies, {memberCount} public members -> {args[1]}");
return 0;

// A file based app runs with reflection based JSON serialization disabled, so even quoting one
// string goes through a source generated context or through this. Surface lines are ASCII type and
// member names, so the escape set below is the whole of what can occur.
static string Quote(string value)
{
    var quoted = new StringBuilder("\"");
    foreach (var character in value)
    {
        quoted.Append(character switch
        {
            '"' => "\\\"",
            '\\' => "\\\\",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            < ' ' => $"\\u{(int)character:x4}",
            _ => character.ToString(),
        });
    }
    return quoted.Append('"').ToString();
}

static List<string> ReadSurface(string path)
{
    var members = new List<string>();
    using var stream = File.OpenRead(path);
    using var reader = new PEReader(stream);
    if (!reader.HasMetadata)
        return members;

    var metadata = reader.GetMetadataReader();

    foreach (var handle in metadata.TypeDefinitions)
    {
        var type = metadata.GetTypeDefinition(handle);
        if (!IsVisible(metadata, type))
            continue;

        var typeName = FullName(metadata, type);
        members.Add($"type {Describe(type.Attributes)} {typeName}");

        foreach (var fieldHandle in type.GetFields())
        {
            var field = metadata.GetFieldDefinition(fieldHandle);
            var visibility = field.Attributes & FieldAttributes.FieldAccessMask;
            if (visibility is FieldAttributes.Public or FieldAttributes.Family)
                members.Add($"field {typeName}.{metadata.GetString(field.Name)}");
        }

        foreach (var methodHandle in type.GetMethods())
        {
            var method = metadata.GetMethodDefinition(methodHandle);
            var visibility = method.Attributes & MethodAttributes.MemberAccessMask;
            if (visibility is not (MethodAttributes.Public or MethodAttributes.Family))
                continue;
            var methodName = metadata.GetString(method.Name);
            // Property and event accessors are reported through their property or event, not twice.
            if (methodName.StartsWith("get_", StringComparison.Ordinal)
                || methodName.StartsWith("set_", StringComparison.Ordinal)
                || methodName.StartsWith("add_", StringComparison.Ordinal)
                || methodName.StartsWith("remove_", StringComparison.Ordinal))
                continue;
            members.Add($"method {typeName}.{methodName}({method.GetParameters().Count})");
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            var property = metadata.GetPropertyDefinition(propertyHandle);
            var accessors = property.GetAccessors();
            if (!IsVisibleAccessor(metadata, accessors.Getter) && !IsVisibleAccessor(metadata, accessors.Setter))
                continue;
            var shape = (IsVisibleAccessor(metadata, accessors.Getter) ? "get" : "")
                        + (IsVisibleAccessor(metadata, accessors.Setter) ? "set" : "");
            members.Add($"property {typeName}.{metadata.GetString(property.Name)} [{shape}]");
        }

        foreach (var eventHandle in type.GetEvents())
        {
            var eventDefinition = metadata.GetEventDefinition(eventHandle);
            var accessors = eventDefinition.GetAccessors();
            if (!IsVisibleAccessor(metadata, accessors.Adder) && !IsVisibleAccessor(metadata, accessors.Remover))
                continue;
            members.Add($"event {typeName}.{metadata.GetString(eventDefinition.Name)}");
        }
    }

    members.Sort(StringComparer.Ordinal);

    // Overloads that differ only in parameter types render identically, and a set comparison then
    // cannot see one of them disappear. A DROP mutation proved exactly that: removing one overload of
    // IPublishEndpoint.Publish left the surface unchanged. Numbering repeats keeps every overload its
    // own line, so the count is part of the compared surface rather than lost in it.
    var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
    for (var i = 0; i < members.Count; i++)
    {
        var member = members[i];
        occurrences[member] = occurrences.TryGetValue(member, out var seen) ? seen + 1 : 1;
        if (occurrences[member] > 1)
            members[i] = $"{member} #{occurrences[member]}";
    }

    return members;
}

static bool IsVisibleAccessor(MetadataReader metadata, MethodDefinitionHandle handle)
{
    if (handle.IsNil)
        return false;
    var visibility = metadata.GetMethodDefinition(handle).Attributes & MethodAttributes.MemberAccessMask;
    return visibility is MethodAttributes.Public or MethodAttributes.Family;
}

static bool IsVisible(MetadataReader metadata, TypeDefinition type)
{
    var visibility = type.Attributes & TypeAttributes.VisibilityMask;
    if (visibility == TypeAttributes.Public)
        return true;
    if (visibility is not (TypeAttributes.NestedPublic or TypeAttributes.NestedFamily))
        return false;
    var declaring = type.GetDeclaringType();
    return !declaring.IsNil && IsVisible(metadata, metadata.GetTypeDefinition(declaring));
}

static string Describe(TypeAttributes attributes)
{
    if ((attributes & TypeAttributes.Interface) != 0)
        return "interface";
    if ((attributes & TypeAttributes.Abstract) != 0 && (attributes & TypeAttributes.Sealed) != 0)
        return "static";
    if ((attributes & TypeAttributes.Abstract) != 0)
        return "abstract";
    if ((attributes & TypeAttributes.Sealed) != 0)
        return "sealed";
    return "class";
}

static string FullName(MetadataReader metadata, TypeDefinition type)
{
    var name = metadata.GetString(type.Name);
    var declaring = type.GetDeclaringType();
    if (!declaring.IsNil)
        return $"{FullName(metadata, metadata.GetTypeDefinition(declaring))}+{name}";
    var space = metadata.GetString(type.Namespace);
    return string.IsNullOrEmpty(space) ? name : $"{space}.{name}";
}
