namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    /// <summary>
    /// The foreign licence closure and the usage telemetry closure are gone, and this is what keeps
    /// them gone.
    /// <para>
    /// Deleting the sources is not the proof. A compiler error would catch a leftover call, but it
    /// would not catch a type resolved by name at run time, a service still registered in the
    /// container, or a configuration key still read from the environment. So each of those is
    /// measured on its own: the loaded types, the built container, and the bytes of the shipped
    /// assemblies.
    /// </para>
    /// <para>
    /// Every check is name exact. <c>UsageCachePolicy</c> is a cache eviction policy that has nothing
    /// to do with telemetry and stays, and a substring rule over "Usage" or "License" would delete
    /// the wrong things and then report success for it. The controls below assert exactly that
    /// distinction, so a green result cannot mean the scan read nothing.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_removed_closures
    {
        /// <summary>The namespaces the two closures lived in. Nothing shipped may declare a type here.</summary>
        static readonly string[] RemovedNamespaces =
        {
            "ViciOne.ServiceBus.Licensing",
            "ViciOne.ServiceBus.UsageTelemetry",
            "ViciOne.ServiceBus.UsageTracking"
        };

        /// <summary>
        /// The removed identities, by exact name: the licence model and reader, the two licence
        /// exceptions, the telemetry contracts and their observers, and the two extension classes.
        /// </summary>
        static readonly string[] RemovedTypeNames =
        {
            "BusUsageTelemetry",
            "EndpointUsageTelemetry",
            "HostUsageTelemetry",
            "IUsageTelemetrySource",
            "IUsageTracker",
            "InvalidLicenseException",
            "InvalidLicenseFormatException",
            "LicenseContact",
            "LicenseCustomer",
            "LicenseFeature",
            "LicenseFile",
            "LicenseInfo",
            "LicenseProduct",
            "LicenseReader",
            "LicenseSettings",
            "RiderUsageTelemetry",
            "UsageTelemetryBusObserver",
            "UsageTelemetryConfigurationObserver",
            "UsageTelemetryEndpointConfigurationObserver",
            "UsageTelemetryOptions",
            "UsageTelemetryOptionsExtensions",
            "UsageTelemetrySerializerContext",
            "UsageTracker",
            "ViciOneServiceBusUsageTelemetry",
            "ViciOneServiceBusUsageTelemetryExtensions"
        };

        /// <summary>The environment and configuration keys the licence closure read.</summary>
        static readonly string[] RemovedConfigurationKeys =
        {
            "UseLicenseFile",
            "VICIONE_SERVICEBUS_LICENSE",
            "VICIONE_SERVICEBUS_LICENSE_PATH"
        };

        /// <summary>
        /// A retained name that a careless substring rule over "Usage" would have swept up with the
        /// rest. It is <c>UsageCachePolicy&lt;TValue&gt;</c>, so metadata and bytes both carry it with
        /// the arity suffix the CLR appends to a generic name.
        /// </summary>
        const string RetainedNeighbour = "UsageCachePolicy`1";

        [Test]
        public void Should_declare_no_type_in_a_removed_namespace()
        {
            string[] offenders = LoadedTypes()
                .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal))
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty, "a shipped assembly still declares a removed type");
        }

        [Test]
        public void Should_declare_no_type_under_a_removed_name()
        {
            string[] offenders = LoadedTypes()
                .Where(type => RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
                .Select(type => type.FullName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty, "a shipped assembly still declares a removed type");
        }

        [Test]
        public void Should_still_declare_the_retained_neighbour()
        {
            // The control for both checks above: the reader has to see the type that stays, or an
            // empty offender list would only mean that no type was read at all. It also fixes the
            // boundary — this name carries "Usage" and is correctly untouched.
            Type[] retained = LoadedTypes().Where(type => type.Name == RetainedNeighbour).ToArray();

            Assert.That(retained, Is.Not.Empty, $"{RetainedNeighbour} was expected to stay and was not read");
        }

        [Test]
        public void Should_register_no_removed_service_in_the_container()
        {
            // A registration survives a source deletion when it is made through an open generic or a
            // factory, so the built collection is read rather than the source.
            IServiceCollection collection = new ServiceCollection()
                .AddViciOneServiceBus(x => x.UsingInMemory());

            string[] offenders = collection
                .SelectMany(descriptor => new[] { descriptor.ServiceType, descriptor.ImplementationType })
                .Where(type => type != null)
                .Where(type => RemovedNamespaces.Contains(type.Namespace, StringComparer.Ordinal)
                    || RemovedTypeNames.Contains(type.Name, StringComparer.Ordinal))
                .Select(type => type.FullName)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty, "the default registration still carries a removed service");
            Assert.That(collection, Is.Not.Empty, "nothing was registered at all, so the check read nothing");
        }

        [Test]
        public void Should_carry_no_removed_name_in_the_shipped_bytes()
        {
            // The half that a compiler cannot give: a name used by reflection or read as a
            // configuration key survives every build, and only the bytes show it.
            string[] wanted = RemovedTypeNames.Concat(RemovedConfigurationKeys).Concat(RemovedNamespaces).ToArray();

            string[] offenders = ProductAssemblies()
                .SelectMany(assembly => AssemblyStrings(assembly).Select(text => new { assembly, text }))
                .Where(hit => wanted.Contains(hit.text, StringComparer.Ordinal))
                .Select(hit => $"{hit.assembly.GetName().Name}: {hit.text}")
                .Distinct()
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty, "a shipped assembly still carries a removed name in its bytes");
        }

        [Test]
        public void Should_read_the_retained_neighbour_out_of_the_shipped_bytes()
        {
            // The control for the byte scan, and the same boundary once more: the retained name is
            // present in the bytes, so an empty offender list above is a statement and not a silence.
            string[] found = ProductAssemblies()
                .SelectMany(AssemblyStrings)
                .Where(text => text == RetainedNeighbour)
                .Distinct()
                .ToArray();

            Assert.That(found, Is.Not.Empty, $"{RetainedNeighbour} was not read from the shipped bytes at all");
        }

        /// <summary>
        /// The two assemblies the closures were split across, each anchored on a type that can only
        /// come from it. Anchoring both on <see cref="IBus"/> and <see cref="IBusControl"/> reads
        /// ViciOne.ServiceBus.Abstractions twice and never opens ViciOne.ServiceBus at all, which is
        /// where the usage tracker itself lived.
        /// </summary>
        static IEnumerable<Assembly> ProductAssemblies()
        {
            yield return typeof(IBus).Assembly;
            yield return typeof(InMemoryConfigurationExtensions).Assembly;
        }

        [Test]
        public void Should_read_two_distinct_product_assemblies()
        {
            // The control for the scope of every scan in this fixture: two anchors that resolve to
            // one assembly would leave the other unmeasured while all of them still report green.
            string[] names = ProductAssemblies().Select(assembly => assembly.GetName().Name).ToArray();

            Assert.That(names, Is.Unique, "the scans read the same assembly twice: " + string.Join(", ", names));
            Assert.That(names, Has.Length.EqualTo(2));
        }

        static IEnumerable<Type> LoadedTypes()
        {
            return ProductAssemblies().SelectMany(assembly => assembly.GetTypes());
        }

        /// <summary>
        /// The printable runs in the assembly file, which is where both heaps live: a type name in
        /// the string heap and a configuration key in the user string heap are equally visible here.
        /// </summary>
        static IEnumerable<string> AssemblyStrings(Assembly assembly)
        {
            var bytes = System.IO.File.ReadAllBytes(assembly.Location);
            var builder = new System.Text.StringBuilder();

            foreach (var value in bytes)
            {
                if (value >= 0x20 && value < 0x7f)
                    builder.Append((char)value);
                else
                {
                    if (builder.Length > 0)
                        yield return builder.ToString();

                    builder.Clear();
                }
            }

            if (builder.Length > 0)
                yield return builder.ToString();
        }
    }
}
