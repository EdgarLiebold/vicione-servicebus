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

    /// <summary>Formats a type's own generic contracts and its declared public and protected members.</summary>
    /// <param name="type">The type whose declared API is inventoried.</param>
    /// <returns>Member declarations before the inventory's ordinal ordering.</returns>
    internal static IEnumerable<string> FormatMembers(Type type)
    {
        // Each enumeration owns the reflection cache and does not share it across package load contexts.
        NullabilityInfoContext nullability = new();
        if (type.IsGenericTypeDefinition)
        {
            int inheritedParameterCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
            foreach (Type parameter in type.GetGenericArguments().Skip(inheritedParameterCount))
                yield return $"GENERIC {FormatGenericParameter(parameter)}";
        }

        foreach (ConstructorInfo constructor in type.GetConstructors(DeclaredMembers).Where(IsExternallyVisible))
            yield return $"CTOR {Visibility(constructor)} {FormatType(type)}({FormatParameters(constructor.GetParameters(), nullability)})";

        foreach (MethodInfo method in type.GetMethods(DeclaredMembers)
                     .Where(IsExternallyVisible)
                     .Where(static method => !IsPropertyOrEventAccessor(method)))
        {
            string genericArguments = method.IsGenericMethodDefinition
                ? $"<{string.Join(",", method.GetGenericArguments().Select(static argument => argument.Name))}>"
                : string.Empty;
            string genericContracts = method.IsGenericMethodDefinition
                ? $" [generic={string.Join(",", method.GetGenericArguments().Select(FormatGenericParameter))}]"
                : string.Empty;
            yield return $"METHOD {Visibility(method)} {MethodModifiers(method)}{FormatType(method.ReturnType)} "
                + $"{method.Name}{genericArguments}({FormatParameters(method.GetParameters(), nullability)}){genericContracts}"
                + FormatCustomModifiers(method.ReturnParameter.GetRequiredCustomModifiers(), method.ReturnParameter.GetOptionalCustomModifiers(), "return-")
                + FormatNullability(nullability.Create(method.ReturnParameter), "return-");
        }

        foreach (PropertyInfo property in type.GetProperties(DeclaredMembers).Where(IsExternallyVisible))
        {
            string index = property.GetIndexParameters() is { Length: > 0 } parameters
                ? $"[{FormatParameters(parameters, nullability)}]"
                : string.Empty;
            yield return $"PROPERTY {FormatType(property.PropertyType)} {property.Name}{index} "
                + $"{{ {Accessor(property.GetMethod, "get")} {Accessor(property.SetMethod, "set")} }}"
                + FormatCustomModifiers(property.GetRequiredCustomModifiers(), property.GetOptionalCustomModifiers())
                + FormatNullability(nullability.Create(property), read: property.GetMethod is { } getter && IsExternallyVisible(getter),
                    write: property.SetMethod is { } setter && IsExternallyVisible(setter));
        }

        foreach (EventInfo @event in type.GetEvents(DeclaredMembers).Where(IsExternallyVisible))
            yield return $"EVENT {FormatType(@event.EventHandlerType!)} {@event.Name} "
                + $"{{ {Accessor(@event.AddMethod, "add")} {Accessor(@event.RemoveMethod, "remove")} }}"
                + FormatNullability(nullability.Create(@event));

        foreach (FieldInfo field in type.GetFields(DeclaredMembers).Where(IsExternallyVisible))
        {
            string modifiers = field.IsLiteral ? "const " : field.IsStatic ? "static " : string.Empty;
            modifiers += field.IsInitOnly ? "readonly " : string.Empty;
            string value = field.IsLiteral ? $" = {FormatValue(field.GetRawConstantValue())}" : string.Empty;
            yield return $"FIELD {Visibility(field)} {modifiers}{FormatType(field.FieldType)} {field.Name}{value}"
                + FormatCustomModifiers(field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers())
                + FormatNullability(nullability.Create(field));
        }
    }

    private static string FormatGenericParameter(Type parameter)
    {
        string flags = parameter.GenericParameterAttributes.ToString().Replace(", ", "&", StringComparison.Ordinal);
        string constraints = string.Join("&", parameter.GetGenericParameterConstraints()
            .Select(FormatType)
            .Order(StringComparer.Ordinal));
        bool unmanaged = parameter.GetCustomAttributesData().Any(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsUnmanagedAttribute");

        return $"{parameter.Name}{{flags={flags};constraints=[{constraints}];nullable={FormatGenericNullability(parameter)};unmanaged={FormatValue(unmanaged)}}}";
    }

    private static string FormatGenericNullability(Type parameter)
    {
        CustomAttributeData? annotation = parameter.GetCustomAttributesData().SingleOrDefault(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        if (annotation is not null)
        {
            object? value = annotation.ConstructorArguments.Single().Value;
            return value is IEnumerable<CustomAttributeTypedArgument> flags
                ? $"[{string.Join(",", flags.Select(flag => FormatValue(flag.Value)))}]"
                : $"[{FormatValue(value)}]";
        }

        if ((parameter.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            return "[0]";

        for (MemberInfo? scope = parameter.DeclaringMethod ?? (MemberInfo?)parameter.DeclaringType;
             scope is not null;
             scope = scope.DeclaringType)
        {
            CustomAttributeData? context = scope.GetCustomAttributesData().SingleOrDefault(attribute =>
                attribute.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (context is not null)
                return $"[{FormatValue(context.ConstructorArguments.Single().Value)}]";
        }

        return "[0]";
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
        if (method.IsVirtual)
        {
            if (method.GetBaseDefinition() != method)
            {
                if (method.IsFinal)
                    modifiers.Add("sealed");
                modifiers.Add("override");
            }
            else
            {
                if (!method.IsAbstract)
                    modifiers.Add("virtual");
                if (method.IsFinal)
                    modifiers.Add("final");
            }
        }
        string formatted = string.Join(' ', modifiers);
        return formatted.Length == 0 ? string.Empty : formatted + " ";
    }

    private static string Visibility(MethodBase method)
        => method.IsPublic ? "public" : method.IsFamily ? "protected" : "protected-internal";

    private static string Visibility(FieldInfo field)
        => field.IsPublic ? "public" : field.IsFamily ? "protected" : "protected-internal";

    private static string Accessor(MethodInfo? method, string name)
    {
        if (method is null || !IsExternallyVisible(method))
            return string.Empty;

        Type[] required = method.ReturnParameter.GetRequiredCustomModifiers();
        if (name == "set" && required.Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit"))
            name = "init";

        string modifiers = MethodModifiers(method).TrimEnd();
        string methodContract = modifiers.Length == 0 ? string.Empty : $"[modifiers={modifiers}]";
        string returnContract = FormatCustomModifiers(required, method.ReturnParameter.GetOptionalCustomModifiers(), "return-").TrimStart();
        string parameterContracts = string.Join(",", method.GetParameters().Select(parameter =>
            FormatCustomModifiers(parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers()).TrimStart()));
        string parameters = parameterContracts.Trim(',').Length == 0 ? string.Empty : $"[parameter-modifiers=[{parameterContracts}]]";
        return $"{Visibility(method)}-{name}{methodContract}{returnContract}{parameters};";
    }

    /// <summary>Preserves ordered required and optional CLR modifiers without conflating either list.</summary>
    private static string FormatCustomModifiers(Type[] required, Type[] optional, string prefix = "")
        => required.Length == 0 && optional.Length == 0 ? string.Empty
            : $" [{prefix}modreq=[{string.Join(",", required.Select(FormatType))}];{prefix}modopt=[{string.Join(",", optional.Select(FormatType))}]]";

    private static string FormatParameters(IEnumerable<ParameterInfo> parameters, NullabilityInfoContext nullability)
        => string.Join(", ", parameters.Select(parameter => FormatParameter(parameter, nullability)));

    private static string FormatParameter(ParameterInfo parameter, NullabilityInfoContext nullability)
    {
        IList<CustomAttributeData> attributes = parameter.GetCustomAttributesData();
        bool readOnlyLocation = attributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.RequiresLocationAttribute");
        bool readOnlyInput = attributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
        bool byReference = parameter.ParameterType.IsByRef;
        string modifier = !byReference ? string.Empty
            : readOnlyLocation ? "ref readonly "
            : readOnlyInput ? "in "
            : parameter.IsOut && !parameter.IsIn ? "out "
            : "ref ";
        Type parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        bool parameterCollection = attributes.Any(attribute =>
            attribute.AttributeType.FullName is "System.ParamArrayAttribute" or "System.Runtime.CompilerServices.ParamCollectionAttribute");
        string collection = parameterCollection ? "params " : string.Empty;
        string optional = parameter.HasDefaultValue
            ? $" = {FormatParameterDefault(parameter, parameterType)}" + (parameter.IsOptional ? string.Empty : " [required]")
            : parameter.IsOptional ? " [optional]" : string.Empty;
        bool explicitDirection = (parameter.IsIn || parameter.IsOut) && (!byReference || modifier == "ref ");
        string direction = !explicitDirection ? string.Empty
            : $" [direction={(parameter.IsIn && parameter.IsOut ? "in&out" : parameter.IsIn ? "in" : "out")}]";
        return $"{collection}{modifier}{FormatType(parameterType)} {parameter.Name}{optional}{direction}"
            + FormatCustomModifiers(parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers())
            + FormatNullability(nullability.Create(parameter));
    }

    /// <summary>Preserves unknown or refined read/write positions relative to each CLR type's nullability default.</summary>
    private static string FormatNullability(NullabilityInfo info, string prefix = "", bool read = true, bool write = true)
        => HasNonDefaultNullability(info, read, write)
            ? $" [{prefix}nullability={FormatNullabilityNode(info, read, write)}]"
            : string.Empty;

    private static bool HasNonDefaultNullability(NullabilityInfo info, bool read = true, bool write = true)
    {
        Type type = info.Type.IsByRef ? info.Type.GetElementType()! : info.Type;
        bool reference = type.IsGenericParameter || (!type.IsValueType && !type.IsPointer && !type.IsFunctionPointer);
        bool nullableValue = Nullable.GetUnderlyingType(type) is not null;
        NullabilityState defaultState = nullableValue ? NullabilityState.Nullable : NullabilityState.NotNull;
        return ((reference || nullableValue) && ((read && info.ReadState != defaultState) || (write && info.WriteState != defaultState)))
            || (info.ElementType is { } element && HasNonDefaultNullability(element))
            || info.GenericTypeArguments.Any(argument => HasNonDefaultNullability(argument));
    }

    /// <summary>Preserves child positions even when only one node differs from its CLR type's nullability default.</summary>
    private static string FormatNullabilityNode(NullabilityInfo info, bool read = true, bool write = true)
    {
        string element = info.ElementType is { } child ? $";element={FormatNullabilityNode(child)}" : string.Empty;
        string arguments = info.GenericTypeArguments.Length == 0 ? string.Empty
            : $";arguments=[{string.Join(",", info.GenericTypeArguments.Select(argument => FormatNullabilityNode(argument)))}]";
        // Accessor visibility limits the root contract, not the mutability of returned arrays or generic elements.
        return $"{{read={(read ? info.ReadState.ToString() : "none")};write={(write ? info.WriteState.ToString() : "none")}{element}{arguments}}}";
    }

    /// <summary>Distinguishes typed struct and generic defaults from concrete reference and nullable-value null constants.</summary>
    private static string FormatParameterDefault(ParameterInfo parameter, Type parameterType)
        => parameter.RawDefaultValue is null && (parameterType.IsGenericParameter
            || (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null))
            ? $"default({FormatType(parameterType)})"
            : FormatValue(parameter.RawDefaultValue);

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
            char character => $"'{Escape(character.ToString()).Replace("'", "\\'", StringComparison.Ordinal)}'",
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
