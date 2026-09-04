using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace ViciOne.ServiceBus.Analyzers.V5;

/// <summary>
/// Provides a blocking consumer call analyzer implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlockingConsumerCallAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Defines the diagnostic id value.
    /// </summary>
    public const string DiagnosticId = "VOSB5001";

    private static readonly DiagnosticDescriptor s_rule = new(
        DiagnosticId,
        "Do not block inside a ServiceBus consumer",
        "Consumer code must await asynchronous work instead of using blocking call '{0}'",
        "Reliability",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Blocking waits consume receive concurrency and make shutdown or cancellation nondeterministic.");

    /// <summary>
    /// Gets the supported diagnostics value.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>
    /// Performs the initialize operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation
            || !AnalyzerSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IMethodSymbol method = invocation.TargetMethod;
        bool blocking =
            (method.Name == "Sleep" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Thread"))
            || (method.Name is "WaitAll" or "WaitAny" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Tasks.Task"))
            || (method.Name == "Wait" && AnalyzerSymbolFacts.IsTaskLikeResultOwner(
                context.Compilation, method.ContainingType))
            || (method.Name == "Wait" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation,
                method.ContainingType,
                "System.Threading.SemaphoreSlim",
                "System.Threading.ManualResetEventSlim",
                "System.Threading.CountdownEvent"))
            || (method.Name == "SignalAndWait" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Barrier"))
            || (method.Name is "WaitOne" or "WaitAll" or "WaitAny" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.WaitHandle"))
            || (method.Name == "Join" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Thread"))
            || (method.Name == "GetResult" && AnalyzerSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation,
                method.ContainingType,
                "System.Runtime.CompilerServices.TaskAwaiter",
                "System.Runtime.CompilerServices.TaskAwaiter`1",
                "System.Runtime.CompilerServices.ValueTaskAwaiter",
                "System.Runtime.CompilerServices.ValueTaskAwaiter`1",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable+ConfiguredTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1+ConfiguredTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable+ConfiguredValueTaskAwaiter",
                "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable`1+ConfiguredValueTaskAwaiter"));

        if (blocking)
            context.ReportDiagnostic(Diagnostic.Create(s_rule, invocation.Syntax.GetLocation(), method.Name));
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference
            || !AnalyzerSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IPropertySymbol property = propertyReference.Property;
        if (property.Name == "Result" && AnalyzerSymbolFacts.IsTaskLikeResultOwner(
                context.Compilation, property.ContainingType))
            context.ReportDiagnostic(Diagnostic.Create(s_rule, propertyReference.Syntax.GetLocation(), property.Name));
    }
}
