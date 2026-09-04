#:package System.Reflection.MetadataLoadContext
#:property Nullable=enable
#:property ImplicitUsings=enable
#:property JsonSerializerIsReflectionEnabledByDefault=true
#:property PublishAot=false

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run tools/api-conventions/ApiInventory.cs -- <artifacts-bin-root> <output.json>");
    return 2;
}

string binRoot = Path.GetFullPath(args[0]);
string outPath = Path.GetFullPath(args[1]);

string[] products =
[
    "ViciOne.ServiceBus.Abstractions",
    "ViciOne.ServiceBus",
    "ViciOne.ServiceBus.Testing",
    "ViciOne.ServiceBus.MessagePack",
    "ViciOne.ServiceBus.SignalR",
    "ViciOne.ServiceBus.StateMachineVisualizer",
    "ViciOne.ServiceBus.RabbitMqTransport",
    "ViciOne.ServiceBus.RabbitMqTransport.Testing",
    "ViciOne.ServiceBus.ActiveMqTransport",
    "ViciOne.ServiceBus.AmazonSqsTransport",
    "ViciOne.ServiceBus.Azure.ServiceBus.Core",
    "ViciOne.ServiceBus.Azure.ServiceBus.Testing",
    "ViciOne.ServiceBus.EventHubIntegration",
    "ViciOne.ServiceBus.EventHubIntegration.Testing",
    "ViciOne.ServiceBus.SqlTransport.PostgreSql",
    "ViciOne.ServiceBus.SqlTransport.SqlServer",
    "ViciOne.ServiceBus.EntityFrameworkCoreIntegration",
    "ViciOne.ServiceBus.Azure.Table",
    "ViciOne.ServiceBus.Azure.Storage",
    "ViciOne.ServiceBus.DynamoDbIntegration",
    "ViciOne.ServiceBus.AmazonS3",
    "ViciOne.ServiceBus.QuartzIntegration",
];

string[] nameBuckets =
[
    "Configurator", "Specification", "Factory", "Observer", "Filter", "Pipe", "Context",
    "Definition", "Builder", "Extensions", "Connector", "Provider", "Options", "Exception",
    "Topology", "Settings", "Handle", "Cache",
];

string[] callbackContractNames =
[
    "ViciOne.ServiceBus.IConsumer`1",
    "ViciOne.ServiceBus.IFilter`1",
    "ViciOne.ServiceBus.IPipe`1",
    "ViciOne.ServiceBus.IProbeSite",
    "ViciOne.ServiceBus.IExecuteActivity`1",
    "ViciOne.ServiceBus.ICompensateActivity`1",
    "ViciOne.ServiceBus.IActivity`2",
    "ViciOne.ServiceBus.IJobConsumer`1",
    "ViciOne.ServiceBus.IActivityObserver",
    "ViciOne.ServiceBus.ISendObserver",
    "ViciOne.ServiceBus.IPublishObserver",
    "ViciOne.ServiceBus.IConsumeObserver",
    "ViciOne.ServiceBus.IConsumeMessageObserver`1",
    "ViciOne.ServiceBus.IReceiveObserver",
    "ViciOne.ServiceBus.IReceiveEndpointObserver",
    "ViciOne.ServiceBus.IReceiveTransportObserver",
    "ViciOne.ServiceBus.IFilterObserver",
    "ViciOne.ServiceBus.IFilterObserver`1",
    "ViciOne.ServiceBus.IRetryObserver",
    "ViciOne.ServiceBus.IBusObserver",
    "ViciOne.ServiceBus.IConsumerFactory`1",
    "ViciOne.ServiceBus.ISagaFactory`2",
    "ViciOne.ServiceBus.ISagaPolicy`2",
    "ViciOne.ServiceBus.ISagaRepository`1",
    "ViciOne.ServiceBus.Saga.ISagaRepositoryContextFactory`1",
    "ViciOne.ServiceBus.Saga.ISagaConsumeContextFactory`1",
    "ViciOne.ServiceBus.Saga.ISagaConsumeContextFactory`2",
    "ViciOne.ServiceBus.IStateMachineActivity",
    "ViciOne.ServiceBus.IStateMachineActivity`1",
    "ViciOne.ServiceBus.IStateMachineActivity`2",
    "ViciOne.ServiceBus.IBehavior`1",
    "ViciOne.ServiceBus.IBehavior`2",
    "ViciOne.ServiceBus.IEventObserver`1",
    "ViciOne.ServiceBus.IStateObserver`1",
];
var callbackContracts = callbackContractNames.ToHashSet(StringComparer.Ordinal);

