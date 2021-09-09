using System;
using System.Net.Mail;

namespace RTChat.Server.API.Exceptions
{
    public class NullUserException : Exception
    {
        private const String ErrorMessage = "The user with id {0} was not found.";

        public NullUserException(String userId)
            : base(String.Format(ErrorMessage, userId))
        {
        }
    }
}