using System;
using System.Text.Json.Serialization;
using RTChat.Server.API.Constants;

namespace RTChat.Server.API.Models
{
    public class Message
    {
        [JsonPropertyName(MessageParameterNames.Sender)]
        public User Sender { get; init; }
        
        [JsonPropertyName(MessageParameterNames.Receiver)]
        public User Receiver { get; init; }
        
        [JsonPropertyName(MessageParameterNames.Content)]
        public String Content { get; init; }
    }
}