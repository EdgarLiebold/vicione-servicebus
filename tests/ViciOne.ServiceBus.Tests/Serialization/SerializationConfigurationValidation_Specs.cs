namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Linq;
    using System.Net.Mime;
    using System.Text.Json;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Configuration;
    using ViciOne.ServiceBus.Serialization;


    /// <summary>
    /// Validation has to reject a content type that was named but never registered.
    /// <para>
    /// The matching check used to sit inside the branch that runs when no content type was given at
    /// all, so it only ever compared against an empty media type. A configuration naming an unknown
    /// type validated clean and then threw when the collection was created, which is late and far away
    /// from the line that caused it.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Validating_a_serialization_configuration
    {
        const string Unregistered = "application/vnd.nobody.registered+json";

        [Test]
        public void Should_accept_the_registered_serializer_content_type()
        {
            var configuration = new SerializationConfiguration();

            Assert.That(Validate(configuration), Is.Empty);
        }

        [Test]
        public void Should_reject_an_unregistered_serializer_content_type()
        {
            var configuration = new SerializationConfiguration { SerializerContentType = new ContentType(Unregistered) };

            Assert.That(Validate(configuration), Does.Contain("SerializerContentType"));
        }

        [Test]
        public void Should_reject_an_unregistered_default_content_type()
        {
            var configuration = new SerializationConfiguration { DefaultContentType = new ContentType(Unregistered) };

            Assert.That(Validate(configuration), Does.Contain("DefaultContentType"));
        }

        [Test]
        public void Should_match_a_registered_content_type_regardless_of_case()
        {
            // Media types are case insensitive, so a differently cased spelling of a registered type is
            // that type and must not be reported as missing.
            var configuration = new SerializationConfiguration
            {
                SerializerContentType =
                    new ContentType(SystemTextJsonMessageSerializer.JsonContentType.MediaType.ToUpperInvariant())
            };

            Assert.That(Validate(configuration), Is.Empty);
        }

        [Test]
        public void Should_reject_an_unregistered_content_type_in_a_child_configuration()
        {
            // A child inherits what its source registered, so the check runs against the union rather
            // than against the child alone.
            var child = (SerializationConfiguration)new SerializationConfiguration().CreateSerializationConfiguration();
            child.SerializerContentType = new ContentType(Unregistered);

            Assert.That(Validate(child), Does.Contain("SerializerContentType"));
        }

        [Test]
        public void Should_accept_an_inherited_content_type_in_a_child_configuration()
        {
            var child = (SerializationConfiguration)new SerializationConfiguration().CreateSerializationConfiguration();
            child.SerializerContentType = SystemTextJsonMessageSerializer.JsonContentType;

            Assert.That(Validate(child), Is.Empty);
        }

        static string Validate(SerializationConfiguration configuration)
        {
            return string.Join(" | ", configuration.Validate().Select(x => x.ToString()));
        }
    }


    /// <summary>
    /// Both option callbacks are declared to return the options to use. One of them threw that result
    /// away, and neither refused a callback that returned nothing.
    /// </summary>
    [TestFixture]
    public class Configuring_json_serializer_options
    {
        [Test]
        public void Should_use_the_options_the_callback_returns()
        {
            // Returning a different instance used to be discarded, so only mutating the given one had
            // any effect although the signature says otherwise. The naming policy is read through the
            // converter, so the written property name says which options actually reached it.
            var options = new JsonSerializerOptions();

            options.SetMessageSerializerOptions<SampleMessage>(
                _ => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            Assert.That(Write(options), Does.Contain("\"messageId\""));
        }

        [Test]
        public void Should_use_the_options_the_callback_mutated_and_returned()
        {
            var options = new JsonSerializerOptions();

            options.SetMessageSerializerOptions<SampleMessage>(x =>
            {
                x.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                return x;
            });

            Assert.That(Write(options), Does.Contain("\"messageId\""));
        }

        [Test]
        public void Should_refuse_a_callback_that_returns_nothing()
        {
            var options = new JsonSerializerOptions();

            Assert.That(() => options.SetMessageSerializerOptions<SampleMessage>(_ => null!),
                Throws.TypeOf<ConfigurationException>());
        }

        static string Write(JsonSerializerOptions options)
        {
            return JsonSerializer.Serialize(new SampleMessage { MessageId = 27 }, options);
        }


        public class SampleMessage
        {
            public int MessageId { get; set; }
        }
    }


    /// <summary>
    /// The other callback, and the one my previous version never touched although its prose claimed
    /// both. Putting the old implementation back — the one that assigns whatever the callback returned,
    /// including nothing — left all nine configuration tests green, so the correction was unproven.
    /// <para>
    /// These options are process wide state, so the fixture works on its own copy and puts the previous
    /// instance back afterwards. Without that a failing case here would change how every later test in
    /// the run serializes.
    /// </para>
    /// </summary>
    [TestFixture]
    public class Configuring_the_shared_json_options
    {
        [SetUp]
        public void Save()
        {
            _original = SystemTextJsonMessageSerializer.Options;
            SystemTextJsonMessageSerializer.Options = new JsonSerializerOptions(_original) { WriteIndented = false };
        }

        [TearDown]
        public void Restore()
        {
            if (_original != null)
                SystemTextJsonMessageSerializer.Options = _original;
        }

        JsonSerializerOptions _original;

        [Test]
        public void Should_keep_what_the_callback_mutated_and_returned()
        {
            Configure(x =>
            {
                x.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

                return x;
            });

            Assert.That(SystemTextJsonMessageSerializer.Options.PropertyNamingPolicy, Is.EqualTo(JsonNamingPolicy.CamelCase));
        }

        [Test]
        public void Should_keep_the_instance_the_callback_returned_instead()
        {
            var replacement = new JsonSerializerOptions { WriteIndented = true };

            Configure(_ => replacement);

            Assert.That(SystemTextJsonMessageSerializer.Options, Is.SameAs(replacement));
        }

        [Test]
        public void Should_hand_the_callback_a_copy_and_not_the_shared_instance()
        {
            // Once anything has serialized through them the shared options are frozen, so mutating that
            // instance would throw at some later and unrelated moment.
            var before = SystemTextJsonMessageSerializer.Options;

            Configure(x =>
            {
                x.WriteIndented = true;

                return x;
            });

            Assert.Multiple(() =>
            {
                Assert.That(SystemTextJsonMessageSerializer.Options, Is.Not.SameAs(before));
                Assert.That(before.WriteIndented, Is.False, "the instance in use was handed out and mutated");
            });
        }

        [Test]
        public void Should_refuse_a_callback_that_returns_nothing()
        {
            var before = SystemTextJsonMessageSerializer.Options;

            Assert.Multiple(() =>
            {
                Assert.That(() => Configure(_ => null), Throws.TypeOf<ConfigurationException>());
                Assert.That(SystemTextJsonMessageSerializer.Options, Is.SameAs(before),
                    "the options every serializer in the process reads may not be left empty");
            });
        }

        [Test]
        public void Should_leave_the_options_alone_when_no_callback_was_given()
        {
            var before = SystemTextJsonMessageSerializer.Options;

            Configure(null);

            Assert.That(SystemTextJsonMessageSerializer.Options, Is.SameAs(before));
        }

        static void Configure(Func<JsonSerializerOptions, JsonSerializerOptions> configure)
        {
            Bus.Factory.CreateUsingInMemory(cfg => cfg.ConfigureJsonSerializerOptions(configure));
        }
    }
}
