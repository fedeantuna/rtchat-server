using System;

namespace RTChat.Server.API.Constants
{
    public static class ApplicationCacheEntrySizes
    {
        public const Int32 TokenResponse = 4;
        public const Int32 User = 4;
        public const Int32 ListeningUser = 7000;
        public const Int32 ActiveConnectionsForUser = 1;
    }
}