var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (string file in Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
    paths[Path.GetFileNameWithoutExtension(file)] = file;

if (Directory.Exists(binRoot))
{
    foreach (string file in Directory.GetFiles(binRoot, "*.dll", SearchOption.AllDirectories))
    {
        string normalized = file.Replace('\\', '/');
        if (normalized.Contains("/release/", StringComparison.Ordinal) &&
            !normalized.Contains("/ref/", StringComparison.Ordinal) &&
            !normalized.Contains("/refint/", StringComparison.Ordinal))
        {
            paths.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }
    }
}

foreach (string product in products)
{
    string canonicalProductPath = Path.Combine(binRoot, product, "release", product + ".dll");
    if (File.Exists(canonicalProductPath))
        paths[product] = canonicalProductPath;
}

string nugetRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".nuget",
    "packages");
if (Directory.Exists(nugetRoot))
{
    foreach (string file in Directory.EnumerateFiles(nugetRoot, "*.dll", SearchOption.AllDirectories))
    {
        string normalized = file.Replace('\\', '/');
        if (normalized.Contains("/lib/net10.0/", StringComparison.Ordinal) ||
            normalized.Contains("/lib/net9.0/", StringComparison.Ordinal) ||
            normalized.Contains("/lib/net8.0/", StringComparison.Ordinal) ||
            normalized.Contains("/lib/netstandard2.1/", StringComparison.Ordinal) ||
            normalized.Contains("/lib/netstandard2.0/", StringComparison.Ordinal))
        {
            paths.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }
    }
}

var resolver = new PathAssemblyResolver(paths.Values);
using var metadata = new MetadataLoadContext(resolver, coreAssemblyName: "System.Private.CoreLib");

var reports = new List<AssemblyInventory>();
var allTypes = new List<TypeInventory>();
var loadedPublicTypes = new List<Type>();
var missingAssemblies = new List<string>();
var loadErrors = new Dictionary<string, string>(StringComparer.Ordinal);

foreach (string product in products)
{
    string assemblyPath = Path.Combine(binRoot, product, "release", product + ".dll");
    if (!File.Exists(assemblyPath))
    {
        missingAssemblies.Add(product);
        continue;
    }

    Assembly assembly;
    try
    {
        assembly = metadata.LoadFromAssemblyPath(assemblyPath);
    }
    catch (Exception exception)
    {
        loadErrors[product] = exception.Message;
        continue;
    }

    Type[] types;
    try
    {
        types = assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException exception)
    {
        types = exception.Types.Where(type => type is not null).ToArray()!;
        loadErrors[product] = string.Join(
            " | ",
            exception.LoaderExceptions.Select(loaderException => loaderException?.Message ?? "Unknown type-load failure."));
    }

    List<Type> publicTypes = types.Where(type => type.IsPublic || type.IsNestedPublic).ToList();
    List<Type> topLevelTypes = publicTypes.Where(type => !type.IsNested).ToList();
    loadedPublicTypes.AddRange(publicTypes);
    try
    {
        reports.Add(InventoryAssembly(product, publicTypes, topLevelTypes));
    }
    catch (Exception exception)
    {
        loadErrors[product] = loadErrors.TryGetValue(product, out string? existing)
            ? $"{existing} | Inventory failed: {exception.Message}"
            : $"Inventory failed: {exception.Message}";
    }

    allTypes.AddRange(topLevelTypes.Select(type => new TypeInventory(
        product,
        type.Namespace ?? string.Empty,
        type.Name,
        TypeKind(type))));
}

ApplicationSurfaceInventory applicationSurface;
try
{
    applicationSurface = InventoryApplicationSurface(loadedPublicTypes);
}
catch (Exception exception)
{
    loadErrors["application-surface"] = exception.Message;
    applicationSurface = new ApplicationSurfaceInventory(0, 0, 0, 0, [], [], [], []);
}

