// ViciOne modification: WP-F2-SERVICEBUS-CI-BASELINE-02, 2026-08-08.
namespace ViciOne.ServiceBus.Analyzers.Tests
{
    using System;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;


    /// <summary>
    /// Guards the analyzer test harness itself.
    /// A fixture that does not bind produces zero analyzer diagnostics, which is indistinguishable from
    /// "the analyzer found nothing". On the imported baseline that silence hid a broken reference set and
    /// kept 72 of 108 analyzer tests red without ever naming a cause. These tests fail if that silence returns.
    /// </summary>
    public class HarnessIntegrity_Specs :
        DiagnosticVerifier
    {
        const string UnresolvedTypeSource = @"
namespace ConsoleApplication1
{
    class Program
    {
        static void Main()
        {
            ThisTypeDoesNotExistAnywhere value = null;
        }
    }
}
";

        const string ValidSource = @"
using System;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main()
        {
            var value = new Uri(""urn:test"");
        }
    }
}
";

        [Test]
        public void Should_fail_loudly_when_a_fixture_does_not_bind()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => VerifyCSharpDiagnostic(UnresolvedTypeSource));

            Assert.That(exception.Message, Does.Contain("compilation reported"));
            Assert.That(exception.Message, Does.Contain("CS0246"));
        }

        [Test]
        public void Should_not_report_a_compilation_defect_for_a_binding_fixture()
        {
            Assert.DoesNotThrow(() => VerifyCSharpDiagnostic(ValidSource));
        }

        [Test]
        public void Should_resolve_the_shared_framework_reference_set()
        {
            // System.Uri and System.ComponentModel types were the facades missing from the hand-picked baseline set.
            const string source = @"
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var uri = new Uri(""urn:test"");
            var provider = (IServiceProvider)null;

            await Task.CompletedTask;
        }
    }
}
";

            Assert.DoesNotThrow(() => VerifyCSharpDiagnostic(source));
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MessageContractAnalyzer();
        }
    }
}
