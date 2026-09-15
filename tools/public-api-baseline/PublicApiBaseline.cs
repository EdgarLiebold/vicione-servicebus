using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;

namespace ViciOne.ServiceBus.Build;

/// <summary>Emits a stable public-surface inventory from assemblies restored out of freshly built packages.</summary>
internal static class PublicApiBaseline
{
    private const BindingFlags DeclaredMembers = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static int Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Usage: ViciOne.ServiceBus.Build.PublicApiBaseline <global-packages> <package-feed> <output-file>");
            return 2;
        }

        string globalPackages = Path.GetFullPath(args[0]);
        string packageFeed = Path.GetFullPath(args[1]);
        string outputFile = Path.GetFullPath(args[2]);
        if (!Directory.Exists(globalPackages) || !Directory.Exists(packageFeed))
            throw new DirectoryNotFoundException("Both the restored global-package folder and fresh package feed must exist.");

        string[] packageFiles = Directory.GetFiles(packageFeed, "ViciOne.ServiceBus*.nupkg")
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (packageFiles.Length == 0)
            throw new InvalidOperationException("No freshly packed ViciOne.ServiceBus packages were found.");

        string[] assemblyFiles = Directory.GetFiles(globalPackages, "ViciOne.ServiceBus*.dll", SearchOption.AllDirectories)
            .Where(static path => Normalized(path).Contains("/lib/net10.0/", StringComparison.Ordinal))
            .Where(static path => !path.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (assemblyFiles.Length == 0)
            throw new InvalidOperationException("The package-only restore produced no ViciOne.ServiceBus net10.0 assemblies.");

        var lines = new List<string>
        {
            "# ViciOne.ServiceBus packed public API baseline",
            "# Generated exclusively from assemblies restored from the fresh package feed.",
            $"PACKAGES {packageFiles.Length}",
        };
        foreach (string package in packageFiles)
            lines.Add($"PACKAGE {Path.GetFileName(package)}");

        lines.Add($"ASSEMBLIES {assemblyFiles.Length}");
        var context = new PackageAssemblyLoadContext(globalPackages);
        foreach (string assemblyFile in assemblyFiles)
        {
            Assembly assembly = context.LoadFromAssemblyPath(assemblyFile);
            lines.Add(string.Empty);
            lines.Add($"ASSEMBLY {assembly.GetName().Name} VERSION={assembly.GetName().Version}");

            foreach (Type type in assembly.GetTypes().Where(IsExternallyVisible).OrderBy(FormatType, StringComparer.Ordinal))
            {
                lines.Add($"TYPE {TypeKind(type)} {TypeVisibility(type)} {FormatType(type)}{FormatTypeModifiers(type)}");
                foreach (string member in FormatMembers(type).Order(StringComparer.Ordinal))
                    lines.Add($"  {member}");
            }
        }
        context.Unload();

        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        File.WriteAllText(outputFile, string.Join('\n', lines) + "\n");
        Console.WriteLine($"Packed public API baseline generated: {assemblyFiles.Length} assemblies, {lines.Count} lines, SHA256={Sha256(outputFile)}");
        return 0;
    }

    private static IEnumerable<string> FormatMembers(Type type)
    {
        foreach (ConstructorInfo constructor in type.GetConstructors(DeclaredMembers).Where(IsExternallyVisible))
            yield return $"CTOR {Visibility(constructor)} {FormatType(type)}({FormatParameters(constructor.GetParameters())})";

        foreach (MethodInfo method in type.GetMethods(DeclaredMembers)
                     .Where(IsExternallyVisible)
                     .Where(static method => !IsPropertyOrEventAccessor(method)))
        {
            string genericArguments = method.IsGenericMethodDefinition
                ? $"<{string.Join(",", method.GetGenericArguments().Select(static argument => argument.Name))}>"
                : string.Empty;
            yield return $"METHOD {Visibility(method)} {MethodModifiers(method)}{FormatType(method.ReturnType)} "
                + $"{method.Name}{genericArguments}({FormatParameters(method.GetParameters())})";
        }

        foreach (PropertyInfo property in type.GetProperties(DeclaredMembers).Where(IsExternallyVisible))
        {
            string index = property.GetIndexParameters() is { Length: > 0 } parameters
                ? $"[{FormatParameters(parameters)}]"
                : string.Empty;
            yield return $"PROPERTY {FormatType(property.PropertyType)} {property.Name}{index} "
                + $"{{ {Accessor(property.GetMethod, "get")} {Accessor(property.SetMethod, "set")} }}";
        }

        foreach (EventInfo @event in type.GetEvents(DeclaredMembers).Where(IsExternallyVisible))
            yield return $"EVENT {FormatType(@event.EventHandlerType!)} {@event.Name} "
                + $"{{ {Accessor(@event.AddMethod, "add")} {Accessor(@event.RemoveMethod, "remove")} }}";

        foreach (FieldInfo field in type.GetFields(DeclaredMembers).Where(IsExternallyVisible))
        {
            string modifiers = field.IsLiteral ? "const " : field.IsStatic ? "static " : string.Empty;
            modifiers += field.IsInitOnly ? "readonly " : string.Empty;
            string value = field.IsLiteral ? $" = {FormatValue(field.GetRawConstantValue())}" : string.Empty;
            yield return $"FIELD {Visibility(field)} {modifiers}{FormatType(field.FieldType)} {field.Name}{value}";
        }
    }

    private static bool IsExternallyVisible(Type type)
    {
        bool ownVisibility = type.IsNested
            ? type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem
            : type.IsPublic;
        return ownVisibility && (type.DeclaringType is null || IsExternallyVisible(type.DeclaringType));
    }

    private static bool IsExternallyVisible(MethodBase method)
        => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static bool IsExternallyVisible(FieldInfo field)
        => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static bool IsExternallyVisible(PropertyInfo property)
        => property.GetAccessors(true).Any(IsExternallyVisible);

    private static bool IsExternallyVisible(EventInfo @event)
        => new[] { @event.AddMethod, @event.RemoveMethod, @event.RaiseMethod }
            .Where(static method => method is not null)
            .Any(static method => IsExternallyVisible(method!));

    private static bool IsPropertyOrEventAccessor(MethodInfo method)
        => method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal)
                                    || method.Name.StartsWith("set_", StringComparison.Ordinal)
                                    || method.Name.StartsWith("add_", StringComparison.Ordinal)
                                    || method.Name.StartsWith("remove_", StringComparison.Ordinal));

    private static string TypeKind(Type type)
        => type.IsInterface ? "interface" : type.IsEnum ? "enum" : typeof(MulticastDelegate).IsAssignableFrom(type.BaseType)
            ? "delegate" : type.IsValueType ? "struct" : "class";

    private static string TypeVisibility(Type type)
        => type.IsNestedFamily ? "protected" : type.IsNestedFamORAssem ? "protected-internal" : "public";

    private static string FormatTypeModifiers(Type type)
    {
        var modifiers = new List<string>();
        if (type.IsAbstract && type.IsSealed)
            modifiers.Add("static");
        else
        {
            if (type.IsAbstract && !type.IsInterface)
                modifiers.Add("abstract");
            if (type.IsSealed && !type.IsValueType)
                modifiers.Add("sealed");
        }

        if (type.BaseType is { } baseType && baseType != typeof(object) && baseType != typeof(ValueType)
            && baseType != typeof(Enum) && baseType != typeof(MulticastDelegate))
            modifiers.Add($"base={FormatType(baseType)}");

        Type[] interfaces = GetDirectInterfaces(type)
            .Where(IsExternallyVisible)
            .OrderBy(FormatType, StringComparer.Ordinal)
            .ToArray();
        if (interfaces.Length > 0)
            modifiers.Add($"interfaces={string.Join("&", interfaces.Select(FormatType))}");

        return modifiers.Count == 0 ? string.Empty : $" [{string.Join(",", modifiers)}]";
    }

    private static IEnumerable<Type> GetDirectInterfaces(Type type)
    {
        Type[] interfaces = type.GetInterfaces();
        var inherited = new HashSet<Type>();
        if (type.BaseType is { } baseType)
            inherited.UnionWith(baseType.GetInterfaces());

        foreach (Type interfaceType in interfaces)
            inherited.UnionWith(interfaceType.GetInterfaces());

        return interfaces.Where(candidate => !inherited.Contains(candidate));
    }

    private static string MethodModifiers(MethodInfo method)
    {
        var modifiers = new List<string>();
        if (method.IsStatic)
            modifiers.Add("static");
        if (method.IsAbstract)
            modifiers.Add("abstract");
        else if (method.IsVirtual && method.GetBaseDefinition() == method)
            modifiers.Add("virtual");
        string formatted = string.Join(' ', modifiers);
        return formatted.Length == 0 ? string.Empty : formatted + " ";
    }

    private static string Visibility(MethodBase method)
        => method.IsPublic ? "public" : method.IsFamily ? "protected" : "protected-internal";

    private static string Visibility(FieldInfo field)
        => field.IsPublic ? "public" : field.IsFamily ? "protected" : "protected-internal";

    private static string Accessor(MethodInfo? method, string name)
        => method is null || !IsExternallyVisible(method) ? string.Empty : $"{Visibility(method)}-{name};";

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters)
        => string.Join(", ", parameters.Select(FormatParameter));

    private static string FormatParameter(ParameterInfo parameter)
    {
        string modifier = parameter.IsOut ? "out "
            : parameter.ParameterType.IsByRef && parameter.IsIn ? "in "
            : parameter.ParameterType.IsByRef ? "ref "
            : string.Empty;
        Type parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        string optional = parameter.HasDefaultValue ? $" = {FormatValue(parameter.DefaultValue)}" : string.Empty;
        return $"{modifier}{FormatType(parameterType)} {parameter.Name}{optional}";
    }

    /// <summary>Formats the CLR type identity used by public API declarations and member signatures.</summary>
    /// <param name="type">The reflected type, including any generic arguments or element-type modifiers.</param>
    /// <returns>The namespace-qualified type name and its arguments or element-type modifiers.</returns>
    internal static string FormatType(Type type)
    {
        if (type.IsByRef)
            return $"{FormatType(type.GetElementType()!)}&";
        if (type.IsPointer)
            return $"{FormatType(type.GetElementType()!)}*";
        if (type.IsArray)
        {
            string dimensions = type.GetArrayRank() == 1 && !type.IsSZArray
                ? "*"
                : new string(',', type.GetArrayRank() - 1);
            return $"{FormatType(type.GetElementType()!)}[{dimensions}]";
        }
        if (type.IsGenericParameter)
            return type.Name;
        if (!type.IsGenericType)
            return (type.FullName ?? type.Name).Replace('+', '.');

        string[] segments = (type.GetGenericTypeDefinition().FullName ?? type.Name).Split('+');
        Type[] arguments = type.GetGenericArguments();
        int argumentIndex = 0;
        for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
        {
            string segment = segments[segmentIndex];
            int tick = segment.IndexOf('`');
            if (tick < 0)
                continue;

            int arity = int.Parse(segment.AsSpan(tick + 1), System.Globalization.CultureInfo.InvariantCulture);
            string ownArguments = string.Join(",", arguments[argumentIndex..(argumentIndex + arity)].Select(FormatType));
            segments[segmentIndex] = $"{segment[..tick]}<{ownArguments}>";
            argumentIndex += arity;
        }

        return string.Join('.', segments);
    }

    private static string FormatValue(object? value)
        => value switch
        {
            null => "null",
            string text => $"\"{Escape(text)}\"",
            char character => $"'{Escape(character.ToString())}'",
            bool boolean => boolean ? "true" : "false",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "null",
        };

    private static string Escape(string value)
        => string.Concat(value.Select(static character => character switch
        {
            '\\' => "\\\\",
            '\"' => "\\\"",
            '\0' => "\\0",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ when char.IsControl(character) => $"\\u{(int)character:x4}",
            _ => character.ToString(),
        }));

    private static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Normalized(string path) => path.Replace('\\', '/');

    private sealed class PackageAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly IReadOnlyDictionary<string, string[]> _candidates;

        public PackageAssemblyLoadContext(string globalPackages)
            : base("ViciOnePackedPublicApi", isCollectible: true)
        {
            _candidates = Directory.GetFiles(globalPackages, "*.dll", SearchOption.AllDirectories)
                .Select(static path => (Path: path, Name: TryGetAssemblyName(path)))
                .Where(static candidate => candidate.Name?.Name is not null)
                .GroupBy(static candidate => candidate.Name!.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.OrderByDescending(candidate => candidate.Name!.Version).Select(static candidate => candidate.Path).ToArray(),
                    StringComparer.OrdinalIgnoreCase);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (!_candidates.TryGetValue(assemblyName.Name!, out string[]? candidates))
                return null;

            string candidate = candidates.FirstOrDefault(path => AssemblyName.ReferenceMatchesDefinition(
                assemblyName,
                AssemblyName.GetAssemblyName(path))) ?? candidates[0];
            return LoadFromAssemblyPath(candidate);
        }

        private static AssemblyName? TryGetAssemblyName(string path)
        {
            try
            {
                return AssemblyName.GetAssemblyName(path);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
            catch (System.Globalization.CultureNotFoundException)
            {
                // Satellite assemblies are irrelevant to an API baseline and cannot be parsed in invariant mode.
                return null;
            }
        }
    }
}
