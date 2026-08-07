// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class InvalidLicenseException :
        Exception
    {
        public InvalidLicenseException()
            : this("The license was not valid")
        {
        }

        public InvalidLicenseException(string message)
            : base(message)
        {
        }
    }
}