var aggregate = new AggregateInventory(
    reports.Sum(report => report.PublicTypes),
    reports.Sum(report => report.TopLevelPublicTypes),
    reports.Sum(report => report.NestedPublicTypes),
    reports.Sum(report => report.Interfaces),
    reports.Sum(report => report.ExtensionMethods),
    reports.Sum(report => report.AsyncMethods),
    reports.Sum(report => report.AsyncWithoutSuffix),
    reports.Sum(report => report.AsyncWithoutCancellationToken),
    reports.Sum(report => report.AsyncCancellationTokenNotLast),
    reports.Sum(report => report.DateTimeInSignatures),
    reports.Sum(report => report.TypesInInternalsNamespace),
    reports.Sum(report => report.Obsolete),
    reports.Sum(report => report.EditorBrowsableNever),
    reports.Sum(report => report.ExtensionMethodsInRootNamespace),
    reports.Sum(report => report.RootNamespacePublicTypes),
    applicationSurface.SendShapes,
    applicationSurface.PublishShapes,
    applicationSurface.ConsumeContextMembers,
    applicationSurface.ConsumeContextCompletions);

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllText(
    outPath,
    JsonSerializer.Serialize(
        new
        {
            generatedAtUtc = DateTimeOffset.UtcNow,
            binRoot,
            callbackInterfaceExceptions = callbackContractNames,
            missingAssemblies,
            loadErrors,
            aggregate,
            applicationSurface,
            report = reports,
            allTypes = allTypes.OrderBy(type => type.Assembly, StringComparer.Ordinal)
                .ThenBy(type => type.Namespace, StringComparer.Ordinal)
                .ThenBy(type => type.Name, StringComparer.Ordinal),
        },
        new JsonSerializerOptions { WriteIndented = true }));

Console.WriteLine($"Wrote {outPath}");
return missingAssemblies.Count == 0 && loadErrors.Count == 0 ? 0 : 1;

