// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RabbitMqTransport.Tests
{
    using System;
    using NUnit.Framework;
    using Transports;


    public class When_converting_a_type_to_a_message_name
    {
        readonly IMessageNameFormatter _formatter;

        public When_converting_a_type_to_a_message_name()
        {
            _formatter = new RabbitMqMessageNameFormatter();
        }

        [Test]
        public void Should_handle_an_interface_name()
        {
            var name = _formatter.GetMessageName(typeof(NameEasyToo));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:NameEasyToo"));
        }

        [Test]
        public void Should_handle_nested_classes()
        {
            var name = _formatter.GetMessageName(typeof(Nested));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:When_converting_a_type_to_a_message_name-Nested"));
        }

        [Test]
        public void Should_handle_regular_classes()
        {
            var name = _formatter.GetMessageName(typeof(NameEasy));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:NameEasy"));
        }

        [Test]
        public void Should_throw_an_exception_on_an_open_generic_class_name()
        {
            Assert.Throws<ArgumentException>(() => _formatter.GetMessageName(typeof(NameGeneric<>)));
        }

        [Test]
        public void Should_handle_a_closed_single_generic()
        {
            var name = _formatter.GetMessageName(typeof(NameGeneric<string>));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:NameGeneric--System:String--"));
        }

        [Test]
        public void Should_handle_a_closed_double_generic()
        {
            var name = _formatter.GetMessageName(typeof(NameDoubleGeneric<string, NameEasy>));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:NameDoubleGeneric--System:String::NameEasy--"));
        }

        [Test]
        public void Should_handle_a_closed_double_generic_with_a_generic()
        {
            var name = _formatter.GetMessageName(typeof(NameDoubleGeneric<NameGeneric<NameEasyToo>, NameEasy>));
            Assert.That(name, Is.EqualTo("ViciOne.ServiceBus.RabbitMqTransport.Tests:NameDoubleGeneric--NameGeneric--NameEasyToo--::NameEasy--"));
        }


        class Nested
        {
        }
    }


    class NameEasy
    {
    }


    interface NameEasyToo
    {
    }


    class NameGeneric<T>
    {
    }


    class NameDoubleGeneric<T1, T2>
    {
    }
}
