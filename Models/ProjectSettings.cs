using System;
using System.IO;
using System.Text.Json.Serialization;

namespace RoboBlocos.Models
{
    public class ProjectSettings
    {
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; } = "Programa 1";

        [JsonPropertyName("robotSettings")]
        public RobotSettings RobotSettings { get; set; } = new();

        [JsonPropertyName("connectionSettings")]
        public ConnectionSettings ConnectionSettings { get; set; } = new();

        [JsonPropertyName("loggingSettings")]
        public LoggingSettings LoggingSettings { get; set; } = new();

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [JsonPropertyName("projectPath")]
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// Estado atual do projeto (não salvo no arquivo JSON)
        /// </summary>
        [JsonIgnore]
        public ProjectState State { get; set; } = ProjectState.New;
    }

    public class RobotSettings
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "RCX2";

        [JsonPropertyName("firmwareOption")]
        public FirmwareOption FirmwareOption { get; set; } = FirmwareOption.Recommended;

        [JsonPropertyName("customFirmwarePath")]
        public string CustomFirmwarePath { get; set; } = string.Empty;
    }

    public class ConnectionSettings
    {
        [JsonPropertyName("serialPort")]
        public string SerialPort { get; set; } = "COM1";

        [JsonPropertyName("connectionAttempts")]
        public int ConnectionAttempts { get; set; } = 5;
    }

    public class LoggingSettings
    {
        [JsonPropertyName("logFirmwareDownload")]
        public bool LogFirmwareDownload { get; set; } = true;

        [JsonPropertyName("logCompilationErrors")]
        public bool LogCompilationErrors { get; set; } = false;
    }

    public enum FirmwareOption
    {
        Recommended,
        ChooseFile
    }

    /// <summary>
    /// Representa o estado atual de um projeto
    /// </summary>
    public enum ProjectState
    {
        /// <summary>
        /// Projeto recém criado, ainda não foi salvo nem modificado
        /// </summary>
        New,
        
        /// <summary>
        /// Projeto salvo sem modificações pendentes
        /// </summary>
        Saved,
        
        /// <summary>
        /// Projeto com modificações não salvas
        /// </summary>
        Modified
    }
}