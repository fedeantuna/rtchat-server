using System;

namespace RTChat.Server.API.Exceptions
{
    public class NullTokenException : Exception
    {
        private const String ErrorMessage = "The received token was null";

        public NullTokenException()
            : base(ErrorMessage)
        {
        }
    }
}