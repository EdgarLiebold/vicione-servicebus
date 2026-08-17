namespace ViciOne.ServiceBus.Tests.Serialization
{
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
}
