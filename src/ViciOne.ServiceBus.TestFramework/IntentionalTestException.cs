namespace ViciOne.ServiceBus.TestFramework
{
    using System;


    /// <summary>
    /// Thrown in places where it is expected as part of a unit test
    /// </summary>
    [Serializable]
    public class IntentionalTestException :
        Exception
    {
        public IntentionalTestException()
            : this("This exception was thrown intentionally as part of a test")
        {
        }

        public IntentionalTestException(string message)
            : base(message)
        {
        }

        public IntentionalTestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
