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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
                    settings.ProjectPath = ProjectUtilities.CreateProjectPath(settings.ProjectName);
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
        /// Renomeia o projeto (atualiza nome e pasta) e salva as configurações.
        /// </summary>
        /// <param name="project">Projeto a ser renomeado</param>
        /// <param name="newName">Novo nome desejado</param>
        /// <returns>Projeto atualizado ou null em caso de erro</returns>
        public static async Task<ProjectSettings?> RenameProjectAsync(ProjectSettings project, string newName)
        {
            try
            {
                if (project == null) return null;

                var cleaned = ProjectUtilities.CleanFileName(newName, "ProjetoSemTitulo", 50);
                if (string.IsNullOrWhiteSpace(cleaned)) return null;

                // Caminhos atuais
                var oldPath = project.ProjectPath;
                var root = ProjectUtilities.GetProjectsRootPath();
                var targetName = cleaned;

                // Se já existir pasta com o novo nome e não for a mesma pasta atual, gerar nome único
                var desiredPath = Path.Combine(root, ProjectUtilities.CleanFileName(targetName));

                // Se o projeto ainda não tem pasta, criar uma
                if (string.IsNullOrWhiteSpace(oldPath))
                {
                    project.ProjectName = targetName;
                    project.ProjectPath = desiredPath;
                    return await SaveProjectAsync(project);
                }

                var oldFolderName = Path.GetFileName(oldPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.Equals(oldFolderName, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    // Apenas atualizar o nome interno e salvar
                    project.ProjectName = targetName;
                    return await SaveProjectAsync(project);
                }

                // Garantir que não vamos sobrescrever outro projeto
                if (!string.Equals(oldPath, desiredPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Se já existir, gerar nome único
                    int counter = 1;
                    var finalPath = desiredPath;
                    while (Directory.Exists(finalPath))
                    {
                        var candidate = $"{targetName} ({counter})";
                        finalPath = Path.Combine(root, ProjectUtilities.CleanFileName(candidate));
                        counter++;
                    }

                    desiredPath = finalPath;
                }

                // Garantir diretório raiz
                Directory.CreateDirectory(root);

                // Move diretório
                if (!string.Equals(oldPath, desiredPath, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.Move(oldPath, desiredPath);
                }

                project.ProjectName = targetName;
                project.ProjectPath = desiredPath;

                return await SaveProjectAsync(project);
            }
            catch
            {
                return null;
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
                string rootPath = ProjectUtilities.GetProjectsRootPath();
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

        /// <summary>
        /// Exclui um projeto movendo-o para a pasta de excluídos
        /// </summary>
        /// <param name="projectPath">Caminho do projeto a ser excluído</param>
        /// <returns>True se o projeto foi excluído com sucesso, False caso contrário</returns>
        public static async Task<bool> DeleteProjectAsync(string projectPath)
        {
            try
            {
                if (!Directory.Exists(projectPath))
                {
                    return false;
                }

                await Task.Run(() => MoveProjectToDeletedFolder(projectPath));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Move um projeto para a pasta de excluídos
        /// </summary>
        /// <param name="projectPath">Caminho do projeto a ser movido</param>
        private static void MoveProjectToDeletedFolder(string projectPath)
        {
            // Criar pasta "Excluídos" se não existir
            var deletedFolder = ProjectUtilities.GetDeletedProjectsPath();
            if (!Directory.Exists(deletedFolder))
            {
                Directory.CreateDirectory(deletedFolder);
            }

            // Obter o nome da pasta do projeto
            var projectFolderName = Path.GetFileName(projectPath);
            var destinationPath = Path.Combine(deletedFolder, projectFolderName);

            // Se já existir uma pasta com o mesmo nome na pasta de excluídos, adicionar timestamp
            if (Directory.Exists(destinationPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var newFolderName = $"{projectFolderName}_{timestamp}";
                destinationPath = Path.Combine(deletedFolder, newFolderName);
            }

            // Mover a pasta
            Directory.Move(projectPath, destinationPath);
        }

        // Métodos de alto nível (vindos do antigo ProjectManager)

        /// <summary>
        /// Abre um projeto existente de forma assíncrona
        /// </summary>
        /// <param name="project">Configurações do projeto a ser aberto</param>
        /// <returns>True se o projeto foi aberto com sucesso, False caso contrário</returns>
        public static async Task<bool> OpenProjectAsync(ProjectSettings project)
        {
            try
            {
                // Verificar se o projeto ainda existe
                if (!ProjectUtilities.IsValidProjectDirectory(project.ProjectPath))
                {
                    return false;
                }

                // Recarregar as configurações do projeto para garantir que estão atualizadas
                var updatedProject = await LoadProjectAsync(project.ProjectPath);
                
                // Atualizar a data de último acesso
                updatedProject.LastModified = DateTime.Now;
                await SaveProjectAsync(updatedProject);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exclui um projeto movendo-o para a pasta de excluídos
        /// </summary>
        /// <param name="project">Projeto a ser excluído</param>
        /// <returns>True se o projeto foi excluído com sucesso, False caso contrário</returns>
        public static async Task<bool> DeleteProjectAsync(ProjectSettings project)
        {
            try
            {
                if (string.IsNullOrEmpty(project.ProjectPath))
                {
                    return false;
                }

                return await DeleteProjectAsync(project.ProjectPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cria um novo projeto com configurações padrão e o salva
        /// </summary>
        /// <param name="projectName">Nome do projeto (opcional)</param>
        /// <returns>Configurações do projeto criado ou null em caso de erro</returns>
        public static async Task<ProjectSettings> CreateNewProjectAsync(string projectName = null)
        {
            try
            {
                // Criar configurações padrão do projeto
                var projectSettings = ProjectUtilities.CreateDefaultProject(projectName);
                
                // Salvar o projeto
                var savedProject = await SaveProjectAsync(projectSettings);
                
                return savedProject;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Valida se um projeto está em condições adequadas para ser usado
        /// </summary>
        /// <param name="project">Projeto a ser validado</param>
        /// <returns>True se o projeto é válido, False caso contrário</returns>
        public static async Task<bool> ValidateProjectAsync(ProjectSettings project)
        {
            try
            {
                if (project == null)
                    return false;

                // Verificar se o diretório é válido
                if (!ProjectUtilities.IsValidProjectDirectory(project.ProjectPath))
                    return false;

                // Tentar carregar o projeto para verificar se o arquivo está íntegro
                var loadedProject = await LoadProjectAsync(project.ProjectPath);
                
                return loadedProject != null;
            }
            catch
            {
                return false;
            }
        }
    }
}