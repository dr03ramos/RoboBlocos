using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoboBlocos.Models
{
    /// <summary>
    /// Representa um tutorial completo do RoboBlocos
    /// </summary>
    public class Tutorial
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<TutorialStep> Steps { get; set; } = new();

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [JsonPropertyName("thumbnailPath")]
        public string? ThumbnailPath { get; set; }
    }

    /// <summary>
    /// Representa um passo individual dentro de um tutorial
    /// </summary>
    public class TutorialStep
    {
        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("imagePath")]
        public string? ImagePath { get; set; }

        [JsonPropertyName("codeSnippet")]
        public string? CodeSnippet { get; set; }
    }
}
