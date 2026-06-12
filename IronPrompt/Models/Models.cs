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

        [JsonPropertyName("imagePaths")]
        public System.Collections.Generic.List<string> ImagePaths { get; set; } = new();
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
        public string Language { get; set; } = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", System.StringComparison.OrdinalIgnoreCase) ? "tr" : "en";

        [JsonPropertyName("autoScrollEnabled")]
        public bool AutoScrollEnabled { get; set; } = true;
    }

    public class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("thinking")]
        public string Thinking { get; set; } = string.Empty;

        [JsonPropertyName("images")]
        public System.Collections.Generic.List<string>? Images { get; set; }
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

    public class OllamaShowResponse
    {
        [JsonPropertyName("details")]
        public OllamaShowDetails? Details { get; set; }

        [JsonPropertyName("capabilities")]
        public System.Collections.Generic.List<string>? Capabilities { get; set; }
    }

    public class OllamaShowDetails
    {
        [JsonPropertyName("families")]
        public System.Collections.Generic.List<string>? Families { get; set; }
    }

    public class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        public string SizeFormatted => $"{(double)Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    public class OllamaListResponse
    {
        [JsonPropertyName("models")]
        public System.Collections.Generic.List<OllamaModel> Models { get; set; } = new();
    }

    public class OllamaPullRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = true;
    }

    public class OllamaPullResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("digest")]
        public string Digest { get; set; } = string.Empty;

        [JsonPropertyName("total")]
        public long? Total { get; set; }

        [JsonPropertyName("completed")]
        public long? Completed { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
    }

    public class OllamaDeleteRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
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
    [JsonSerializable(typeof(System.Collections.Generic.List<string>))]
    [JsonSerializable(typeof(OllamaShowResponse))]
    [JsonSerializable(typeof(OllamaShowDetails))]
    [JsonSerializable(typeof(OllamaModel))]
    [JsonSerializable(typeof(OllamaListResponse))]
    [JsonSerializable(typeof(OllamaPullRequest))]
    [JsonSerializable(typeof(OllamaPullResponse))]
    [JsonSerializable(typeof(OllamaDeleteRequest))]
    [JsonSerializable(typeof(System.Collections.Generic.List<OllamaModel>))]
    public partial class OllamaJsonContext : JsonSerializerContext
    {
    }
}
