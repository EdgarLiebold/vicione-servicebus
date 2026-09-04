using System.Text;
using System.Text.RegularExpressions;

bool apply = args.Contains("--apply", StringComparer.Ordinal);

string repositoryRoot = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(repositoryRoot, "ViciOne.ServiceBus.slnx")))
    throw new InvalidOperationException("Run this tool from the ViciOne.ServiceBus repository root.");

(string Old, string New)[] replacements =
[
    ("ViciOne.ServiceBus.RabbitMqTransport.Testing", "ViciOne.ServiceBus.RabbitMq.Testing"),
    ("ViciOne.ServiceBus.EventHubIntegration.Testing", "ViciOne.ServiceBus.EventHubs.Testing"),
    ("ViciOne.ServiceBus.EventHub.Testing", "ViciOne.ServiceBus.EventHubs.Testing"),
    ("ViciOne.ServiceBus.Azure.ServiceBus.Testing", "ViciOne.ServiceBus.AzureServiceBus.Testing"),
    ("ViciOne.ServiceBus.EntityFrameworkCoreIntegration", "ViciOne.ServiceBus.EntityFrameworkCore"),
    ("ViciOne.ServiceBus.DynamoDbIntegration", "ViciOne.ServiceBus.DynamoDb"),
    ("ViciOne.ServiceBus.RabbitMqTransport", "ViciOne.ServiceBus.RabbitMq"),
    ("ViciOne.ServiceBus.ActiveMqTransport", "ViciOne.ServiceBus.ActiveMq"),
    ("ViciOne.ServiceBus.AmazonSqsTransport", "ViciOne.ServiceBus.AmazonSqs"),
    ("ViciOne.ServiceBus.Azure.ServiceBus.Core", "ViciOne.ServiceBus.AzureServiceBus"),
    ("ViciOne.ServiceBus.EventHubIntegration", "ViciOne.ServiceBus.EventHubs"),
    ("ViciOne.ServiceBus.QuartzIntegration", "ViciOne.ServiceBus.Quartz"),
    ("ViciOne.ServiceBus.RabbitMQ", "ViciOne.ServiceBus.RabbitMq"),
    ("ViciOne.ServiceBus.ActiveMQ", "ViciOne.ServiceBus.ActiveMq"),
    ("ViciOne.ServiceBus.AmazonSQS", "ViciOne.ServiceBus.AmazonSqs"),
    ("ViciOne.ServiceBus.SqlTransport.PostgreSQL", "ViciOne.ServiceBus.SqlTransport.PostgreSql"),
];

HashSet<string> textExtensions = new(StringComparer.OrdinalIgnoreCase)
{
    ".cs", ".csproj", ".props", ".targets", ".sln", ".slnx", ".json", ".md", ".txt",
    ".xml", ".config", ".editorconfig", ".sh", ".ps1", ".yml", ".yaml",
};
string[] excludedSegments = [".git", "artifacts", "bin", "obj", "evidence", "review"];

bool IsExcluded(string path)
{
    string relative = Path.GetRelativePath(repositoryRoot, path);
    return relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
        .Any(segment => excludedSegments.Contains(segment, StringComparer.Ordinal));
}

string ReplaceKnownNames(string value)
{
    foreach ((string oldValue, string newValue) in replacements)
        value = value.Replace(oldValue, newValue, StringComparison.Ordinal);

    value = value
        .Replace("<PackageId>ViciOne.ServiceBus.EventHub</PackageId>", "<PackageId>ViciOne.ServiceBus.EventHubs</PackageId>", StringComparison.Ordinal)
        .Replace("[ViciOne.ServiceBus.EventHub]", "[ViciOne.ServiceBus.EventHubs]", StringComparison.Ordinal)
        .Replace("\"ViciOne.ServiceBus.EventHub\"", "\"ViciOne.ServiceBus.EventHubs\"", StringComparison.Ordinal);

    return value;
}

