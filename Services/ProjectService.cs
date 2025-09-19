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
                    settings.ProjectPath = ProjectUtilities.CreateProjectPath(settings.ProjectName);
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
        /// Renomeia o projeto (atualiza nome e pasta) e salva as configura��es.
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

                // Se j� existir pasta com o novo nome e n�o for a mesma pasta atual, gerar nome �nico
                var desiredPath = Path.Combine(root, ProjectUtilities.CleanFileName(targetName));

                // Se o projeto ainda n�o tem pasta, criar uma
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

                // Garantir que n�o vamos sobrescrever outro projeto
                if (!string.Equals(oldPath, desiredPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Se j� existir, gerar nome �nico
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

                // Garantir diret�rio raiz
                Directory.CreateDirectory(root);

                // Move diret�rio
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
        /// Exclui um projeto movendo-o para a pasta de exclu�dos
        /// </summary>
        /// <param name="projectPath">Caminho do projeto a ser exclu�do</param>
        /// <returns>True se o projeto foi exclu�do com sucesso, False caso contr�rio</returns>
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
        /// Move um projeto para a pasta de exclu�dos
        /// </summary>
        /// <param name="projectPath">Caminho do projeto a ser movido</param>
        private static void MoveProjectToDeletedFolder(string projectPath)
        {
            // Criar pasta "Exclu�dos" se n�o existir
            var deletedFolder = ProjectUtilities.GetDeletedProjectsPath();
            if (!Directory.Exists(deletedFolder))
            {
                Directory.CreateDirectory(deletedFolder);
            }

            // Obter o nome da pasta do projeto
            var projectFolderName = Path.GetFileName(projectPath);
            var destinationPath = Path.Combine(deletedFolder, projectFolderName);

            // Se j� existir uma pasta com o mesmo nome na pasta de exclu�dos, adicionar timestamp
            if (Directory.Exists(destinationPath))
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var newFolderName = $"{projectFolderName}_{timestamp}";
                destinationPath = Path.Combine(deletedFolder, newFolderName);
            }

            // Mover a pasta
            Directory.Move(projectPath, destinationPath);
        }

        // M�todos de alto n�vel (vindos do antigo ProjectManager)

        /// <summary>
        /// Abre um projeto existente de forma ass�ncrona
        /// </summary>
        /// <param name="project">Configura��es do projeto a ser aberto</param>
        /// <returns>True se o projeto foi aberto com sucesso, False caso contr�rio</returns>
        public static async Task<bool> OpenProjectAsync(ProjectSettings project)
        {
            try
            {
                // Verificar se o projeto ainda existe
                if (!ProjectUtilities.IsValidProjectDirectory(project.ProjectPath))
                {
                    return false;
                }

                // Recarregar as configura��es do projeto para garantir que est�o atualizadas
                var updatedProject = await LoadProjectAsync(project.ProjectPath);
                
                // Atualizar a data de �ltimo acesso
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
        /// Exclui um projeto movendo-o para a pasta de exclu�dos
        /// </summary>
        /// <param name="project">Projeto a ser exclu�do</param>
        /// <returns>True se o projeto foi exclu�do com sucesso, False caso contr�rio</returns>
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
        /// Cria um novo projeto com configura��es padr�o e o salva
        /// </summary>
        /// <param name="projectName">Nome do projeto (opcional)</param>
        /// <returns>Configura��es do projeto criado ou null em caso de erro</returns>
        public static async Task<ProjectSettings> CreateNewProjectAsync(string projectName = null)
        {
            try
            {
                // Criar configura��es padr�o do projeto
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
        /// Valida se um projeto est� em condi��es adequadas para ser usado
        /// </summary>
        /// <param name="project">Projeto a ser validado</param>
        /// <returns>True se o projeto � v�lido, False caso contr�rio</returns>
        public static async Task<bool> ValidateProjectAsync(ProjectSettings project)
        {
            try
            {
                if (project == null)
                    return false;

                // Verificar se o diret�rio � v�lido
                if (!ProjectUtilities.IsValidProjectDirectory(project.ProjectPath))
                    return false;

                // Tentar carregar o projeto para verificar se o arquivo est� �ntegro
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