AssemblyInventory InventoryAssembly(string assemblyName, List<Type> publicTypes, List<Type> topLevelTypes)
{
    var extensionClasses = new HashSet<string>(StringComparer.Ordinal);
    var namespaces = topLevelTypes.Select(type => type.Namespace ?? string.Empty).ToHashSet(StringComparer.Ordinal);
    var buckets = nameBuckets.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
    var extensionMethodsPerNamespace = new Dictionary<string, int>(StringComparer.Ordinal);
    var asyncNoSuffixSample = new List<string>();
    var asyncNoCancellationTokenSample = new List<string>();
    var asyncCancellationTokenNotLastSample = new List<string>();
    var dateTimeSignatures = new HashSet<string>(StringComparer.Ordinal);
    var sendShapes = new HashSet<string>(StringComparer.Ordinal);
    var publishShapes = new HashSet<string>(StringComparer.Ordinal);
    var consumeContextMembers = new HashSet<string>(StringComparer.Ordinal);
    var consumeContextCompletions = new HashSet<string>(StringComparer.Ordinal);
    int members = 0;
    int extensionMethods = 0;
    int asyncMethods = 0;
    int asyncWithSuffix = 0;
    int asyncWithCancellationToken = 0;
    int asyncWithoutCancellationToken = 0;
    int asyncCancellationTokenNotLast = 0;
    int obsolete = 0;
    int editorBrowsableNever = 0;
    int publicFields = 0;
    int publicMutableProperties = 0;

    foreach (Type type in publicTypes)
    {
        foreach (string bucket in nameBuckets)
        {
            if (type.Name.Contains(bucket, StringComparison.Ordinal))
                buckets[bucket]++;
        }

        if (HasAttribute(type, "System.ObsoleteAttribute"))
            obsolete++;
        if (HasEditorBrowsableNever(type))
            editorBrowsableNever++;

        MemberInfo[] declaredMembers;
        try
        {
            declaredMembers = type.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not inspect public members of '{type.FullName}'.", exception);
        }

        foreach (MemberInfo member in declaredMembers)
        {
            if (member is MethodInfo { IsSpecialName: true })
                continue;

            members++;
            if (SignatureContainsDateTime(member))
                dateTimeSignatures.Add(MemberSignature(type, member));
            if (HasAttribute(member, "System.ObsoleteAttribute"))
                obsolete++;
            if (HasEditorBrowsableNever(member))
                editorBrowsableNever++;

            if ((type.FullName is "ViciOne.ServiceBus.ConsumeContext" or "ViciOne.ServiceBus.ConsumeContext`1") &&
                member is not MethodInfo { IsSpecialName: true })
                consumeContextMembers.Add(MemberSignature(type, member));

            if (member is MethodInfo method)
            {
                bool extensionMethod = method.IsStatic && HasAttribute(method, typeof(ExtensionAttribute).FullName!);
                if (extensionMethod)
                {
                    extensionMethods++;
                    extensionClasses.Add(type.FullName ?? type.Name);
                    string extensionNamespace = type.Namespace ?? string.Empty;
                    extensionMethodsPerNamespace[extensionNamespace] =
                        extensionMethodsPerNamespace.GetValueOrDefault(extensionNamespace) + 1;

                    ParameterInfo[] parameters = SafeParameters(method);
                    if (extensionNamespace == "ViciOne.ServiceBus")
                    {
                        string operationName = method.Name.EndsWith("Async", StringComparison.Ordinal)
                            ? method.Name[..^5]
                            : method.Name;
                        string shape = ExtensionShape(method, parameters);
                        if (operationName == "Send")
                            sendShapes.Add(shape);
                        else if (operationName == "Publish")
                            publishShapes.Add(shape);

                        if (parameters.Length > 0 && IsConsumeContext(parameters[0].ParameterType))
                            consumeContextCompletions.Add(MemberSignature(type, method));
                    }
                }

                bool delegateInvoke = type.BaseType?.FullName == "System.MulticastDelegate" && method.Name == "Invoke";
                if (!IsTaskLike(method.ReturnType) || delegateInvoke)
                    continue;

                asyncMethods++;
                if (method.Name.EndsWith("Async", StringComparison.Ordinal))
                    asyncWithSuffix++;
                else if (asyncNoSuffixSample.Count < 400)
                    asyncNoSuffixSample.Add($"{type.FullName}.{method.Name}");

                ParameterInfo[] asyncParameters = SafeParameters(method);
                int cancellationTokenIndex = Array.FindIndex(
                    asyncParameters,
                    parameter => parameter.ParameterType.FullName == "System.Threading.CancellationToken");
                bool hasCancellationToken = cancellationTokenIndex >= 0;
                if (hasCancellationToken)
                {
                    asyncWithCancellationToken++;
                    if (cancellationTokenIndex != asyncParameters.Length - 1)
                    {
                        asyncCancellationTokenNotLast++;
                        if (asyncCancellationTokenNotLastSample.Count < 400)
                            asyncCancellationTokenNotLastSample.Add($"{type.FullName}.{method.Name}");
                    }
                }
                else if (!IsCallbackMethod(type, method))
                {
                    asyncWithoutCancellationToken++;
                    if (asyncNoCancellationTokenSample.Count < 400)
                        asyncNoCancellationTokenSample.Add($"{type.FullName}.{method.Name}");
                }
            }
            else if (member is FieldInfo field && !field.IsLiteral && !type.IsEnum)
            {
                publicFields++;
            }
            else if (member is PropertyInfo property &&
                     property.SetMethod is { IsPublic: true } &&
                     !type.IsInterface)
            {
                publicMutableProperties++;
            }
        }
    }

    return new AssemblyInventory(
        assemblyName,
        publicTypes.Count,
        topLevelTypes.Count,
        publicTypes.Count - topLevelTypes.Count,
        namespaces.Count,
        publicTypes.Count(type => type.IsInterface),
        publicTypes.Count(type => type.IsClass && type.IsAbstract && type.IsSealed),
        publicTypes.Count(type => type.IsClass && type.IsAbstract && !type.IsSealed),
        publicTypes.Count(type => type.IsValueType && !type.IsEnum),
        publicTypes.Count(type => type.IsEnum),
        publicTypes.Count(type => type.BaseType?.FullName == "System.MulticastDelegate"),
        publicTypes.Count(type => type.IsGenericTypeDefinition),
        members,
        extensionMethods,
        extensionClasses.Count,
        asyncMethods,
        asyncWithSuffix,
        asyncMethods - asyncWithSuffix,
        asyncWithCancellationToken,
        asyncWithoutCancellationToken,
        asyncCancellationTokenNotLast,
        obsolete,
        editorBrowsableNever,
        publicFields,
        publicMutableProperties,
        topLevelTypes.Count(type => (type.Namespace ?? string.Empty).Contains(".Internals", StringComparison.Ordinal)),
        topLevelTypes.Count(type => type.Namespace == "ViciOne.ServiceBus"),
        extensionMethodsPerNamespace.GetValueOrDefault("ViciOne.ServiceBus"),
        dateTimeSignatures.Count,
        sendShapes.Count,
        publishShapes.Count,
        consumeContextMembers.Count,
        consumeContextCompletions.Count,
        buckets,
        extensionMethodsPerNamespace.OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Take(12)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
        namespaces.Order(StringComparer.Ordinal).ToArray(),
        asyncNoSuffixSample,
        asyncNoCancellationTokenSample,
        asyncCancellationTokenNotLastSample,
        dateTimeSignatures.Order(StringComparer.Ordinal).Take(400).ToArray());
}