HashSet<string> applicationFiles = new(StringComparer.Ordinal)
{
    "src/ViciOne.ServiceBus.Abstractions/IBus.cs",
    "src/ViciOne.ServiceBus.Abstractions/IBusControl.cs",
    "src/ViciOne.ServiceBus.Abstractions/ISendEndpoint.cs",
    "src/ViciOne.ServiceBus.Abstractions/ISendEndpointProvider.cs",
    "src/ViciOne.ServiceBus.Abstractions/IPublishEndpoint.cs",
    "src/ViciOne.ServiceBus.Abstractions/IConsumer.cs",
    "src/ViciOne.ServiceBus.Abstractions/SendOptions.cs",
    "src/ViciOne.ServiceBus.Abstractions/PublishOptions.cs",
    "src/ViciOne.ServiceBus.Abstractions/RequestOptions.cs",
    "src/ViciOne.ServiceBus.Abstractions/ScheduleOptions.cs",
    "src/ViciOne.ServiceBus.Abstractions/MessageHeaders.cs",
    "src/ViciOne.ServiceBus.Abstractions/Serialization/Admission/PayloadAdmissionException.cs",
    "src/ViciOne.ServiceBus.Abstractions/Diagnostics/Redaction/SensitiveMemberAttribute.cs",
    "src/ViciOne.ServiceBus.Abstractions/Diagnostics/Redaction/SensitivePayloadAttribute.cs",
    "src/ViciOne.ServiceBus.Abstractions/Diagnostics/Redaction/MessagePayloadSensitivity.cs",
    "src/ViciOne.ServiceBus.Abstractions/Diagnostics/Redaction/MessageSensitivityDescriptor.cs",
    "src/ViciOne.ServiceBus.Abstractions/Clients/IRequestClient.cs",
    "src/ViciOne.ServiceBus.Abstractions/Clients/Response.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contexts/ConsumeContext.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contexts/IOutgoingMessages.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/IDurableSender.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendReceipt.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendOptions.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendId.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendCapacityExceededException.cs",
    "src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendIdentityConflictException.cs",
    "src/ViciOne.ServiceBus.Abstractions/Scheduling/IMessageScheduler.cs",
    "src/ViciOne.ServiceBus.Abstractions/Scheduling/ScheduledMessage.cs",
};

HashSet<string> advancedContractFiles = new(StringComparer.Ordinal)
{
    "src/ViciOne.ServiceBus.Abstractions/Contracts/Batch.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/BatchCompletionMode.cs",
};

HashSet<string> configurationEntryPointFiles = new(StringComparer.Ordinal)
{
    "src/ViciOne.ServiceBus/InMemoryTransport/InMemoryConfigurationExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.ActiveMq/Configuration/ActiveMqBusFactoryConfiguratorExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.AmazonSqs/Configuration/AmazonSqsBusFactoryConfiguratorExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.AzureServiceBus/AzureBusFactory.cs",
    "src/Transports/ViciOne.ServiceBus.AzureServiceBus/Configuration/ServiceBusConfigurationExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.EventHubs/EventHubIntegrationExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.RabbitMq/Configuration/RabbitMqBusFactoryConfiguratorExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/Configuration/PostgresBusFactoryConfiguratorExtensions.cs",
    "src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/Configuration/SqlServerBusFactoryConfiguratorExtensions.cs",
};

HashSet<string> observerContractFiles = new(StringComparer.Ordinal)
{
    "src/ViciOne.ServiceBus.Abstractions/Contracts/BusReady.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/HostReady.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveEndpointCompleted.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveEndpointEvent.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveEndpointFaulted.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveEndpointReady.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveEndpointStopping.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveTransportCompleted.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveTransportEvent.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveTransportFaulted.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/ReceiveTransportReady.cs",
    "src/ViciOne.ServiceBus.Abstractions/Contracts/RiderReady.cs",
};

