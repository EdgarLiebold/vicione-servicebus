using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ViciOne.ServiceBus.Architecture.Tests.Build;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

/// <summary>Rejects compatibility-only identities that have no place in the greenfield product.</summary>
public sealed class GreenfieldApiArchitectureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS", "application-interfaces-declare-required-capabilities")]
    public void ApplicationInterfaces_DeclareRequiredCapabilitiesWithoutRuntimeProbingDefaults()
    {
        Type[] applicationContracts =
        [
            typeof(ISendEndpoint),
            typeof(IPublishEndpoint),
            typeof(IMessageScheduler),
            typeof(ConsumeContext<>),
        ];
        string[] runtimeDefaults = applicationContracts
            .SelectMany(static type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(static method => !method.IsAbstract)
            .Select(static method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            runtimeDefaults.Length == 0,
            $"Application capabilities must be compile-time requirements, not runtime-probing defaults:{Environment.NewLine}{string.Join(Environment.NewLine, runtimeDefaults)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-SAGA-API", "interfaces-use-dotnet-prefix")]
    public void SagaInterfaces_UseTheDotNetInterfacePrefix()
    {
        string sagaDirectory = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus.Sagas");
        string[] violations = Directory.EnumerateFiles(sagaDirectory, "*.cs", SearchOption.AllDirectories)
            .SelectMany(FindUnprefixedInterfaces)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Saga interfaces must begin with I followed by an uppercase letter:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-CAPABILITIES", "standard-unsupported-capability-exceptions")]
    public void UnsupportedCapabilities_UseTheStandardBclContract()
    {
        string[] violations = ProductSources()
            .Where(path => File.ReadAllText(path).Contains("NotImplementedByDesignException", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Unsupported capabilities must use NotSupportedException:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-METADATA", "no-binary-serialization-compatibility")]
    public void ProductDeclarations_HaveNoLegacyBinarySerializationMetadata()
    {
        string[] violations = ProductSources()
            .Where(ContainsSerializableAttribute)
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Legacy binary-serialization attributes remain:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-IDENTITY", "no-former-state-machine-brand")]
    public void ProductSourceAndPackages_HaveNoFormerStateMachineBrand()
    {
        string formerBrand = "Auto" + "matonymous";
        string[] productFiles = ProductSources()
            .Concat(RepositoryLayout.ProductProjects)
            .Distinct(RepositoryLayout.PathComparer)
            .ToArray();
        string[] violations = productFiles
            .Where(path => File.ReadAllText(path).Contains(formerBrand, StringComparison.OrdinalIgnoreCase))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Former state-machine brand remains in product source or package metadata:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-APPLICATION-CONTRACTS", "host-bridge-overloads-dispatch-to-provider-contract")]
    public void ProviderNeutralBridgeOverloads_DoNotForwardToThemselves()
    {
        string[] violations = ProductSources()
            .SelectMany(FindSelfForwardingOverrides)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Provider-neutral bridge overloads must adapt their callback before dispatching to a provider-specific overload:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-GREENFIELD-RUNTIME", "dynamic-pipe-registration-does-not-block-on-readiness")]
    public void DynamicPipeRegistration_DoesNotUseSynchronousReadinessWaiting()
    {
        string runtimeDirectory = Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Runtime");
        string[] violations = Directory.EnumerateFiles(runtimeDirectory, "ServiceBusRuntime*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => File.ReadAllText(path).Contains("TaskBlocking", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Dynamic pipe registration must remain a direct, non-blocking connection operation:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool ContainsSerializableAttribute(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetCompilationUnitRoot();
        return root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Any(attribute => attribute.Name.ToString() is "Serializable" or "SerializableAttribute"
                or "System.Serializable" or "System.SerializableAttribute");
    }

    private static IEnumerable<string> FindUnprefixedInterfaces(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetCompilationUnitRoot();

        foreach (InterfaceDeclarationSyntax declaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            string name = declaration.Identifier.ValueText;
            if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
                continue;

            int line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            yield return $"{RepositoryLayout.RelativeToRoot(path)}:{line}:{name}";
        }
    }

    private static IEnumerable<string> FindSelfForwardingOverrides(string path)
    {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(path))
            .GetCompilationUnitRoot();

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (!method.Modifiers.Any(static modifier => modifier.RawKind == (int)SyntaxKind.OverrideKeyword))
            {
                continue;
            }

            string[] parameterNames = method.ParameterList.Parameters
                .Select(static parameter => parameter.Identifier.ValueText)
                .ToArray();

            bool directlyForwardsEveryParameter = method.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.ValueText == method.Identifier.ValueText)
                .Any(invocation => invocation.ArgumentList.Arguments.Count == parameterNames.Length &&
                    invocation.ArgumentList.Arguments
                        .Select(static argument => (argument.Expression as IdentifierNameSyntax)?.Identifier.ValueText)
                        .SequenceEqual(parameterNames));

            if (directlyForwardsEveryParameter)
            {
                yield return $"{RepositoryLayout.RelativeToRoot(path)}:{method.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
            }
        }
    }

    private static IEnumerable<string> ProductSources() => RepositoryLayout.ProductProjects
        .SelectMany(project => MsBuildEvaluation.ItemMetadata(project, "Compile", "FullPath"))
        .Select(Path.GetFullPath)
        .Where(path => path.StartsWith(
            RepositoryLayout.Root + Path.DirectorySeparatorChar,
            RepositoryLayout.PathComparison))
        .Distinct(RepositoryLayout.PathComparer);
}
