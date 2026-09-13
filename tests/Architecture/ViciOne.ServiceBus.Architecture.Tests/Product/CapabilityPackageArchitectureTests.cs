using System.Reflection;
using System.Xml.Linq;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.EntityFrameworkCore;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class CapabilityPackageArchitectureTests
{
    private static readonly string[] CapabilityAssemblyNames =
    [
        "ViciOne.ServiceBus.Courier",
        "ViciOne.ServiceBus.Futures",
        "ViciOne.ServiceBus.Initializers",
        "ViciOne.ServiceBus.JobService",
        "ViciOne.ServiceBus.Mediator",
        "ViciOne.ServiceBus.Sagas",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "dedicated-assemblies-own-capability-types")]
    public void CapabilityTypes_AreOwnedOnlyByTheirDedicatedAssemblies()
    {
        AssertAssembly("ViciOne.ServiceBus.Sagas", typeof(ISaga), typeof(SagaStateMachine<>), typeof(CorrelatedBy<>));
        AssertAssembly("ViciOne.ServiceBus.Courier", typeof(IActivity<,>), typeof(IExecuteActivity<>), typeof(RoutingSlip));
        AssertAssembly("ViciOne.ServiceBus.Futures", typeof(FutureState));
        AssertAssembly("ViciOne.ServiceBus.JobService", typeof(IJobConsumer<>), typeof(JobContext<>));
        AssertAssembly("ViciOne.ServiceBus.Mediator", typeof(IMediator));
        AssertAssembly("ViciOne.ServiceBus.Initializers", typeof(InVar), typeof(AdvancedMessageInitializerExtensions));
        AssertProjectOwnsAllCompiledSources("src/ViciOne.ServiceBus.Courier/ViciOne.ServiceBus.Courier.csproj");
        AssertProjectOwnsAllCompiledSources("src/ViciOne.ServiceBus.Mediator/ViciOne.ServiceBus.Mediator.csproj");

        Assembly mediatorAssembly = typeof(IMediator).Assembly;
        string[] exposedMediatorInternals = mediatorAssembly.GetExportedTypes()
            .Where(static type => type.Namespace is "ViciOne.ServiceBus.Mediator.Contexts" or "ViciOne.ServiceBus.DependencyInjection"
                || type.Name is "MediatorConfiguration"
                    or "MediatorRegistrationContext"
                    or "ServiceCollectionMediatorConfigurator")
            .Select(static type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(exposedMediatorInternals);

        Assembly[] capabilityAssemblies =
        [
            typeof(IActivity<,>).Assembly,
            typeof(FutureState).Assembly,
            typeof(InVar).Assembly,
            typeof(IJobConsumer<>).Assembly,
            typeof(IMediator).Assembly,
            typeof(ISaga).Assembly,
        ];
        HashSet<string> capabilityTypes = capabilityAssemblies
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type => !type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false))
            .Where(static type => type.FullName is not null && !type.FullName.StartsWith("<", StringComparison.Ordinal))
            .Select(static type => type.FullName)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        string[] duplicatedFoundationTypes =
        [
            .. ProductAssemblyFacts.Abstractions.GetTypes()
                .Concat(ProductAssemblyFacts.Core.GetTypes())
                .Select(static type => type.FullName)
                .OfType<string>()
                .Where(capabilityTypes.Contains)
                .Order(StringComparer.Ordinal),
        ];
        Assert.Empty(duplicatedFoundationTypes);
        Assert.DoesNotContain(ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Abstractions),
            CapabilityAssemblyNames.Contains);
        Assert.DoesNotContain(ProductAssemblyFacts.ReferencedAssemblyNames(ProductAssemblyFacts.Core),
            CapabilityAssemblyNames.Contains);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "approved-one-way-product-dependencies")]
    public void CapabilityProjects_HaveOnlyTheApprovedProductDependencies()
    {
        AssertProductReferences("src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
        AssertProductReferences("src/ViciOne.ServiceBus.Courier/ViciOne.ServiceBus.Courier.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
        AssertProductReferences("src/ViciOne.ServiceBus.Futures/ViciOne.ServiceBus.Futures.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj",
            "src/ViciOne.ServiceBus.Courier/ViciOne.ServiceBus.Courier.csproj",
            "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj");
        AssertProductReferences("src/ViciOne.ServiceBus.JobService/ViciOne.ServiceBus.JobService.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj",
            "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj");
        AssertProductReferences("src/ViciOne.ServiceBus.Mediator/ViciOne.ServiceBus.Mediator.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
        AssertProductReferences("src/ViciOne.ServiceBus.Initializers/ViciOne.ServiceBus.Initializers.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
        AssertProductReferences("src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
        AssertProductReferences(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore.Sagas/ViciOne.ServiceBus.EntityFrameworkCore.Sagas.csproj",
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj",
            "src/ViciOne.ServiceBus.Futures/ViciOne.ServiceBus.Futures.csproj",
            "src/ViciOne.ServiceBus.JobService/ViciOne.ServiceBus.JobService.csproj",
            "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj");
        AssertProductReferences(
            "src/ViciOne.ServiceBus.StateMachineVisualizer/ViciOne.ServiceBus.StateMachineVisualizer.csproj",
            "src/ViciOne.ServiceBus.Abstractions/ViciOne.ServiceBus.Abstractions.csproj",
            "src/ViciOne.ServiceBus.Sagas/ViciOne.ServiceBus.Sagas.csproj");
        AssertDirectPackages("src/ViciOne.ServiceBus.StateMachineVisualizer/ViciOne.ServiceBus.StateMachineVisualizer.csproj");

        AssertDirectPackages(
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj",
            "Microsoft.EntityFrameworkCore.Relational");

        AssertAssembly("ViciOne.ServiceBus.EntityFrameworkCore.Sagas", typeof(SagaDbContext));
        Assert.DoesNotContain(ProductAssemblyFacts.ReferencedAssemblyNames(
            typeof(EntityFrameworkMessageJournalConfigurationExtensions).Assembly), CapabilityAssemblyNames.Contains);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SUITE-COMPOSITION", "only-core-rabbitmq-and-efcore-are-direct-product-dependencies")]
    public void SuiteComposition_HasExactlyTheApprovedProductDependencies()
    {
        AssertProductReferences("samples/SuiteComposition/ViciOne.ServiceBus.Samples.SuiteComposition.csproj",
            "src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/ViciOne.ServiceBus.EntityFrameworkCore.csproj",
            "src/Transports/ViciOne.ServiceBus.RabbitMq/ViciOne.ServiceBus.RabbitMq.csproj",
            "src/ViciOne.ServiceBus/ViciOne.ServiceBus.csproj");
    }

    private static void AssertAssembly(string expectedAssemblyName, params Type[] types)
    {
        Assert.All(types, type => Assert.Equal(expectedAssemblyName, type.Assembly.GetName().Name));
    }

    private static void AssertProjectOwnsAllCompiledSources(string project)
    {
        string projectPath = Path.Combine(RepositoryLayout.Root, project);
        string projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path '{projectPath}' has no directory.");
        string ownedPrefix = projectDirectory + Path.DirectorySeparatorChar;
        string[] externalSources = MsBuildEvaluation.ItemMetadata(projectPath, "Compile", "FullPath")
            .Select(Path.GetFullPath)
            .Where(source => !source.StartsWith(ownedPrefix, RepositoryLayout.PathComparison))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(externalSources);
    }

    private static void AssertProductReferences(string project, params string[] expected)
    {
        string projectPath = Path.Combine(RepositoryLayout.Root, project);
        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        string[] actual = XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(static reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Select(static reference => reference.Replace('\\', Path.DirectorySeparatorChar))
            .Select(reference => Path.GetFullPath(Path.Combine(projectDirectory, reference)))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }

    private static void AssertDirectPackages(string project, params string[] expected)
    {
        string projectPath = Path.Combine(RepositoryLayout.Root, project);
        string[] actual = XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(static reference => reference.Attribute("Include")?.Value)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Order(StringComparer.Ordinal), actual);
    }
}
