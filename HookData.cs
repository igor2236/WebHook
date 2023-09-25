﻿using System.Text.Json.Serialization;

namespace WebHookExample
{
    public class Data
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("from")]
        public string From { get; set; } = null!;

        [JsonPropertyName("to")]
        public string To { get; set; } = null!;

        [JsonPropertyName("ack")]
        public string? Ack { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "reaction"!;


        [JsonPropertyName("msgId")]
        public string? msgId { get; set; } = null!;



        [JsonPropertyName("body")]
        public string Body { get; set; } = null!;

        [JsonPropertyName("media")]
        public string? Media { get; set; }

        [JsonPropertyName("fromMe")]
        public bool FromMe { get; set; }

        [JsonPropertyName("isForwarded")]
        public bool IsForwarded { get; set; }

        [JsonPropertyName("time")]
        public long Time { get; set; }
    }
    public class HookData
    {
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = null!;

        [JsonPropertyName("instanceId")]
        public string InstanceId { get; set; } = null!;


        [JsonPropertyName("referenceId")]
        public string? referenceId { get; set; } = null!;


        [JsonPropertyName("data")]
        public Data? Data { get; set; }
    }
}
