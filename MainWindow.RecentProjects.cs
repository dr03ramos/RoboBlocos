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
                    // Ordenar por �ltimo salvamento (mais recente primeiro)
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
        /// Exibe mensagem quando n�o h� projetos dispon�veis
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

            // �cone do projeto
            settingsCard.HeaderIcon = new SymbolIcon(Symbol.Document);

            // Bot�o de op��es
            var optionsButton = CreateOptionsButton(project);
            settingsCard.Content = optionsButton;

            // Evento de clique no card para abrir o projeto
            settingsCard.Click += (s, e) => OpenProject(project);

            return settingsCard;
        }

        /// <summary>
        /// Cria o bot�o de op��es para um projeto
        /// </summary>
        /// <param name="project">Projeto para criar as op��es</param>
        /// <returns>DropDownButton com as op��es do projeto</returns>
        private DropDownButton CreateOptionsButton(ProjectSettings project)
        {
            var optionsButton = new DropDownButton
            {
                Content = new TextBlock { Text = "Op��es" }
            };

            var menuFlyout = new MenuFlyout
            {
                Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Bottom
            };

            var deleteItem = new MenuFlyoutItem
            {
                Text = "Excluir",
                Icon = new FontIcon { Glyph = "\uE74D" } // �cone de lixeira
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
                    await ShowDialogAsync("Projeto n�o encontrado", 
                        "O projeto selecionado n�o foi encontrado. Pode ter sido movido ou exclu�do.");
                    
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
                    await ShowDialogAsync("Erro", "N�o foi poss�vel abrir o projeto selecionado.");
                }
            }
            catch (Exception)
            {
                await ShowDialogAsync("Erro", "N�o foi poss�vel abrir o projeto selecionado.");
            }
        }

        /// <summary>
        /// Exclui um projeto ap�s confirma��o do usu�rio
        /// </summary>
        /// <param name="project">Projeto a ser exclu�do</param>
        private async void DeleteProject(ProjectSettings project)
        {
            var confirmed = await ShowConfirmationDialogAsync("Confirmar exclus�o", 
                $"Deseja realmente excluir o projeto \"{ProjectUtilities.GetProjectDisplayName(project)}\"?\n\nO projeto ser� movido para a pasta de exclu�dos e poder� ser recuperado.",
                "Excluir", "Cancelar");

            if (confirmed)
            {
                try
                {
                    var success = await ProjectService.DeleteProjectAsync(project);
                    
                    if (!success)
                    {
                        await ShowDialogAsync("Projeto n�o encontrado", 
                            "O diret�rio do projeto n�o foi encontrado. Pode ter sido movido ou exclu�do anteriormente.");
                    }
                }
                catch (Exception ex)
                {
                    await ShowDialogAsync("Erro ao excluir", 
                        $"N�o foi poss�vel excluir o projeto: {ex.Message}");
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