ApplicationSurfaceInventory InventoryApplicationSurface(IReadOnlyCollection<Type> publicTypes)
{
    string[] sendReceivers =
    [
        "ViciOne.ServiceBus.ISendEndpoint",
        "ViciOne.ServiceBus.ISendEndpointProvider",
        "ViciOne.ServiceBus.ConsumeContext",
        "ViciOne.ServiceBus.ConsumeContext`1",
    ];
    string[] publishReceivers = ["ViciOne.ServiceBus.IPublishEndpoint"];
    var sendShapes = new HashSet<string>(StringComparer.Ordinal);
    var publishShapes = new HashSet<string>(StringComparer.Ordinal);
    var consumeMemberShapes = new HashSet<string>(StringComparer.Ordinal);
    var consumeExtensionShapes = new HashSet<string>(StringComparer.Ordinal);

    Type consumeContext = publicTypes.Single(type => type.FullName == "ViciOne.ServiceBus.ConsumeContext`1");
    IReadOnlyCollection<Type> consumeContextClosure = InterfaceClosure(consumeContext);

    foreach (Type contract in consumeContextClosure)
    {
        foreach (MemberInfo member in contract.GetMembers(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (member is MethodInfo { IsSpecialName: true })
                continue;
            if (member.MemberType is MemberTypes.Method or MemberTypes.Property or MemberTypes.Event)
                consumeMemberShapes.Add(CompletionShape(member));
        }
    }

    foreach (Type type in publicTypes)
    {
        foreach (MethodInfo method in type.GetMethods(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName)
                continue;

            ParameterInfo[] parameters = SafeParameters(method);
            bool extensionMethod = method.IsStatic && HasAttribute(method, typeof(ExtensionAttribute).FullName!);
            string operationName = method.Name.EndsWith("Async", StringComparison.Ordinal)
                ? method.Name[..^5]
                : method.Name;

            if (type.Namespace == "ViciOne.ServiceBus")
            {
                if (!extensionMethod && type.FullName == "ViciOne.ServiceBus.ISendEndpoint" && operationName == "Send")
                    sendShapes.Add(OperationShape(method, parameters, skipReceiver: false));
                else if (!extensionMethod && type.FullName == "ViciOne.ServiceBus.IPublishEndpoint" && operationName == "Publish")
                    publishShapes.Add(OperationShape(method, parameters, skipReceiver: false));
                else if (extensionMethod && parameters.Length > 0)
                {
                    string receiver = GenericDefinitionName(parameters[0].ParameterType);
                    if (operationName == "Send" && sendReceivers.Contains(receiver, StringComparer.Ordinal))
                        sendShapes.Add(OperationShape(method, parameters, skipReceiver: true));
                    else if (operationName == "Publish" && publishReceivers.Contains(receiver, StringComparer.Ordinal))
                        publishShapes.Add(OperationShape(method, parameters, skipReceiver: true));

                    if (IsExtensionApplicable(parameters[0].ParameterType, consumeContextClosure))
                        consumeExtensionShapes.Add(CompletionShape(method, skipReceiver: true));
                }
            }
        }
    }

    var consumeCompletions = new HashSet<string>(consumeMemberShapes, StringComparer.Ordinal);
    consumeCompletions.UnionWith(consumeExtensionShapes);

    return new ApplicationSurfaceInventory(
        sendShapes.Count,
        publishShapes.Count,
        consumeMemberShapes.Count,
        consumeCompletions.Count,
        sendShapes.Order(StringComparer.Ordinal).ToArray(),
        publishShapes.Order(StringComparer.Ordinal).ToArray(),
        consumeMemberShapes.Order(StringComparer.Ordinal).ToArray(),
        consumeExtensionShapes.Order(StringComparer.Ordinal).ToArray());
}

static IReadOnlyCollection<Type> InterfaceClosure(Type type)
{
    var closure = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        [TypeSignature(type)] = type,
    };
    foreach (Type contract in type.GetInterfaces())
        closure.TryAdd(TypeSignature(contract), contract);
    return closure.Values;
}

