// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData
{
    using System;
    using Util;


    public class InMemoryMessageDataId
    {
        readonly NewId _id;

        public InMemoryMessageDataId()
        {
            _id = NewId.Next();
        }

        public Uri Uri => new Uri("urn:msgdata:" + FormatUtil.Formatter.Format(_id.ToByteArray()));
    }
}
