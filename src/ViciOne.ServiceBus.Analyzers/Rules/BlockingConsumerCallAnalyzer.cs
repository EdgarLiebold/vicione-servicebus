using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ViciOne.ServiceBus.Analyzers.Rules;

/// <summary>Rejects synchronous blocking inside message-consumer implementations.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlockingConsumerCallAnalyzer : DiagnosticAnalyzer
{
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

    /// <summary>Registers invocation and task-result analysis for non-generated source.</summary>
    /// <param name="context">The analyzer registration context.</param>
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
            || !ServiceBusSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IMethodSymbol method = invocation.TargetMethod;
        bool blocking =
            (method.Name == "Sleep" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Thread"))
            || (method.Name is "WaitAll" or "WaitAny" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Tasks.Task"))
            || (method.Name == "Wait" && ServiceBusSymbolFacts.IsTaskLikeResultOwner(
                context.Compilation, method.ContainingType))
            || (method.Name == "Wait" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation,
                method.ContainingType,
                "System.Threading.SemaphoreSlim",
                "System.Threading.ManualResetEventSlim",
                "System.Threading.CountdownEvent"))
            || (method.Name == "SignalAndWait" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Barrier"))
            || (method.Name is "WaitOne" or "WaitAll" or "WaitAny" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.WaitHandle"))
            || (method.Name == "Join" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
                context.Compilation, method.ContainingType, "System.Threading.Thread"))
            || (method.Name == "GetResult" && ServiceBusSymbolFacts.IsCanonicalFrameworkType(
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
            || !ServiceBusSymbolFacts.IsInsideConsumerImplementation(context.Compilation, context.ContainingSymbol))
            return;

        IPropertySymbol property = propertyReference.Property;
        if (property.Name == "Result" && ServiceBusSymbolFacts.IsTaskLikeResultOwner(
                context.Compilation, property.ContainingType))
            context.ReportDiagnostic(Diagnostic.Create(s_rule, propertyReference.Syntax.GetLocation(), property.Name));
    }
}
