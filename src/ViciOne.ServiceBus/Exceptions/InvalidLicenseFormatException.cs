// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class InvalidLicenseFormatException :
        Exception
    {
        public InvalidLicenseFormatException()
            : this("The license format was not recognized")
        {
        }

        public InvalidLicenseFormatException(string message)
            : base(message)
        {
        }
    }
}
