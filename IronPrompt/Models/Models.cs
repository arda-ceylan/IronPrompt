// Copyright 2026 Arda Ceylan
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Text.Json.Serialization;

namespace IronPrompt.Models
{
    public class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("think")]
        public bool? Think { get; set; }
    }

    public class OllamaResponseChunk
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("thinking")]
        public string Thinking { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    public class ChatMessageData
    {
        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = string.Empty;

        [JsonPropertyName("isUser")]
        public bool IsUser { get; set; }

        [JsonPropertyName("rawContent")]
        public string RawContent { get; set; } = string.Empty;
    }

    public class ChatSessionData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("subtitle")]
        public string Subtitle { get; set; } = string.Empty;

        [JsonPropertyName("scrollOffset")]
        public double ScrollOffset { get; set; }

        [JsonPropertyName("messages")]
        public System.Collections.Generic.List<ChatMessageData> Messages { get; set; } = new();
    }

    public class WindowSettingsData
    {
        [JsonPropertyName("x")]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        public double? Y { get; set; }

        [JsonPropertyName("width")]
        public double? Width { get; set; }

        [JsonPropertyName("height")]
        public double? Height { get; set; }

        [JsonPropertyName("windowState")]
        public int WindowState { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = "tr";
    }

    public class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("thinking")]
        public string Thinking { get; set; } = string.Empty;
    }

    public class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public System.Collections.Generic.List<OllamaChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    public class OllamaChatResponseChunk
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    [JsonSerializable(typeof(OllamaRequest))]
    [JsonSerializable(typeof(OllamaResponseChunk))]
    [JsonSerializable(typeof(ChatSessionData))]
    [JsonSerializable(typeof(ChatMessageData))]
    [JsonSerializable(typeof(WindowSettingsData))]
    [JsonSerializable(typeof(OllamaChatRequest))]
    [JsonSerializable(typeof(OllamaChatMessage))]
    [JsonSerializable(typeof(OllamaChatResponseChunk))]
    [JsonSerializable(typeof(System.Collections.Generic.List<ChatSessionData>))]
    [JsonSerializable(typeof(System.Collections.Generic.List<ChatMessageData>))]
    [JsonSerializable(typeof(System.Collections.Generic.List<OllamaChatMessage>))]
    public partial class OllamaJsonContext : JsonSerializerContext
    {
    }
}
