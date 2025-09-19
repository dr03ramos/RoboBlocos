using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Models;
using RoboBlocos.Services;
using RoboBlocos.Utilities;
using System;
using System.Linq;
using CommunityToolkit.WinUI.Controls;

namespace RoboBlocos
{
    public sealed partial class MainWindow
    {
        /// <summary>
        /// Carrega e exibe os projetos recentes na interface
        /// </summary>
        private async void LoadRecentProjects()
        {
            try
            {
                var recentProjects = await ProjectUtilities.GetRecentProjectsAsync(10);
                
                // Limpar o container de projetos recentes
                RecentProjectsContainer.Children.Clear();

                if (recentProjects.Any())
                {
                    // Ordenar por último salvamento (mais recente primeiro)
                    var sortedProjects = recentProjects.OrderByDescending(p => p.LastModified);

                    foreach (var project in sortedProjects)
                    {
                        var settingsCard = CreateProjectCard(project);
                        RecentProjectsContainer.Children.Add(settingsCard);
                    }
                }
                else
                {
                    ShowNoProjectsMessage();
                }
            }
            catch (Exception)
            {
                ShowErrorMessage("Erro ao carregar projetos recentes.");
            }
        }

        /// <summary>
        /// Exibe mensagem quando não há projetos disponíveis
        /// </summary>
        private void ShowNoProjectsMessage()
        {
            var noProjectsMessage = new TextBlock
            {
                Text = "Nenhum projeto encontrado. Crie seu primeiro programa clicando em \"Novo programa\".",
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 20, 0, 0)
            };
            RecentProjectsContainer.Children.Add(noProjectsMessage);
        }

        /// <summary>
        /// Exibe mensagem de erro na interface
        /// </summary>
        /// <param name="message">Mensagem de erro a ser exibida</param>
        private void ShowErrorMessage(string message)
        {
            RecentProjectsContainer.Children.Clear();
            var errorMessage = new TextBlock
            {
                Text = message,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Opacity = 0.7,
                Margin = new Thickness(0, 20, 0, 0)
            };
            RecentProjectsContainer.Children.Add(errorMessage);
        }

        /// <summary>
        /// Cria um card visual para representar um projeto na interface
        /// </summary>
        /// <param name="project">Projeto para criar o card</param>
        /// <returns>SettingsCard configurado para o projeto</returns>
        private SettingsCard CreateProjectCard(ProjectSettings project)
        {
            var settingsCard = new SettingsCard
            {
                Header = ProjectUtilities.GetProjectDisplayName(project),
                Description = ProjectUtilities.GetProjectDisplayDescription(project),
                IsClickEnabled = true,
                Margin = new Thickness(0, 0, 0, 4)
            };

            // Ícone do projeto
            settingsCard.HeaderIcon = new SymbolIcon(Symbol.Document);

            // Botão de opções
            var optionsButton = CreateOptionsButton(project);
            settingsCard.Content = optionsButton;

            // Evento de clique no card para abrir o projeto
            settingsCard.Click += (s, e) => OpenProject(project);

            return settingsCard;
        }

        /// <summary>
        /// Cria o botão de opções para um projeto
        /// </summary>
        /// <param name="project">Projeto para criar as opções</param>
        /// <returns>DropDownButton com as opções do projeto</returns>
        private DropDownButton CreateOptionsButton(ProjectSettings project)
        {
            var optionsButton = new DropDownButton
            {
                Content = new TextBlock { Text = "Opções" }
            };

            var menuFlyout = new MenuFlyout
            {
                Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
            };

            var deleteItem = new MenuFlyoutItem
            {
                Text = "Excluir",
                Icon = new FontIcon { Glyph = "\uE74D" } // Ícone de lixeira
            };

            deleteItem.Click += (s, e) => DeleteProject(project);
            menuFlyout.Items.Add(deleteItem);

            optionsButton.Flyout = menuFlyout;
            return optionsButton;
        }

        /// <summary>
        /// Abre um projeto existente no IDE
        /// </summary>
        /// <param name="project">Projeto a ser aberto</param>
        private async void OpenProject(ProjectSettings project)
        {
            try
            {
                // Verificar se o projeto ainda existe usando o ProjectService
                var isValid = await ProjectService.ValidateProjectAsync(project);
                if (!isValid)
                {
                    await ShowDialogAsync("Projeto não encontrado", 
                        "O projeto selecionado não foi encontrado. Pode ter sido movido ou excluído.");
                    
                    // Recarregar a lista de projetos
                    LoadRecentProjects();
                    return;
                }

                // Tentar abrir o projeto
                var success = await ProjectService.OpenProjectAsync(project);
                if (success)
                {
                    // Fechar a janela principal e abrir o IDE
                    var ide = new IDE(project);
                    ide.Activate();
                    this.Close();
                }
                else
                {
                    await ShowDialogAsync("Erro", "Não foi possível abrir o projeto selecionado.");
                }
            }
            catch (Exception)
            {
                await ShowDialogAsync("Erro", "Não foi possível abrir o projeto selecionado.");
            }
        }

        /// <summary>
        /// Exclui um projeto após confirmação do usuário
        /// </summary>
        /// <param name="project">Projeto a ser excluído</param>
        private async void DeleteProject(ProjectSettings project)
        {
            var confirmed = await ShowConfirmationDialogAsync("Confirmar exclusão", 
                $"Deseja realmente excluir o projeto \"{ProjectUtilities.GetProjectDisplayName(project)}\"?\n\nO projeto será movido para a pasta de excluídos e poderá ser recuperado.",
                "Excluir", "Cancelar");

            if (confirmed)
            {
                try
                {
                    var success = await ProjectService.DeleteProjectAsync(project);
                    
                    if (!success)
                    {
                        await ShowDialogAsync("Projeto não encontrado", 
                            "O diretório do projeto não foi encontrado. Pode ter sido movido ou excluído anteriormente.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Erro ao excluir", 
                        $"Não foi possível excluir o projeto: {ex.Message}");
                }
                finally
                {
                    // Recarregar a lista independentemente do resultado
                    LoadRecentProjects();
                }
            }
        }
    }
}
