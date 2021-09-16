using System;

namespace RTChat.Server.API.Exceptions
{
    public class EmptyMessageException : Exception
    {
        private const String ErrorMessage = "The message must not be empty.";

        public EmptyMessageException()
            : base(ErrorMessage)
        {
        }
    }
}