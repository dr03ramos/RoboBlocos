using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RoboBlocos.Models;
using RoboBlocos.Utilities;

namespace RoboBlocos.Services
{
    public class ProjectService
    {
        private const string PROJECT_FILE_NAME = "project.json";
        private const string PROJECTS_FOLDER = "ProjetosRoboBlocos";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
        /// Cria o caminho completo para um novo projeto baseado no nome fornecido
        /// </summary>
        /// <param name="projectName">Nome do projeto</param>
        /// <returns>Caminho completo para o diretório do projeto</returns>
        public static string CreateProjectPath(string projectName)
        {
            string rootPath = GetProjectsRootPath();
            string cleanedName = ProjectUtilities.CleanFileName(projectName, "ProjetoSemTitulo", 50);
            return Path.Combine(rootPath, cleanedName);
        }

        /// <summary>
        /// Salva as configurações do projeto de forma assíncrona
        /// </summary>
        /// <param name="settings">Configurações do projeto a serem salvas</param>
        /// <returns>Configurações atualizadas do projeto</returns>
        /// <exception cref="InvalidOperationException">Lançada quando não é possível salvar o projeto</exception>
        public static async Task<ProjectSettings> SaveProjectAsync(ProjectSettings settings)
        {
            try
            {
                // Atualiza o tempo da última modificação
                settings.LastModified = DateTime.Now;

                // Cria o caminho do projeto se não estiver definido
                if (string.IsNullOrEmpty(settings.ProjectPath))
                {
                    settings.ProjectPath = CreateProjectPath(settings.ProjectName);
                }

                // Garante que o diretório existe
                Directory.CreateDirectory(settings.ProjectPath);

                // Salva as configurações do projeto
                string projectFilePath = Path.Combine(settings.ProjectPath, PROJECT_FILE_NAME);
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                await File.WriteAllTextAsync(projectFilePath, json);

                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Falha ao salvar o projeto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Carrega as configurações de um projeto de forma assíncrona
        /// </summary>
        /// <param name="projectPath">Caminho para o diretório do projeto</param>
        /// <returns>Configurações do projeto carregadas</returns>
        /// <exception cref="FileNotFoundException">Lançada quando o arquivo do projeto não é encontrado</exception>
        /// <exception cref="InvalidOperationException">Lançada quando não é possível carregar o projeto</exception>
        public static async Task<ProjectSettings> LoadProjectAsync(string projectPath)
        {
            try
            {
                string projectFilePath = Path.Combine(projectPath, PROJECT_FILE_NAME);
                
                if (!File.Exists(projectFilePath))
                {
                    throw new FileNotFoundException($"Arquivo do projeto não encontrado: {projectFilePath}");
                }

                string json = await File.ReadAllTextAsync(projectFilePath);
                var settings = JsonSerializer.Deserialize<ProjectSettings>(json, JsonOptions);

                if (settings == null)
                {
                    throw new InvalidOperationException("Falha ao deserializar as configurações do projeto");
                }

                // Atualiza o caminho do projeto caso tenha sido movido
                settings.ProjectPath = projectPath;
                return settings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Falha ao carregar o projeto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Verifica se um projeto existe no caminho especificado
        /// </summary>
        /// <param name="projectPath">Caminho para verificar</param>
        /// <returns>True se o projeto existe, False caso contrário</returns>
        public static bool ProjectExists(string projectPath)
        {
            return File.Exists(Path.Combine(projectPath, PROJECT_FILE_NAME));
        }

        /// <summary>
        /// Obtém todos os projetos existentes no diretório raiz
        /// </summary>
        /// <returns>Array de DirectoryInfo com os projetos encontrados, ordenados por data de modificação</returns>
        public static DirectoryInfo[] GetAllProjects()
        {
            try
            {
                string rootPath = GetProjectsRootPath();
                if (!Directory.Exists(rootPath))
                {
                    return Array.Empty<DirectoryInfo>();
                }

                var rootDir = new DirectoryInfo(rootPath);
                return rootDir.GetDirectories()
                    .Where(dir => ProjectExists(dir.FullName))
                    .OrderByDescending(dir => dir.LastWriteTime)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<DirectoryInfo>();
            }
        }
    }
}