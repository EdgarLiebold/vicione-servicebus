// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Saga.Messages
{
    using System;


    [Serializable]
    public class RegisterUser :
        CorrelatedMessage
    {
        public RegisterUser(Guid correlationId, string username, string password, string displayName, string email)
            :
            base(correlationId)
        {
            Username = username;
            Password = password;
            DisplayName = displayName;
            Email = email;
        }

        protected RegisterUser()
        {
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
    }
}