static bool IsExtensionApplicable(Type receiver, IReadOnlyCollection<Type> targetClosure)
{
    if (receiver.IsByRef)
        receiver = receiver.GetElementType() ?? receiver;

    if (receiver.IsGenericParameter)
    {
        Type[] constraints = receiver.GetGenericParameterConstraints();
        return constraints.Length == 0 || constraints.All(constraint => IsExtensionApplicable(constraint, targetClosure));
    }

    if (receiver.FullName == "System.Object")
        return true;

    string receiverDefinition = GenericDefinitionName(receiver);
    foreach (Type candidate in targetClosure)
    {
        if (receiverDefinition == GenericDefinitionName(candidate))
            return true;
        try
        {
            if (receiver.IsAssignableFrom(candidate))
                return true;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Could not determine whether extension receiver '{receiver}' applies to '{candidate}'.",
                exception);
        }
    }
    return false;
}

static string GenericDefinitionName(Type type)
{
    if (type.IsByRef)
        type = type.GetElementType() ?? type;
    return type.IsGenericType
        ? type.GetGenericTypeDefinition().FullName ?? type.GetGenericTypeDefinition().Name
        : type.FullName ?? type.Name;
}

static string OperationShape(MethodInfo method, ParameterInfo[] parameters, bool skipReceiver)
{
    string operationName = method.Name.EndsWith("Async", StringComparison.Ordinal)
        ? method.Name[..^5]
        : method.Name;
    IEnumerable<ParameterInfo> operationParameters = skipReceiver ? parameters.Skip(1) : parameters;
    return $"{operationName}`{method.GetGenericArguments().Length}({string.Join(",", operationParameters.Select(parameter => TypeSignature(parameter.ParameterType)))})";
}

static string CompletionShape(MemberInfo member, bool skipReceiver = false) => member switch
{
    MethodInfo method => $"method:{method.Name}`{method.GetGenericArguments().Length}({string.Join(",", SafeParameters(method).Skip(skipReceiver ? 1 : 0).Select(parameter => TypeSignature(parameter.ParameterType)))})",
    PropertyInfo property => $"property:{property.Name}:{TypeSignature(property.PropertyType)}",
    EventInfo eventInfo => $"event:{eventInfo.Name}:{TypeSignature(eventInfo.EventHandlerType)}",
    _ => $"{member.MemberType}:{member.Name}",
};

bool IsCallbackMethod(Type type, MethodInfo method)
{
    if (type.IsInterface)
        return IsCallbackContract(type);

    foreach (Type contract in type.GetInterfaces().Where(IsCallbackContract))
    {
        MethodInfo[] contractMethods;
        try
        {
            contractMethods = contract.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Could not inspect callback contract '{contract}' on '{type.FullName}'.",
                exception);
        }

        if (contractMethods.Any(contractMethod => SameMethodShape(contractMethod, method)))
            return true;
    }

    return false;
}

bool IsCallbackContract(Type type)
{
    if (!type.IsInterface)
        return false;

    return callbackContracts.Contains(GenericDefinitionName(type));
}

static bool SameMethodShape(MethodInfo contractMethod, MethodInfo implementationMethod)
{
    if (contractMethod.Name != implementationMethod.Name ||
        contractMethod.GetGenericArguments().Length != implementationMethod.GetGenericArguments().Length)
        return false;

    ParameterInfo[] contractParameters = SafeParameters(contractMethod);
    ParameterInfo[] implementationParameters = SafeParameters(implementationMethod);
    if (contractParameters.Length != implementationParameters.Length)
        return false;

    return contractParameters
        .Select(parameter => parameter.ParameterType)
        .Zip(implementationParameters.Select(parameter => parameter.ParameterType), SameTypeShape)
        .All(matches => matches);
}

