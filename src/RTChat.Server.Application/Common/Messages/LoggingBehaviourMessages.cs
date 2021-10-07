using System;

namespace RTChat.Server.Application.Common.Messages
{
    public static class LoggingBehaviourMessages
    {
        public const String LoggingBehaviourInformationMessageNameParameter = "Name";
        public const String LoggingBehaviourInformationMessageUserIdParameter = "@UserId";
        public const String LoggingBehaviourInformationMessageUserNameParameter = "@UserName";
        public const String LoggingBehaviourInformationMessageRequestParameter = "@Request";
        public static readonly String LoggingBehaviourInformationMessage = $"Request {{{LoggingBehaviourInformationMessageNameParameter}}} {{{LoggingBehaviourInformationMessageUserIdParameter}}} {{{LoggingBehaviourInformationMessageUserNameParameter}}} {{{LoggingBehaviourInformationMessageRequestParameter}}}";
    }
}