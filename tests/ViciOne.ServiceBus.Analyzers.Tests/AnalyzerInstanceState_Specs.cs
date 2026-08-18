namespace ViciOne.ServiceBus.Analyzers.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;


    /// <summary>
    /// An analyzer instance is created once and reused for every compilation the host runs. Anything a
    /// compilation owns - symbols, the compilation itself, semantic models, syntax - must therefore never
    /// be stored in an instance field: it keeps the whole compilation alive and it lets one compilation
    /// answer with the symbols of another. These tests fail if such a field returns.
    /// </summary>
    [TestFixture]
    public class AnalyzerInstanceState_Specs :
        DiagnosticVerifier
    {
        static readonly Type[] CompilationBoundTypes =
        {
            typeof(ISymbol),
            typeof(Compilation),
            typeof(SemanticModel),
            typeof(SyntaxNode),
            typeof(SyntaxTree),
            typeof(Location),
            typeof(Diagnostic)
        };

        [Test]
        public void Should_hold_no_compilation_bound_state_in_an_analyzer_instance_field()
        {
            List<Type> analyzers = AnalyzerTypes().ToList();

            Assert.That(analyzers, Is.Not.Empty, "no analyzer type was found, so the scan proves nothing");

            var offenders = analyzers
                .SelectMany(InstanceFields)
                .Where(field => IsCompilationBound(field.FieldType))
                .Select(field => $"{field.DeclaringType?.Name}.{field.Name} : {Describe(field.FieldType)}")
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "an analyzer instance field holds state that belongs to a single compilation");
        }

        [Test]
        public void Should_detect_a_compilation_bound_instance_field()
        {
            // Control for the test above: without it an empty result would also be produced by a scan
            // that reads no field at all.
            var offenders = InstanceFields(typeof(AnalyzerWithCompilationBoundField))
                .Where(field => IsCompilationBound(field.FieldType))
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.EqualTo(new[] { "_members", "_symbol" }),
                "the scan does not recognise compilation bound state it is meant to catch");
        }

        [Test]
        public void Should_cover_every_analyzer_of_the_product_assembly()
        {
            // Names the carrier set instead of trusting that the reflection scan happened to see it.
            string[] names = AnalyzerTypes()
                .Select(type => type.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(names, Is.EqualTo(new[]
            {
                nameof(AsyncMethodAnalyzer),
                nameof(CancellationTokenOverloadMethodAnalyzer),
                nameof(MessageContractAnalyzer)
            }));
        }

        [Test]
        public void Should_analyse_two_independent_compilations_with_one_analyzer_instance()
        {
            // The reuse the fields above are dangerous for, exercised end to end: one instance, two
            // compilations that share no symbol, each asserted on its own expected result.
            var analyzer = new CancellationTokenOverloadMethodAnalyzer();

            Diagnostic[] first = Analyze(analyzer, CancellableConsumer("FirstOrder"));
            Diagnostic[] second = Analyze(analyzer, UncancellableConsumer("SecondOrder"));
            Diagnostic[] firstAgain = Analyze(analyzer, CancellableConsumer("ThirdOrder"));

            Assert.Multiple(() =>
            {
                Assert.That(first.Select(d => d.Id), Is.EqualTo(new[] { CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId }),
                    "the first compilation did not report the overload that is there");
                Assert.That(second, Is.Empty,
                    "the second compilation reported an overload although its own source has none");
                Assert.That(firstAgain.Select(d => d.Id), Is.EqualTo(new[] { CancellationTokenOverloadMethodAnalyzer.CancellationTokenOverloadMethodRuleId }),
                    "the third compilation lost the result after an unrelated compilation ran on the same instance");
            });
        }

        static Diagnostic[] Analyze(DiagnosticAnalyzer analyzer, string source)
        {
            return GetSortedDiagnosticsFromDocuments(analyzer, new[] { CreateDocument(source) });
        }

        static string CancellableConsumer(string contract)
        {
            return Source(contract, "return Task.Delay(10);");
        }

        static string UncancellableConsumer(string contract)
        {
            return Source(contract, "return Task.CompletedTask;");
        }

        static string Source(string contract, string body)
        {
            return $@"
using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ConsoleApplication1
{{
    public interface {contract}
    {{
        Guid Id {{ get; }}
    }}

    class Consumer :
        IConsumer<{contract}>
    {{
        public Task Consume(ConsumeContext<{contract}> context)
        {{
            {body}
        }}
    }}
}}
";
        }

        static IEnumerable<Type> AnalyzerTypes()
        {
            return typeof(MessageContractAnalyzer).Assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type));
        }

        static IEnumerable<FieldInfo> InstanceFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);

                foreach (var field in fields)
                    yield return field;
            }
        }

        static bool IsCompilationBound(Type type)
        {
            if (CompilationBoundTypes.Any(bound => bound.IsAssignableFrom(type)))
                return true;

            if (type.IsArray)
                return IsCompilationBound(type.GetElementType());

            return type.IsGenericType && type.GetGenericArguments().Any(IsCompilationBound);
        }

        static string Describe(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;

            return $"{type.Name}<{string.Join(", ", type.GetGenericArguments().Select(Describe))}>";
        }


        sealed class AnalyzerWithCompilationBoundField
        {
            #pragma warning disable CS0649 // Never assigned: the field exists so the scan has something to find.
            internal readonly ITypeSymbol _symbol;
            internal readonly Dictionary<string, IMethodSymbol> _members;
            internal readonly string _name;
            #pragma warning restore CS0649
        }
    }
}