static bool SameTypeShape(Type left, Type right)
{
    if (left.IsByRef != right.IsByRef || left.IsPointer != right.IsPointer || left.IsArray != right.IsArray)
        return false;

    if (left.IsByRef || left.IsPointer)
        return SameTypeShape(left.GetElementType()!, right.GetElementType()!);

    if (left.IsArray)
        return left.GetArrayRank() == right.GetArrayRank() &&
               SameTypeShape(left.GetElementType()!, right.GetElementType()!);

    if (left.IsGenericParameter || right.IsGenericParameter)
    {
        return left.IsGenericParameter && right.IsGenericParameter &&
               left.GenericParameterPosition == right.GenericParameterPosition &&
               (left.DeclaringMethod is null) == (right.DeclaringMethod is null);
    }

    if (left.IsGenericType || right.IsGenericType)
    {
        if (!left.IsGenericType || !right.IsGenericType || GenericDefinitionName(left) != GenericDefinitionName(right))
            return false;

        return left.GetGenericArguments()
            .Zip(right.GetGenericArguments(), SameTypeShape)
            .All(matches => matches);
    }

    return left.FullName == right.FullName;
}

static bool IsTaskLike(Type returnType)
{
    string? name = returnType.IsGenericType
        ? returnType.GetGenericTypeDefinition().FullName
        : returnType.FullName;
    return name is "System.Threading.Tasks.Task" or
        "System.Threading.Tasks.Task`1" or
        "System.Threading.Tasks.ValueTask" or
        "System.Threading.Tasks.ValueTask`1";
}

static bool IsConsumeContext(Type type)
{
    Type candidate = type.IsByRef ? type.GetElementType() ?? type : type;
    if (candidate.FullName is "ViciOne.ServiceBus.ConsumeContext" or "ViciOne.ServiceBus.ConsumeContext`1")
        return true;
    return candidate.IsGenericType && candidate.GetGenericTypeDefinition().FullName == "ViciOne.ServiceBus.ConsumeContext`1";
}

static string ExtensionShape(MethodInfo method, ParameterInfo[] parameters)
{
    string operationName = method.Name.EndsWith("Async", StringComparison.Ordinal)
        ? method.Name[..^5]
        : method.Name;
    string parameterShape = string.Join(",", parameters.Skip(1).Select(parameter => TypeSignature(parameter.ParameterType)));
    return $"{operationName}`{method.GetGenericArguments().Length}({parameterShape})";
}

static ParameterInfo[] SafeParameters(MethodBase method)
{
    return method.GetParameters();
}

static bool SignatureContainsDateTime(MemberInfo member) => member switch
{
    MethodInfo method => ContainsDateTime(method.ReturnType) || SafeParameters(method).Any(parameter => ContainsDateTime(parameter.ParameterType)),
    ConstructorInfo constructor => SafeParameters(constructor).Any(parameter => ContainsDateTime(parameter.ParameterType)),
    PropertyInfo property => ContainsDateTime(property.PropertyType) || property.GetIndexParameters().Any(parameter => ContainsDateTime(parameter.ParameterType)),
    FieldInfo field => ContainsDateTime(field.FieldType),
    EventInfo eventInfo => eventInfo.EventHandlerType is not null && ContainsDateTime(eventInfo.EventHandlerType),
    _ => false,
};

static bool ContainsDateTime(Type type)
{
    if (type.IsByRef || type.IsPointer || type.IsArray)
        return type.GetElementType() is { } elementType && ContainsDateTime(elementType);
    if (type.FullName == "System.DateTime")
        return true;
    if (!type.IsGenericType)
        return false;
    return type.GetGenericArguments().Any(ContainsDateTime);
}

static string MemberSignature(Type declaringType, MemberInfo member) => member switch
{
    MethodInfo method => $"{declaringType.FullName}.{method.Name}({string.Join(",", SafeParameters(method).Select(parameter => TypeSignature(parameter.ParameterType)))})",
    ConstructorInfo constructor => $"{declaringType.FullName}.ctor({string.Join(",", SafeParameters(constructor).Select(parameter => TypeSignature(parameter.ParameterType)))})",
    PropertyInfo property => $"{declaringType.FullName}.{property.Name}:{TypeSignature(property.PropertyType)}",
    FieldInfo field => $"{declaringType.FullName}.{field.Name}:{TypeSignature(field.FieldType)}",
    EventInfo eventInfo => $"{declaringType.FullName}.{eventInfo.Name}:{TypeSignature(eventInfo.EventHandlerType)}",
    _ => $"{declaringType.FullName}.{member.Name}",
};

