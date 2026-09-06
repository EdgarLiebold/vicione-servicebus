using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

/// <summary>Guards deterministic navigation from a C# type name to its physical source file.</summary>
public sealed class SourceFileNamingArchitectureTests
{
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReviewedPartialFragments =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/ChildSpecificationPipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/Filters/SplitFilterPipeSpecification.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/PipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/Configuration/SpecificationPipeBuilder.cs"] = Types("public class PipeConfigurator`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/DefaultInstanceStateAccessor.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/InitialIfNullStateAccessor.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/IntStateAccessor.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/RawStateAccessor.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/StateAccessorIndex.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Accessors/StringStateAccessor.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/BehaviorContextProxy.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/BehaviorExceptionContextProxy.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/StateMachineConnector.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/StateMachineSagaConfigurator.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/StateMachineSagaSpecification.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Correlation/UncorrelatedEventCorrelation.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/EventObservable.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/NonTransitionEventObserver.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/SelectedEventObserver.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/StateMachineEvent.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/StateMachineRequest.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/StateMachineSchedule.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/StateMachineState.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/StateObservable.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/UnhandledEventBehaviorContext.cs"] = Types("public class ViciOneServiceBusStateMachine`1"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/MessageCorrelationIdEventCorrelationBuilder.cs"] = Types("public class StateMachineInterfaceType`2"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/MessageCorrelationIdFaultEventCorrelationBuilder.cs"] = Types("public class StateMachineInterfaceType`2"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/StateMachineEventConnectorFactory.cs"] = Types("public class StateMachineInterfaceType`2"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/StateMachineSagaMessageConnector.cs"] = Types("public class StateMachineInterfaceType`2"),
            ["src/ViciOne.ServiceBus/SagaStateMachine/Configuration/ViciOneServiceBusEventCorrelationConfigurator.cs"] = Types("public class StateMachineInterfaceType`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/CorrelatedSagaMessageConnector.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/QuerySagaMessageConnector.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/SagaMessageConnector.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/SagaMessageSpecification.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/SagaMessageSplitFilterSpecification.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/SagaPipeSpecificationProxy.cs"] = Types("public class SagaConnector`2"),
            ["src/ViciOne.ServiceBus/Sagas/Configuration/SagaSplitFilterSpecification.cs"] = Types("public class SagaConnector`2"),
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> ReviewedCohesiveMultiTypeFiles =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
        {
            ["src/Persistence/ViciOne.ServiceBus.EntityFrameworkCore/DurableSend/EntityFrameworkDurableSendEntities.cs"] = Types(
                "internal class DurableSendRecord`0", "internal class DurableSendCapacityState`0",
                "internal class ReliableInboxRecord`0", "internal class ReliableRecurringScheduleRecord`0"),
            ["src/Transports/ViciOne.ServiceBus.ActiveMq/Configuration/ActiveMqTransportOptions.cs"] = Types(
                "public enum ActiveMqTransportProtocol`0", "public class ActiveMqTransportOptions`0"),
            ["src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendOperationResult.cs"] = Types(
                "public enum DurableSendOperationOutcome`0", "public record struct DurableSendOperationResult`0"),
            ["src/ViciOne.ServiceBus.Abstractions/DurableSend/DurableSendQuarantinePagination.cs"] = Types(
                "public class DurableSendQuarantinePagination`0", "public record struct DurableSendQuarantineSeek`0"),
            ["src/ViciOne.ServiceBus.Abstractions/ITechnicalFailureClassifier.cs"] = Types(
                "public interface ITechnicalFailureClassifier`0", "public interface IRetryFailureClassification`0"),
            ["src/ViciOne.ServiceBus.Abstractions/Middleware/OneTimeSetupMethod.cs"] = Types(
                "internal class OneTimeSetupMethod`0", "public interface OneTimeContext`0"),
            ["src/ViciOne.ServiceBus.Abstractions/Transports/ITransportSendFailureClassifier.cs"] = Types(
                "public enum TransportSendFailureKind`0", "public interface ITransportSendFailureClassifier`0"),
            ["src/ViciOne.ServiceBus/Configuration/BusCompositionValidation.cs"] = Types(
                "internal interface IBusCompositionRegistration`0", "internal class BusCompositionRegistration`1",
                "internal interface IBusTransportRegistration`0", "internal class BusTransportRegistration`1",
                "internal interface IBusFeatureRegistration`0", "internal class BusFeatureRegistration`1",
                "internal class BusCompositionRegistrations`0", "internal class BusCompositionStartupValidator`1"),
            ["src/ViciOne.ServiceBus/Courier/Configuration/ActivityConsumerKinds.cs"] = Types(
                "internal class ActivityConsumerKind`0", "internal class ExecuteActivityConsumerKind`0"),
            ["src/ViciOne.ServiceBus/DependencyInjection/DeferredBusScopedContextProviders.cs"] = Types(
                "internal class DeferredBusScopedContextProvider`1", "internal class AmbientTransactionScopedBusContextProvider`1",
                "internal class BufferedBusScopedBusContextProvider`1"),
            ["src/ViciOne.ServiceBus/Transports/Components/KillSwitch/KillSwitchEndpoint.cs"] = Types(
                "internal interface IKillSwitchEndpoint`0", "internal class RestartableReceiveEndpointKillSwitchEndpoint`0"),
            ["tests/Testing/ViciOne.ServiceBus.Tests.InternalAccess/Internals/ReflectionImplementationTestDrivers.cs"] = Types(
                "public class DynamicImplementationBuilderTestDriver`0", "public class BusInstanceBuilderTestDriver`0",
                "public class ReadPropertyTestDriver`2", "public class WritePropertyTestDriver`2"),
            ["tests/Transports/ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests/DeployTopologyContracts/ActiveMqDeployTopologyContracts.cs"] = Types(
                "public interface OrderSubmitted`0", "public interface OrderEvent`0", "public interface PackageShipped`0",
                "public interface PackageEvent`0", "public interface CustomerEvent`0"),
            ["tests/Transports/ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests/PublishContracts/ActiveMqPublishContracts.cs"] = Types(
                "public interface FirstPublishedContract`0", "public interface SecondPublishedContract`0"),
            ["tests/Transports/ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests/DeployTopologyContracts/AmazonSqsDeployTopologyContracts.cs"] = Types(
                "public interface OrderSubmitted`0", "public interface OrderEvent`0", "public interface PackageShipped`0",
                "public interface PackageEvent`0", "public interface CustomerEvent`0"),
            ["tests/Transports/ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests/PublishContracts/AmazonSqsPublishContracts.cs"] = Types(
                "public interface FirstPublishedContract`0", "public interface SecondPublishedContract`0"),
            ["tests/Transports/ViciOne.ServiceBus.AzureServiceBus.LocalIntegration.Tests/TopologyContracts/AzureServiceBusTopologyContracts.cs"] = Types(
                "public interface ExplicitTopologyContract`0", "public interface NamespaceTopologyContract`0",
                "public interface ExcludedTopologyContract`0"),
            ["tests/ViciOne.ServiceBus.MessagePack.Tests/Serialization/MessagePackTestContracts.cs"] = Types(
                "public interface BinaryContract`0", "public interface PersonContract`0", "public interface ContactContract`0",
                "public class PersonMessage`0", "public class ContactMessage`0", "public class BinaryMessage`0",
                "public class TemporalMessage`0", "public class ConstructorBoundMessage`0", "public class ScalarMessage`0",
                "public class CollectionMessage`0", "public class NestedMessage`0", "public class EdgeShapeMessage`0",
                "public enum ExampleState`0", "public class EmptyMessage`0", "public class ObjectGraphMessage`0",
                "public interface ValidationContract`0", "public class ValidationMessage`0", "public class XmlPayloadMessage`0"),
            ["tests/ViciOne.ServiceBus.Tests/DependencyInjection/ContainerDiscovery/ContainerDiscoveryTypes.cs"] = Types(
                "public class DiscoveryMarker`0", "public record DiscoveryPing`0", "public record DiscoveryPong`0",
                "public record PingReceived`0", "public record PingAcknowledged`0", "public record PingCompleted`0",
                "public record PingArguments`0", "public record PingLog`0", "public class DiscoveryPingConsumer`0",
                "public class DiscoveryPingConsumerDefinition`0", "public class DiscoveryExcludedConsumer`0",
                "public class DiscoveryPingSaga`0", "public class DiscoveryPingState`0", "public class DiscoveryPingStateMachine`0",
                "public class DiscoveryPingStateDefinition`0", "public class PingActivity`0", "public class PingSecondActivity`0"),
            ["tests/ViciOne.ServiceBus.Tests/JobService/Integration/ContainerJobDiscovery/ContainerJobDiscoveryTypes.cs"] = Types(
                "public class DiscoveryMarker`0", "public record CrunchNumbers`0", "public record JobSnapshot`0",
                "public class JobObservation`0", "public class CrunchNumbersConsumer`0"),
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-SOURCE-NAVIGATION", "every-source-file-has-a-declared-primary-type-or-reviewed-grouping")]
    public void EverySourceFile_IsNamedForItsPrimaryTypeOrAReviewedCohesiveGroup()
    {
        string[] sources = RepositoryLayout.ProductProjects
            .Concat(RepositoryLayout.NativeTestProjects)
            .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
            .Select(Path.GetFullPath)
            .Where(IsRepositorySource)
            .Distinct(RepositoryLayout.PathComparer)
            .OrderBy(RepositoryLayout.RelativeToRoot, StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(sources);

        var inspected = sources.Select(Inspect).ToArray();
        string[] violations = inspected
            .Where(static file => !file.IsConforming && !file.IsReviewedGrouping)
            .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]")
            .ToArray();
        string[] manifestMismatches = inspected
            .Where(static file => file.IsReviewedPath && !file.IsReviewedGrouping)
            .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]")
            .Concat(inspected
                .Where(static file => file.IsReviewedPartialPath && !file.IsReviewedPartial)
                .Select(static file => $"{file.Path}: declares [{string.Join(", ", file.TypeIdentities)}]"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] staleExceptions = inspected
            .Where(static file => file.IsConforming && file.IsReviewedGrouping)
            .Select(static file => file.Path)
            .Concat(ReviewedCohesiveMultiTypeFiles.Keys.Except(
                inspected.Select(static file => file.Path),
                StringComparer.Ordinal))
            .Concat(ReviewedPartialFragments.Keys.Except(
                inspected.Select(static file => file.Path),
                StringComparer.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Source files without a primary type:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
        Assert.True(
            manifestMismatches.Length == 0,
            $"Reviewed source-file manifests no longer match:{Environment.NewLine}{string.Join(Environment.NewLine, manifestMismatches)}");
        Assert.True(
            staleExceptions.Length == 0,
            $"Stale source-file naming exceptions:{Environment.NewLine}{string.Join(Environment.NewLine, staleExceptions)}");
    }

    private static SourceFileInspection Inspect(string source)
    {
        string path = RepositoryLayout.RelativeToRoot(source);
        string fileName = Path.GetFileNameWithoutExtension(source);
        string sourceText = File.ReadAllText(source);
        CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(sourceText)
            .GetCompilationUnitRoot();
        MemberDeclarationSyntax[] declarations = syntax.DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(IsTopLevelType)
            .ToArray();
        string[] typeNames = declarations.Select(TypeName).ToArray();
        string[] typeIdentities = declarations.Select(TypeIdentity).ToArray();
        bool reviewedPartialPath = ReviewedPartialFragments.TryGetValue(path, out IReadOnlySet<string>? expectedPartialTypes);
        bool reviewedPartial = reviewedPartialPath
            && declarations.Length > 0
            && declarations.All(IsPartial)
            && expectedPartialTypes!.SetEquals(typeIdentities);
        bool conventionPartial = declarations.Length > 0
            && declarations.All(declaration => IsPartial(declaration)
                && fileName.StartsWith(TypeName(declaration) + ".", StringComparison.Ordinal));
        bool primaryType = typeNames.Contains(fileName, StringComparer.Ordinal);
        bool publicTypesMatch = path.StartsWith("src/", StringComparison.Ordinal)
            ? declarations
                .Where(IsPublic)
                .All(declaration => StringComparer.Ordinal.Equals(fileName, TypeName(declaration)))
            : declarations.All(declaration =>
                StringComparer.Ordinal.Equals(fileName, TypeName(declaration)) || !ContainsTests(declaration));
        bool generatedFile = (fileName.EndsWith(".generated", StringComparison.Ordinal)
                || fileName.EndsWith(".g", StringComparison.Ordinal))
            && syntax.GetLeadingTrivia().ToFullString().Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
        bool infrastructureFile = declarations.Length == 0
            && (fileName.EndsWith("GlobalUsings", StringComparison.Ordinal)
                || fileName.EndsWith("AssemblyInfo", StringComparison.Ordinal)
                || fileName.EndsWith("AssemblyAttributes", StringComparison.Ordinal));
        bool reviewedPath = ReviewedCohesiveMultiTypeFiles.TryGetValue(path, out IReadOnlySet<string>? expectedTypes);
        bool reviewedGrouping = reviewedPath && expectedTypes!.SetEquals(typeIdentities);

        return new SourceFileInspection(
            path,
            typeIdentities,
            infrastructureFile || generatedFile || reviewedPartial || conventionPartial || (primaryType && publicTypesMatch),
            reviewedPath,
            reviewedGrouping,
            reviewedPartialPath,
            reviewedPartial);
    }

    private static bool IsRepositorySource(string path) =>
        path.StartsWith(RepositoryLayout.Root + Path.DirectorySeparatorChar, RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}artifacts{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison)
        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", RepositoryLayout.PathComparison);

    private static bool IsTopLevelType(MemberDeclarationSyntax declaration) =>
        declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
        && declaration.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax;

    private static string TypeName(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
        _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}.")
    };

    private static bool IsPartial(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Modifiers.Any(SyntaxKind.PartialKeyword),
        DelegateDeclarationSyntax @delegate => @delegate.Modifiers.Any(SyntaxKind.PartialKeyword),
        _ => false,
    };

    private static bool IsPublic(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Modifiers.Any(SyntaxKind.PublicKeyword),
        DelegateDeclarationSyntax @delegate => @delegate.Modifiers.Any(SyntaxKind.PublicKeyword),
        _ => false,
    };

    private static string TypeIdentity(MemberDeclarationSyntax declaration)
    {
        SyntaxTokenList modifiers = declaration switch
        {
            BaseTypeDeclarationSyntax type => type.Modifiers,
            DelegateDeclarationSyntax @delegate => @delegate.Modifiers,
            _ => default,
        };
        string visibility = modifiers.Any(SyntaxKind.PublicKeyword) ? "public"
            : modifiers.Any(SyntaxKind.FileKeyword) ? "file"
            : "internal";
        string kind = declaration switch
        {
            InterfaceDeclarationSyntax => "interface",
            EnumDeclarationSyntax => "enum",
            RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "record struct",
            RecordDeclarationSyntax => "record",
            StructDeclarationSyntax => "struct",
            ClassDeclarationSyntax => "class",
            DelegateDeclarationSyntax => "delegate",
            _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}.")
        };
        int arity = declaration switch
        {
            TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
            DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList?.Parameters.Count ?? 0,
            _ => 0,
        };

        return $"{visibility} {kind} {TypeName(declaration)}`{arity}";
    }

    private static bool ContainsTests(MemberDeclarationSyntax declaration) =>
        TypeName(declaration).EndsWith("Tests", StringComparison.Ordinal)
        || declaration.DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .SelectMany(static method => method.AttributeLists)
        .SelectMany(static list => list.Attributes)
        .SelectMany(static attribute => attribute.Name.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        .Any(static identifier => identifier.Identifier.ValueText is "Fact" or "FactAttribute" or "Theory" or "TheoryAttribute");

    private static IReadOnlySet<string> Types(params string[] identities) =>
        new HashSet<string>(identities, StringComparer.Ordinal);

    private sealed record SourceFileInspection(
        string Path,
        IReadOnlyList<string> TypeIdentities,
        bool IsConforming,
        bool IsReviewedPath,
        bool IsReviewedGrouping,
        bool IsReviewedPartialPath,
        bool IsReviewedPartial);
}
