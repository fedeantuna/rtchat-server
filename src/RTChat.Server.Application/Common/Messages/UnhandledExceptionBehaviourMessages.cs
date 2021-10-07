using System;

namespace RTChat.Server.Application.Common.Messages
{
    public static class UnhandledExceptionBehaviourMessages
    {
        public const String UnhandledExceptionErrorMessageNameParameter = "Name";
        public const String UnhandledExceptionErrorMessageRequestParameter = "@Request";
        public static readonly String UnhandledExceptionErrorMessage = $"Unhandled Exception for Request {{{UnhandledExceptionErrorMessageNameParameter}}} {{{UnhandledExceptionErrorMessageRequestParameter}}}";
    }
}