static string TypeSignature(Type? type)
{
    if (type is null)
        return string.Empty;
    if (type.IsByRef)
        return TypeSignature(type.GetElementType()) + "&";
    if (type.IsArray)
        return TypeSignature(type.GetElementType()) + "[]";
    if (!type.IsGenericType)
        return type.FullName ?? type.Name;
    return $"{type.GetGenericTypeDefinition().FullName}[{string.Join(",", type.GetGenericArguments().Select(TypeSignature))}]";
}

static bool HasAttribute(MemberInfo member, string fullName)
{
    return member.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == fullName);
}

static bool HasEditorBrowsableNever(MemberInfo member)
{
    CustomAttributeData? attribute = member.GetCustomAttributesData()
        .FirstOrDefault(item => item.AttributeType.FullName == "System.ComponentModel.EditorBrowsableAttribute");
    if (attribute is null)
        return false;
    return attribute.ConstructorArguments.Count == 1 &&
           Convert.ToInt32(attribute.ConstructorArguments[0].Value, System.Globalization.CultureInfo.InvariantCulture) == 1;
}

static string TypeKind(Type type) => type.IsInterface
    ? "interface"
    : type.IsEnum
        ? "enum"
        : type.IsValueType
            ? "struct"
            : type.IsAbstract && type.IsSealed
                ? "static"
                : type.IsAbstract
                    ? "abstract"
                    : "class";

internal sealed record AssemblyInventory(
    string Assembly,
    int PublicTypes,
    int TopLevelPublicTypes,
    int NestedPublicTypes,
    int Namespaces,
    int Interfaces,
    int StaticClasses,
    int AbstractClasses,
    int Structs,
    int Enums,
    int Delegates,
    int GenericTypeDefinitions,
    int PublicMembersDeclared,
    int ExtensionMethods,
    int ExtensionClasses,
    int AsyncMethods,
    int AsyncWithAsyncSuffix,
    int AsyncWithoutSuffix,
    int AsyncWithCancellationToken,
    int AsyncWithoutCancellationToken,
    int AsyncCancellationTokenNotLast,
    int Obsolete,
    int EditorBrowsableNever,
    int PublicFields,
    int PublicMutableProperties,
    int TypesInInternalsNamespace,
    int RootNamespacePublicTypes,
    int ExtensionMethodsInRootNamespace,
    int DateTimeInSignatures,
    int SendShapes,
    int PublishShapes,
    int ConsumeContextMembers,
    int ConsumeContextCompletions,
    IReadOnlyDictionary<string, int> NameBuckets,
    IReadOnlyDictionary<string, int> ExtensionMethodsPerNamespace,
    IReadOnlyList<string> NamespaceList,
    IReadOnlyList<string> AsyncNoSuffixSample,
    IReadOnlyList<string> AsyncNoCancellationTokenSample,
    IReadOnlyList<string> AsyncCancellationTokenNotLastSample,
    IReadOnlyList<string> DateTimeSignatureSample);

internal sealed record AggregateInventory(
    int PublicTypes,
    int TopLevelPublicTypes,
    int NestedPublicTypes,
    int Interfaces,
    int ExtensionMethods,
    int AsyncMethods,
    int AsyncWithoutSuffix,
    int AsyncWithoutCancellationToken,
    int AsyncCancellationTokenNotLast,
    int DateTimeInSignatures,
    int TypesInInternalsNamespace,
    int Obsolete,
    int EditorBrowsableNever,
    int ExtensionMethodsInRootNamespace,
    int RootNamespacePublicTypes,
    int SendShapes,
    int PublishShapes,
    int ConsumeContextMembers,
    int ConsumeContextCompletions);

internal sealed record ApplicationSurfaceInventory(
    int SendShapes,
    int PublishShapes,
    int ConsumeContextMembers,
    int ConsumeContextCompletions,
    IReadOnlyList<string> SendShapeSignatures,
    IReadOnlyList<string> PublishShapeSignatures,
    IReadOnlyList<string> ConsumeContextMemberSignatures,
    IReadOnlyList<string> ConsumeContextExtensionSignatures);

internal sealed record TypeInventory(string Assembly, string Namespace, string Name, string Kind);
