using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RoboBlocos.Models;
using Windows.Storage;

namespace RoboBlocos.Services
{
    /// <summary>
    /// Serviço para gerenciar tutoriais (carregar, salvar, excluir)
    /// </summary>
    public static class TutorialService
    {
        private static readonly string TutorialsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RoboBlocos",
            "Tutorials"
        );

        private static readonly string TutorialsIndexFile = Path.Combine(TutorialsFolder, "tutorials.json");

        /// <summary>
        /// Garante que a estrutura de pastas de tutoriais existe
        /// </summary>
        private static void EnsureTutorialsFolderExists()
        {
            if (!Directory.Exists(TutorialsFolder))
            {
                Directory.CreateDirectory(TutorialsFolder);
            }
        }

        /// <summary>
        /// Carrega todos os tutoriais disponíveis
        /// </summary>
        /// <returns>Lista de tutoriais</returns>
        public static async Task<List<Tutorial>> LoadTutorialsAsync()
        {
            EnsureTutorialsFolderExists();

            // Se não existe arquivo de índice, criar tutoriais iniciais
            if (!File.Exists(TutorialsIndexFile))
            {
                var initialTutorials = CreateInitialTutorials();
                await SaveTutorialsIndexAsync(initialTutorials);
                return initialTutorials;
            }

            try
            {
                var json = await File.ReadAllTextAsync(TutorialsIndexFile);
                var tutorials = JsonSerializer.Deserialize<List<Tutorial>>(json);
                return tutorials ?? new List<Tutorial>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao carregar tutoriais: {ex.Message}");
                return new List<Tutorial>();
            }
        }

        /// <summary>
        /// Salva o índice de tutoriais
        /// </summary>
        private static async Task SaveTutorialsIndexAsync(List<Tutorial> tutorials)
        {
            EnsureTutorialsFolderExists();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(tutorials, options);
            await File.WriteAllTextAsync(TutorialsIndexFile, json);
        }

        /// <summary>
        /// Salva ou atualiza um tutorial
        /// </summary>
        /// <param name="tutorial">Tutorial a ser salvo</param>
        public static async Task SaveTutorialAsync(Tutorial tutorial)
        {
            tutorial.LastModified = DateTime.Now;

            var tutorials = await LoadTutorialsAsync();
            var existingIndex = tutorials.FindIndex(t => t.Id == tutorial.Id);

            if (existingIndex >= 0)
            {
                tutorials[existingIndex] = tutorial;
            }
            else
            {
                tutorials.Add(tutorial);
            }

            await SaveTutorialsIndexAsync(tutorials);

            // Criar pasta específica do tutorial
            var tutorialFolder = Path.Combine(TutorialsFolder, $"tutorial-{tutorial.Id}");
            if (!Directory.Exists(tutorialFolder))
            {
                Directory.CreateDirectory(tutorialFolder);
            }

            // Criar pasta de imagens
            var imagesFolder = Path.Combine(tutorialFolder, "images");
            if (!Directory.Exists(imagesFolder))
            {
                Directory.CreateDirectory(imagesFolder);
            }

            // Salvar metadados do tutorial
            var metadataPath = Path.Combine(tutorialFolder, "metadata.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var metadataJson = JsonSerializer.Serialize(tutorial, options);
            await File.WriteAllTextAsync(metadataPath, metadataJson);
        }

        /// <summary>
        /// Exclui um tutorial
        /// </summary>
        /// <param name="tutorialId">ID do tutorial a ser excluído</param>
        public static async Task DeleteTutorialAsync(string tutorialId)
        {
            var tutorials = await LoadTutorialsAsync();
            tutorials.RemoveAll(t => t.Id == tutorialId);
            await SaveTutorialsIndexAsync(tutorials);

            // Excluir pasta do tutorial
            var tutorialFolder = Path.Combine(TutorialsFolder, $"tutorial-{tutorialId}");
            if (Directory.Exists(tutorialFolder))
            {
                try
                {
                    Directory.Delete(tutorialFolder, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao excluir pasta do tutorial: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Obtém o caminho da pasta de imagens de um tutorial
        /// </summary>
        /// <param name="tutorialId">ID do tutorial</param>
        /// <returns>Caminho da pasta de imagens</returns>
        public static string GetTutorialImagesFolder(string tutorialId)
        {
            var tutorialFolder = Path.Combine(TutorialsFolder, $"tutorial-{tutorialId}", "images");
            if (!Directory.Exists(tutorialFolder))
            {
                Directory.CreateDirectory(tutorialFolder);
            }
            return tutorialFolder;
        }

        /// <summary>
        /// Copia uma imagem para a pasta de imagens do tutorial
        /// </summary>
        /// <param name="tutorialId">ID do tutorial</param>
        /// <param name="sourceImagePath">Caminho da imagem de origem</param>
        /// <param name="imageName">Nome do arquivo de destino</param>
        /// <returns>Caminho relativo da imagem salva</returns>
        public static async Task<string> SaveTutorialImageAsync(string tutorialId, string sourceImagePath, string imageName)
        {
            var imagesFolder = GetTutorialImagesFolder(tutorialId);
            var extension = Path.GetExtension(sourceImagePath);
            var fileName = $"{imageName}{extension}";
            var destinationPath = Path.Combine(imagesFolder, fileName);

            await Task.Run(() => File.Copy(sourceImagePath, destinationPath, true));

            return Path.Combine("images", fileName);
        }

        /// <summary>
        /// Obtém o caminho completo de uma imagem do tutorial
        /// </summary>
        /// <param name="tutorialId">ID do tutorial</param>
        /// <param name="relativePath">Caminho relativo da imagem</param>
        /// <returns>Caminho completo da imagem</returns>
        public static string GetFullImagePath(string tutorialId, string relativePath)
        {
            var tutorialFolder = Path.Combine(TutorialsFolder, $"tutorial-{tutorialId}");
            return Path.Combine(tutorialFolder, relativePath);
        }

        /// <summary>
        /// Cria tutoriais iniciais para novos usuários
        /// </summary>
        private static List<Tutorial> CreateInitialTutorials()
        {
            return new List<Tutorial>
            {
            };
        }
    }
}
