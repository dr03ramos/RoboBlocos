using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RoboBlocos.Models;
using RoboBlocos.Services;

namespace RoboBlocos.Utilities
{
    public static class ProjectUtilities
    {
        /// <summary>
        /// Obtém uma lista de projetos acessados recentemente
        /// </summary>
        public static async Task<List<ProjectSettings>> GetRecentProjectsAsync(int maxCount = 10)
        {
            var recentProjects = new List<ProjectSettings>();
            
            try
            {
                var projectDirectories = ProjectService.GetAllProjects();
                
                foreach (var directory in projectDirectories.Take(maxCount))
                {
                    try
                    {
                        var project = await ProjectService.LoadProjectAsync(directory.FullName);
                        recentProjects.Add(project);
                    }
                    catch
                    {
                        // Ignorar projetos que não podem ser carregados
                        continue;
                    }
                }
            }
            catch
            {
                // Se houver um erro ao acessar o diretório dos projetos, retornar lista vazia
            }

            return recentProjects;
        }

        /// <summary>
        /// Cria um nome único de projeto se o nome sugerido já existir
        /// </summary>
        public static string GetUniqueProjectName(string baseName)
        {
            var rootPath = ProjectService.GetProjectsRootPath();
            var suggestedName = baseName;
            var counter = 1;

            while (Directory.Exists(Path.Combine(rootPath, CleanFileName(suggestedName))))
            {
                suggestedName = $"{baseName} ({counter})";
                counter++;
            }

            return suggestedName;
        }

        /// <summary>
        /// Remove caracteres inválidos de um nome de arquivo e garante o comprimento adequado
        /// </summary>
        /// <param name="fileName">O nome do arquivo a ser limpo</param>
        /// <param name="defaultName">Nome padrão a ser usado se o nome do arquivo estiver vazio ou inválido</param>
        /// <param name="maxLength">Comprimento máximo para o nome do arquivo (null para sem limite)</param>
        /// <returns>Um nome de arquivo limpo e válido</returns>
        public static string CleanFileName(string fileName, string defaultName = "ProjetoSemTitulo", int? maxLength = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return defaultName;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleaned = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            if (string.IsNullOrWhiteSpace(cleaned))
                return defaultName;

            cleaned = cleaned.Trim();

            if (maxLength.HasValue && cleaned.Length > maxLength.Value)
            {
                cleaned = cleaned[..maxLength.Value].Trim();
            }

            return cleaned;
        }

        /// <summary>
        /// Obtém as configurações padrão do projeto para um novo projeto
        /// </summary>
        public static ProjectSettings CreateDefaultProject(string projectName = null)
        {
            return new ProjectSettings
            {
                ProjectName = GetUniqueProjectName("Novo Programa"),
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now,
                RobotSettings = new RobotSettings(),
                ConnectionSettings = new ConnectionSettings(),
                LoggingSettings = new LoggingSettings()
            };
        }

        /// <summary>
        /// Verifica se um diretório de projeto existe e contém um arquivo de projeto válido
        /// </summary>
        public static bool IsValidProjectDirectory(string path)
        {
            return !string.IsNullOrEmpty(path) && 
                   Directory.Exists(path) && 
                   ProjectService.ProjectExists(path);
        }
    }
}