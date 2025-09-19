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
        private const string PROJECTS_FOLDER = "ProjetosRoboBlocos";
        private const string DELETED_FOLDER = "Excluídos";

        /// <summary>
        /// Obtém o caminho raiz onde os projetos são armazenados
        /// </summary>
        /// <returns>Caminho completo para a pasta de projetos</returns>
        public static string GetProjectsRootPath()
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documentsPath, PROJECTS_FOLDER);
        }

        /// <summary>
        /// Obtém o caminho para a pasta de projetos excluídos
        /// </summary>
        /// <returns>Caminho completo para a pasta de projetos excluídos</returns>
        public static string GetDeletedProjectsPath()
        {
            return Path.Combine(GetProjectsRootPath(), DELETED_FOLDER);
        }

        /// <summary>
        /// Obtém uma lista de projetos acessados recentemente
        /// </summary>
        /// <param name="maxCount">Número máximo de projetos a retornar</param>
        /// <returns>Lista de configurações de projetos recentes</returns>
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
        /// <param name="baseName">Nome base para o projeto</param>
        /// <returns>Nome único para o projeto</returns>
        public static string GetUniqueProjectName(string baseName)
        {
            var rootPath = GetProjectsRootPath();
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
        /// Cria o caminho completo para um novo projeto baseado no nome fornecido
        /// </summary>
        /// <param name="projectName">Nome do projeto</param>
        /// <returns>Caminho completo para o diretório do projeto</returns>
        public static string CreateProjectPath(string projectName)
        {
            string rootPath = GetProjectsRootPath();
            string cleanedName = CleanFileName(projectName, "ProjetoSemTitulo", 50);
            return Path.Combine(rootPath, cleanedName);
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
        /// <param name="projectName">Nome do projeto (opcional)</param>
        /// <returns>Configurações padrão do projeto</returns>
        public static ProjectSettings CreateDefaultProject(string projectName = null)
        {
            var finalName = string.IsNullOrEmpty(projectName) 
                ? GetUniqueProjectName("Novo Programa") 
                : projectName;

            return new ProjectSettings
            {
                ProjectName = finalName,
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
        /// <param name="path">Caminho do diretório a verificar</param>
        /// <returns>True se é um diretório de projeto válido, False caso contrário</returns>
        public static bool IsValidProjectDirectory(string path)
        {
            return !string.IsNullOrEmpty(path) && 
                   Directory.Exists(path) && 
                   ProjectService.ProjectExists(path);
        }

        /// <summary>
        /// Cria um projeto único com nome gerado automaticamente e o salva
        /// </summary>
        /// <param name="baseName">Nome base para o projeto (opcional)</param>
        /// <returns>Configurações do projeto criado</returns>
        public static async Task<ProjectSettings> CreateUniqueProjectAsync(string baseName = "Novo Programa")
        {
            var uniqueName = GetUniqueProjectName(baseName);
            var projectSettings = CreateDefaultProject(uniqueName);
            
            return await ProjectService.SaveProjectAsync(projectSettings);
        }

        /// <summary>
        /// Obtém o nome de exibição formatado para um projeto
        /// </summary>
        /// <param name="project">Projeto para formatar o nome</param>
        /// <returns>Nome formatado para exibição</returns>
        public static string GetProjectDisplayName(ProjectSettings project)
        {
            if (project == null || string.IsNullOrEmpty(project.ProjectName))
            {
                return "Projeto sem nome";
            }

            return project.ProjectName;
        }

        /// <summary>
        /// Obtém a descrição formatada de um projeto para exibição na interface
        /// </summary>
        /// <param name="project">Projeto para obter a descrição</param>
        /// <returns>Descrição formatada do projeto</returns>
        public static string GetProjectDisplayDescription(ProjectSettings project)
        {
            if (project?.RobotSettings == null || project.ConnectionSettings == null)
            {
                return "Configurações indisponíveis";
            }

            return $"Robô {project.RobotSettings.Model} | Porta {project.ConnectionSettings.SerialPort} | Modificado em {project.LastModified:dd/MM/yyyy HH:mm}";
        }
    }
}