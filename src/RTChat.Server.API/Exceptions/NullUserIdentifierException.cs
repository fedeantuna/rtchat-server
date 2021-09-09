using System;

namespace RTChat.Server.API.Exceptions
{
    public class NullUserIdentifierException : Exception
    {
        private const String ErrorMessage = "The user could not be identified.";

        public NullUserIdentifierException()
            : base(ErrorMessage)
        {
        }
    }
}