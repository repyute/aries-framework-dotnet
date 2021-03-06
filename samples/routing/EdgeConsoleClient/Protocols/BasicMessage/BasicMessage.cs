﻿using System;
using EdgeConsoleClient.Messages;
using Newtonsoft.Json;
using Hyperledger.Aries.Agents;

namespace EdgeConsoleClient.Protocols.BasicMessage
{
    public class BasicMessage : AgentMessage
    {
        public BasicMessage()
        {
            Id = Guid.NewGuid().ToString();
            Type = CustomMessageTypes.BasicMessageType;
        }
        
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("sent_time")]
        public string SentTime { get; set; }
    }
}