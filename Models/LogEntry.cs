using System;
using System.Text.Json.Serialization;

namespace RoboBlocos.Models
{
    public class LogEntry
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("severity")]
        public LogSeverity Severity { get; set; }

        [JsonPropertyName("category")]
        public LogCategory Category { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public LogEntry(string message, LogSeverity severity, LogCategory category)
        {
            Message = message;
            Severity = severity;
            Category = category;
        }
    }

    public enum LogSeverity
    {
        Informational,
        Success,
        Error
    }

    public enum LogCategory
    {
        FirmwareDownload,
        Interface,
        Obrigatórios
    }
}