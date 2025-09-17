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
        /// Obt�m o caminho raiz onde os projetos s�o armazenados
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
        /// <returns>Caminho completo para o diret�rio do projeto</returns>
        public static string CreateProjectPath(string projectName)
        {
            string rootPath = GetProjectsRootPath();
            string cleanedName = ProjectUtilities.CleanFileName(projectName, "ProjetoSemTitulo", 50);
            return Path.Combine(rootPath, cleanedName);
        }

        /// <summary>
        /// Salva as configura��es do projeto de forma ass�ncrona
        /// </summary>
        /// <param name="settings">Configura��es do projeto a serem salvas</param>
        /// <returns>Configura��es atualizadas do projeto</returns>
        /// <exception cref="InvalidOperationException">Lan�ada quando n�o � poss�vel salvar o projeto</exception>
        public static async Task<ProjectSettings> SaveProjectAsync(ProjectSettings settings)
        {
            try
            {
                // Atualiza o tempo da �ltima modifica��o
                settings.LastModified = DateTime.Now;

                // Cria o caminho do projeto se n�o estiver definido
                if (string.IsNullOrEmpty(settings.ProjectPath))
                {
                    settings.ProjectPath = CreateProjectPath(settings.ProjectName);
                }

                // Garante que o diret�rio existe
                Directory.CreateDirectory(settings.ProjectPath);

                // Salva as configura��es do projeto
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
        /// Carrega as configura��es de um projeto de forma ass�ncrona
        /// </summary>
        /// <param name="projectPath">Caminho para o diret�rio do projeto</param>
        /// <returns>Configura��es do projeto carregadas</returns>
        /// <exception cref="FileNotFoundException">Lan�ada quando o arquivo do projeto n�o � encontrado</exception>
        /// <exception cref="InvalidOperationException">Lan�ada quando n�o � poss�vel carregar o projeto</exception>
        public static async Task<ProjectSettings> LoadProjectAsync(string projectPath)
        {
            try
            {
                string projectFilePath = Path.Combine(projectPath, PROJECT_FILE_NAME);
                
                if (!File.Exists(projectFilePath))
                {
                    throw new FileNotFoundException($"Arquivo do projeto n�o encontrado: {projectFilePath}");
                }

                string json = await File.ReadAllTextAsync(projectFilePath);
                var settings = JsonSerializer.Deserialize<ProjectSettings>(json, JsonOptions);

                if (settings == null)
                {
                    throw new InvalidOperationException("Falha ao deserializar as configura��es do projeto");
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
        /// <returns>True se o projeto existe, False caso contr�rio</returns>
        public static bool ProjectExists(string projectPath)
        {
            return File.Exists(Path.Combine(projectPath, PROJECT_FILE_NAME));
        }

        /// <summary>
        /// Obt�m todos os projetos existentes no diret�rio raiz
        /// </summary>
        /// <returns>Array de DirectoryInfo com os projetos encontrados, ordenados por data de modifica��o</returns>
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