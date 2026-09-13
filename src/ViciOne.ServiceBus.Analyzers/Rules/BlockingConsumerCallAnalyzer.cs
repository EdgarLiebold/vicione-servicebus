using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Rules;

/// <summary>Rejects synchronous blocking inside message-consumer implementations.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlockingConsumerCallAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> s_blockingTypesByMethod =
        new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal)
        {
            ["Sleep"] = ["System.Threading.Thread"],
            ["WaitAll"] = ["System.Threading.Tasks.Task", "System.Threading.WaitHandle"],
            ["WaitAny"] = ["System.Threading.Tasks.Task", "System.Threading.WaitHandle"],
            ["Wait"] =
            [
                "System.Threading.SemaphoreSlim",
                "System.Threading.ManualResetEventSlim",
                "System.Threading.CountdownEvent",
                "System.Threading.Monitor",
            ],
            ["SignalAndWait"] = ["System.Threading.Barrier"],
            ["WaitOne"] = ["System.Threading.WaitHandle"],
            ["Join"] = ["System.Threading.Thread"],
            ["Enter"] = ["System.Threading.Monitor", "System.Threading.SpinLock"],
            ["EnterReadLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["EnterWriteLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["EnterUpgradeableReadLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["TryEnterReadLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["TryEnterWriteLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["TryEnterUpgradeableReadLock"] = ["System.Threading.ReaderWriterLockSlim"],
            ["SpinUntil"] = ["System.Threading.SpinWait"],
            ["GetResult"] =
            [
                "System.Runtime.CompilerServices.TaskAwaiter",
                "System.Runtime.CompilerServices.TaskAwaiter`1",
                "System.Runtime.CompilerServices.ValueTaskAwaiter",
                "System.Runtime.CompilerServices.ValueTaskAwaiter`1",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable+ConfiguredTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1+ConfiguredTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable+ConfiguredValueTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1+ConfiguredValueTaskAwaiter",
            ],
        }.ToImmutableDictionary(StringComparer.Ordinal);

    /// <summary>Identifies blocking calls in consumer code.</summary>
    public const string DiagnosticId = "VOSB5001";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Do not block inside a ServiceBus consumer",
        "Consumer code must await asynchronous work instead of using blocking call '{0}'",
        "Reliability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blocking waits consume receive concurrency and make shutdown or cancellation nondeterministic.");

    /// <summary>Gets the blocking-consumer-call diagnostic.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers blocking-operation analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeLock, OperationKind.Lock);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation
            || !ServiceBusSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IMethodSymbol method = invocation.TargetMethod;
        if (IsBlockingInvocation(context.Compilation, method))
            context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), method.Name));
    }

    private static bool IsBlockingInvocation(Compilation compilation, IMethodSymbol method)
    {
        if (method.Name == "Wait" && ServiceBusSymbolFacts.IsTaskLikeResultOwner(compilation, method.ContainingType))
            return true;

        if (method.Name == "TryEnter"
            && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                compilation,
                method.ContainingType,
                "System.Threading.Monitor",
                "System.Threading.SpinLock"))
            return HasTimeoutParameter(compilation, method);

        return s_blockingTypesByMethod.TryGetValue(method.Name, out var metadataNames)
            && ServiceBusSymbolFacts.IsCanonicalFrameworkType(compilation, method.ContainingType, metadataNames);
    }

    private static bool HasTimeoutParameter(Compilation compilation, IMethodSymbol method)
    {
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (parameter.Type.SpecialType == SpecialType.System_Int32
                || parameter.Type is INamedTypeSymbol named
                && ServiceBusSymbolFacts.IsCanonicalFrameworkType(compilation, named, "System.TimeSpan"))
                return true;
        }

        return false;
    }

    private static void AnalyzeLock(OperationAnalysisContext context)
    {
        if (context.Operation is ILockOperation
            && ServiceBusSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            context.ReportDiagnostic(Diagnostic.Create(s_rule, context.Operation.Syntax.GetLocation(), "lock"));
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference
            || !ServiceBusSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IPropertySymbol property = propertyReference.Property;
        if (property.Name == "Result" && ServiceBusSymbolFacts.IsTaskLikeResultOwner(
                context.Compilation, property.ContainingType))
            context.ReportDiagnostic(Diagnostic.Create(s_rule, propertyReference.Syntax.GetLocation(), property.Name));
    }
}
