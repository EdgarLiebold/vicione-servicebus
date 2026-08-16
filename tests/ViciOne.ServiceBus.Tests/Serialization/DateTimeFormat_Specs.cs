namespace ViciOne.ServiceBus.Tests.Serialization
{
    using System;
    using System.Text;
    using ViciOne.ServiceBus.Serialization;
    using NUnit.Framework;


    [TestFixture]
    public class DateTimeFormat_Specs
    {
        [Test]
        public void Using_system_text_json()
        {
            var message = @"{ ""IsoDate"": ""1994-11-05T13:15:30Z"" }";

            var msg = System.Text.Json.JsonSerializer.Deserialize<MessageWithIsoDate>(Encoding.UTF8.GetBytes(message), SystemTextJsonMessageSerializer.Options);

            Assert.That(msg.IsoDate, Is.EqualTo("1994-11-05T13:15:30Z"));
        }

        [Test]
        public void Using_system_text_json_date_time()
        {
            var message = @"{ ""IsoDate"": ""1994-11-05T13:15:30Z"" }";

            var msg =
                System.Text.Json.JsonSerializer.Deserialize<MessageWithDateTime>(Encoding.UTF8.GetBytes(message), SystemTextJsonMessageSerializer.Options);

            Assert.That(msg.IsoDate.Kind, Is.EqualTo(DateTimeKind.Utc));
        }


        class MessageWithIsoDate
        {
            public string IsoDate { get; set; }
        }


        class MessageWithDateTime
        {
            public DateTime IsoDate { get; set; }
        }
    }
}
