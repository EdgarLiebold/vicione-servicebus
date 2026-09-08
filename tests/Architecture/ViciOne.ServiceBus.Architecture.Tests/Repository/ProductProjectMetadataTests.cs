using System.Xml.Linq;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Verifies evaluated identity, packaging, signing, and reference-path rules for every product project.</summary>
public sealed class ProductProjectMetadataTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedPackageDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ViciOne.ServiceBus"] =
                "Core messaging runtime, application composition, routing, reliability, and observability for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Abstractions"] =
                "Public messaging contracts, contexts, topology abstractions, and pipeline primitives for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.ActiveMq"] =
                "ActiveMQ and AMQP transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.AmazonS3"] =
                "Amazon S3 message-data persistence for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.AmazonSqs"] =
                "Amazon SQS queue and SNS topic transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Analyzers"] =
                "Compiler diagnostics and code fixes that enforce ViciOne.ServiceBus API conventions.",
            ["ViciOne.ServiceBus.Azure.Storage"] =
                "Azure Blob Storage message-data persistence for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Azure.Table"] =
                "Azure Table persistence for ViciOne.ServiceBus sagas and bounded message journals.",
            ["ViciOne.ServiceBus.AzureServiceBus"] =
                "Azure Service Bus queue, topic, subscription, and session transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.AzureServiceBus.Testing"] =
                "Azure Service Bus-specific transport test harness APIs for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Courier"] =
                "Routing-slip activities, compensation, and orchestration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.DynamoDb"] =
                "Amazon DynamoDB saga persistence for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.EntityFrameworkCore"] =
                "Entity Framework Core outbox, inbox, and durable messaging persistence for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.EntityFrameworkCore.Sagas"] =
                "Entity Framework Core saga, future, and job persistence for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.EventHubs"] =
                "Azure Event Hubs producer and consumer integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.EventHubs.Testing"] =
                "Azure Event Hubs-specific producer and consumer test harness APIs for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Futures"] =
                "State-machine-backed future requests and durable result routing for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Initializers"] =
                "Convention-based message construction and property initialization for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.JobService"] =
                "Distributed job submission, execution, retry, and scheduling for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Mediator"] =
                "In-process request, send, and publish mediation through ViciOne.ServiceBus pipelines.",
            ["ViciOne.ServiceBus.MessagePack"] =
                "MessagePack envelope serialization for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Quartz"] =
                "Quartz.NET scheduling and recurring-message integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.RabbitMq"] =
                "RabbitMQ exchange, queue, and publisher-confirm transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.RabbitMq.Testing"] =
                "RabbitMQ-specific transport test harness APIs for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.Sagas"] =
                "Saga persistence contracts and state-machine execution for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.SignalR"] =
                "SignalR scale-out backplane integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.SqlTransport.PostgreSql"] =
                "PostgreSQL-backed queue and topic transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.SqlTransport.SqlServer"] =
                "SQL Server-backed queue and topic transport integration for ViciOne.ServiceBus.",
            ["ViciOne.ServiceBus.StateMachineVisualizer"] =
                "Generates Graphviz DOT and Mermaid diagrams from ViciOne ServiceBus state machines.",
            ["ViciOne.ServiceBus.Testing"] =
                "Transport-independent test harness and test-support APIs for ViciOne.ServiceBus applications.",
        };

    private const string AnalyzerAssemblyProject =
        "src/ViciOne.ServiceBus.Analyzers/ViciOne.ServiceBus.Analyzers.csproj";

    private const string AnalyzerPackageProject =
        "src/ViciOne.ServiceBus.Analyzers.Package/ViciOne.ServiceBus.Analyzers.Package.csproj";

    [Fact]
    public void EveryProductProject_HasCanonicalArtifactIdentity()
    {
        Assert.NotEmpty(RepositoryLayout.ProductProjects);

        foreach (var project in RepositoryLayout.ProductProjects)
        {
            var projectName = Path.GetFileNameWithoutExtension(project);
            var relativePath = RepositoryLayout.RelativeToRoot(project);

            Assert.Equal(projectName, MsBuildEvaluation.PropertyOf(project, "AssemblyName"));

            if (relativePath == AnalyzerPackageProject)
            {
                Assert.False(BooleanPropertyOf(project, "IncludeBuildOutput"));
                Assert.Equal("ViciOne.ServiceBus.Analyzers", MsBuildEvaluation.PropertyOf(project, "PackageId"));
            }
            else if (relativePath == AnalyzerAssemblyProject)
            {
                Assert.False(BooleanPropertyOf(project, "IsPackable"));
                Assert.Equal("ViciOne.ServiceBus.Analyzers.Assembly", MsBuildEvaluation.PropertyOf(project, "PackageId"));
            }
            else
            {
                Assert.Equal(projectName, MsBuildEvaluation.PropertyOf(project, "PackageId"));
            }
        }
    }

    [Fact]
    public void EveryProductProject_HasAUniqueEvaluatedPackageIdentity()
    {
        string[] packageIds = RepositoryLayout.ProductProjects
            .Select(project => MsBuildEvaluation.PropertyOf(project, "PackageId"))
            .ToArray();

        Assert.DoesNotContain(packageIds, string.IsNullOrWhiteSpace);
        Assert.Equal(packageIds.Length, packageIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void EveryProductAssemblyProject_IsStrongNameSignedByTheRepositoryKey()
    {
        var expectedKey = Path.GetFullPath(Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.snk"));
        string[] assemblyProjects = RepositoryLayout.ProductProjects
            .Where(project => BooleanPropertyOf(project, "IncludeBuildOutput"))
            .ToArray();

        Assert.NotEmpty(assemblyProjects);

        foreach (var project in assemblyProjects)
        {
            Assert.True(BooleanPropertyOf(project, "SignAssembly"));
            Assert.Equal(
                expectedKey,
                Path.GetFullPath(MsBuildEvaluation.PropertyOf(project, "AssemblyOriginatorKeyFile")));
        }
    }

    [Fact]
    public void EveryPackableProduct_HasASpecificSentenceDescription()
    {
        const string genericDescription =
            "ViciOne.ServiceBus provides a developer-focused, modern platform for creating distributed applications without complexity.";
        string[] packableProjects = RepositoryLayout.ProductProjects
            .Where(project => BooleanPropertyOf(project, "IsPackable"))
            .ToArray();

        Assert.NotEmpty(packableProjects);
        Assert.Equal(ExpectedPackageDescriptions.Count, packableProjects.Length);

        foreach (var project in packableProjects)
        {
            var description = MsBuildEvaluation.PropertyOf(project, "Description");
            var packageId = MsBuildEvaluation.PropertyOf(project, "PackageId");

            Assert.False(string.IsNullOrWhiteSpace(description));
            Assert.True(
                ExpectedPackageDescriptions.TryGetValue(packageId, out string? expectedDescription),
                $"{RepositoryLayout.RelativeToRoot(project)} has no reviewed package-description contract.");
            Assert.Equal(expectedDescription, description);
            Assert.NotEqual(genericDescription, description);
            Assert.DoesNotContain($"; {genericDescription}", description, StringComparison.Ordinal);
            Assert.DoesNotContain("dummy", description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("placeholder", description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stub", description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("tbd", description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("todo", description, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(".", description, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void EveryProductProjectReference_UsesTheCanonicalShortestRelativePath()
    {
        foreach (var project in RepositoryLayout.ProductProjects)
        {
            var projectDirectory = Path.GetDirectoryName(project)
                ?? throw new InvalidOperationException($"No directory for {project}.");
            var declaredProjects = new List<string>();

            foreach (var reference in XDocument.Load(project).Descendants("ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value
                    ?? throw new InvalidOperationException(
                        $"{RepositoryLayout.RelativeToRoot(project)} contains a ProjectReference without Include.");
                var localInclude = include
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace('/', Path.DirectorySeparatorChar);
                var referencedProject = Path.GetFullPath(localInclude, projectDirectory);
                var canonical = Path.GetRelativePath(projectDirectory, referencedProject).Replace('\\', '/');
                declaredProjects.Add(referencedProject);

                Assert.True(
                    File.Exists(referencedProject),
                    $"{RepositoryLayout.RelativeToRoot(project)} references missing project {include}.");
                Assert.Equal(canonical, include.Replace('\\', '/'));
            }

            string[] evaluatedProjects = MsBuildEvaluation.ItemMetadata(project, "ProjectReference", "FullPath")
                .Select(Path.GetFullPath)
                .Order(StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(declaredProjects.Order(StringComparer.Ordinal), evaluatedProjects);
        }
    }

    [Fact]
    public void EveryProductProjectDirectory_MatchesItsProjectName()
    {
        foreach (var project in RepositoryLayout.ProductProjects)
        {
            Assert.Equal(
                Path.GetFileNameWithoutExtension(project),
                Path.GetFileName(Path.GetDirectoryName(project)));
        }
    }

    private static bool BooleanPropertyOf(string project, string property)
    {
        string value = MsBuildEvaluation.PropertyOf(project, property);

        Assert.True(
            bool.TryParse(value, out bool result),
            $"{RepositoryLayout.RelativeToRoot(project)} evaluates {property} to '{value}', not a Boolean value.");
        return result;
    }
}