string? TargetNamespace(string file)
{
    string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
    if (!relative.EndsWith(".cs", StringComparison.Ordinal) || applicationFiles.Contains(relative))
        return null;

    if (configurationEntryPointFiles.Contains(relative))
        return "ViciOne.ServiceBus.Configuration";

    if (advancedContractFiles.Contains(relative))
        return "ViciOne.ServiceBus.Advanced";

    if (observerContractFiles.Contains(relative))
        return "ViciOne.ServiceBus.Advanced.Observers";

    if (relative.StartsWith("src/ViciOne.ServiceBus.Abstractions/Exceptions/", StringComparison.Ordinal)
        || relative.StartsWith("src/ViciOne.ServiceBus.Abstractions/Attributes/", StringComparison.Ordinal)
        || relative.StartsWith("src/ViciOne.ServiceBus.Abstractions/Contracts/", StringComparison.Ordinal))
        return null;

    if (relative.StartsWith("src/ViciOne.ServiceBus.Abstractions/", StringComparison.Ordinal))
    {
        string local = relative["src/ViciOne.ServiceBus.Abstractions/".Length..];
        return local switch
        {
            _ when local.StartsWith("Configuration/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Configuration",
            _ when local.StartsWith("Middleware/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Middleware",
            _ when local.StartsWith("Serialization/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Serialization",
            _ when local.StartsWith("Topology/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Topology",
            _ when local.StartsWith("Observers/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Observers",
            _ when local.StartsWith("Initializers/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Initializers",
            _ when local.StartsWith("Consumers/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Registration",
            _ when local.StartsWith("Transports/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Providers.Transports",
            _ when local.StartsWith("DurableSend/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Operations",
            _ when local.StartsWith("Diagnostics/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Operations",
            _ when local.StartsWith("MessageData/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Serialization",
            _ when local.StartsWith("Courier/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Courier",
            _ when local.StartsWith("Saga", StringComparison.Ordinal) => "ViciOne.ServiceBus.Sagas",
            _ when local.StartsWith("Futures/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Futures",
            _ when local.StartsWith("JobService/", StringComparison.Ordinal) => "ViciOne.ServiceBus.JobService",
            _ when local.StartsWith("Scheduling/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced",
            _ => "ViciOne.ServiceBus.Advanced",
        };
    }

    if (relative.StartsWith("src/ViciOne.ServiceBus/", StringComparison.Ordinal))
    {
        string local = relative["src/ViciOne.ServiceBus/".Length..];
        return local switch
        {
            _ when local.StartsWith("Configuration/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Configuration",
            _ when local.StartsWith("DependencyInjection/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Registration",
            _ when local.StartsWith("Middleware/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Middleware",
            _ when local.StartsWith("Serialization/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Serialization",
            _ when local.StartsWith("Topology/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Topology",
            _ when local.StartsWith("Initializers/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Initializers",
            _ when local.StartsWith("Consumers/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Registration",
            _ when local.StartsWith("Transports/", StringComparison.Ordinal)
                || local.StartsWith("InMemoryTransport/", StringComparison.Ordinal)
                || local.StartsWith("SqlTransport/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Providers.Transports",
            _ when local.StartsWith("MessageJournal/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Providers.Persistence",
            _ when local.StartsWith("DurableSend/", StringComparison.Ordinal)
                || local.StartsWith("Diagnostics/", StringComparison.Ordinal)
                || local.StartsWith("Monitoring/", StringComparison.Ordinal)
                || local.StartsWith("Logging/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Operations",
            _ when local.StartsWith("MessageData/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced.Serialization",
            _ when local.StartsWith("Courier/", StringComparison.Ordinal) || local.StartsWith("RoutingSlip", StringComparison.Ordinal) => "ViciOne.ServiceBus.Courier",
            _ when local.StartsWith("Saga", StringComparison.Ordinal) => "ViciOne.ServiceBus.Sagas",
            _ when local.StartsWith("Futures/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Futures",
            _ when local.StartsWith("JobService/", StringComparison.Ordinal) => "ViciOne.ServiceBus.JobService",
            _ when local.StartsWith("Mediator/", StringComparison.Ordinal) => "ViciOne.ServiceBus.Mediator",
            _ when local.StartsWith("Scheduling/", StringComparison.Ordinal)
                || local.Contains("Scheduler", StringComparison.Ordinal) => "ViciOne.ServiceBus.Advanced",
            _ when local.Contains("Health", StringComparison.Ordinal)
                || local.StartsWith("Introspection", StringComparison.Ordinal) => "ViciOne.ServiceBus.Operations",
            _ when local is "ViciOneServiceBusBus.cs" or "ViciOneServiceBusHostedService.cs" => null,
            _ => "ViciOne.ServiceBus.Advanced",
        };
    }

    (string Prefix, string Namespace)[] canonicalProjects =
    [
        ("src/Transports/ViciOne.ServiceBus.RabbitMq/", "ViciOne.ServiceBus.RabbitMq"),
        ("src/Transports/ViciOne.ServiceBus.RabbitMq.Testing/", "ViciOne.ServiceBus.RabbitMq.Testing"),
        ("src/Transports/ViciOne.ServiceBus.ActiveMq/", "ViciOne.ServiceBus.ActiveMq"),
        ("src/Transports/ViciOne.ServiceBus.AmazonSqs/", "ViciOne.ServiceBus.AmazonSqs"),
        ("src/Transports/ViciOne.ServiceBus.AzureServiceBus/", "ViciOne.ServiceBus.AzureServiceBus"),
        ("src/Transports/ViciOne.ServiceBus.AzureServiceBus.Testing/", "ViciOne.ServiceBus.AzureServiceBus.Testing"),
        ("src/Transports/ViciOne.ServiceBus.EventHubs/", "ViciOne.ServiceBus.EventHubs"),
        ("src/Transports/ViciOne.ServiceBus.EventHubs.Testing/", "ViciOne.ServiceBus.EventHubs.Testing"),
        ("src/Transports/ViciOne.ServiceBus.SqlTransport.PostgreSql/", "ViciOne.ServiceBus.SqlTransport.PostgreSql"),
        ("src/Transports/ViciOne.ServiceBus.SqlTransport.SqlServer/", "ViciOne.ServiceBus.SqlTransport.SqlServer"),
        ("src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/", "ViciOne.ServiceBus.EntityFrameworkCore"),
        ("src/Persistence/ViciOne.ServiceBus.DynamoDb/", "ViciOne.ServiceBus.DynamoDb"),
        ("src/Persistence/ViciOne.ServiceBus.Azure.Table/", "ViciOne.ServiceBus.Azure.Table"),
        ("src/Persistence/ViciOne.ServiceBus.Azure.Storage/", "ViciOne.ServiceBus.Azure.Storage"),
        ("src/Persistence/ViciOne.ServiceBus.AmazonS3/", "ViciOne.ServiceBus.AmazonS3"),
        ("src/Scheduling/ViciOne.ServiceBus.Quartz/", "ViciOne.ServiceBus.Quartz"),
        ("src/ViciOne.ServiceBus.MessagePack/", "ViciOne.ServiceBus.MessagePack"),
    ];
    foreach ((string prefix, string targetNamespace) in canonicalProjects)
    {
        if (relative.StartsWith(prefix, StringComparison.Ordinal))
            return targetNamespace;
    }

    return null;
}

string MoveRootNamespace(string file, string value)
{
    string relative = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
    string? targetNamespace = TargetNamespace(file);
    if (targetNamespace == null)
        return value;

    if (configurationEntryPointFiles.Contains(relative))
    {
        return new Regex(
                @"^namespace\s+[A-Za-z_][A-Za-z0-9_.]*\s*;",
                RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .Replace(value, $"namespace {targetNamespace};", count: 1);
    }

    return value
        .Replace("namespace ViciOne.ServiceBus;", $"namespace {targetNamespace};", StringComparison.Ordinal)
        .Replace("namespace ViciOne.ServiceBus\n{", $"namespace {targetNamespace}\n{{", StringComparison.Ordinal)
        .Replace("namespace ViciOne.ServiceBus\r\n{", $"namespace {targetNamespace}\r\n{{", StringComparison.Ordinal);
}

string migrationToolPath = Path.Combine(repositoryRoot, "tools", "api-conventions", "MoveNamespaces.cs");
int rewrittenFiles = 0;
foreach (string file in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
{
    if (IsExcluded(file)
        || string.Equals(file, migrationToolPath, StringComparison.Ordinal)
        || !textExtensions.Contains(Path.GetExtension(file)))
        continue;

    string original = File.ReadAllText(file, Encoding.UTF8);
    string rewritten = MoveRootNamespace(file, ReplaceKnownNames(original));
    if (string.Equals(original, rewritten, StringComparison.Ordinal))
        continue;

    rewrittenFiles++;
    if (!apply)
        Console.WriteLine($"Would rewrite: {Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/')}");
    if (apply)
        File.WriteAllText(file, rewritten, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

int renamedEntries = 0;
string[] entries = Directory.EnumerateFileSystemEntries(repositoryRoot, "*", SearchOption.AllDirectories)
    .Where(path => !IsExcluded(path))
    .OrderByDescending(path => path.Count(character => character == Path.DirectorySeparatorChar))
    .ThenByDescending(static path => path.Length)
    .ToArray();

foreach (string originalPath in entries)
{
    if (!File.Exists(originalPath) && !Directory.Exists(originalPath))
        continue;

    string name = Path.GetFileName(originalPath);
    string canonicalName = ReplaceKnownNames(name);
    if (string.Equals(name, canonicalName, StringComparison.Ordinal))
        continue;

    string destination = Path.Combine(Path.GetDirectoryName(originalPath)!, canonicalName);
    if (!apply)
        Console.WriteLine($"Would rename: {Path.GetRelativePath(repositoryRoot, originalPath).Replace('\\', '/')} -> {canonicalName}");
    if (apply && (File.Exists(destination) || Directory.Exists(destination)))
        throw new IOException($"Cannot rename '{originalPath}' because '{destination}' already exists.");

    renamedEntries++;
    if (apply)
    {
        if (File.Exists(originalPath))
            File.Move(originalPath, destination);
        else
            Directory.Move(originalPath, destination);
    }
}

Console.WriteLine(apply ? "Canonical migration applied." : "Dry run only. Pass --apply to rewrite names.");
Console.WriteLine($"Rewritten files: {rewrittenFiles}");
Console.WriteLine($"Renamed entries: {renamedEntries}");
return 0;
