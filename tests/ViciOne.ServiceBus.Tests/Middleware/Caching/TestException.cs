namespace ViciOne.ServiceBus.Tests.Middleware.Caching
{
    using System;


    [Serializable]
    public class TestException :
        Exception
    {
        public TestException()
        {
        }

        public TestException(string message)
            : base(message)
        {
        }

        public TestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
