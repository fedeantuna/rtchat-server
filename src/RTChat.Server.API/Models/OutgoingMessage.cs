using System;

namespace RTChat.Server.API.Models
{
    public class OutgoingMessage
    {
        public String ReceiverId { get; init; }
        public String Content { get; init; }
    }
}