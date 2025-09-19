using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Models;
using RoboBlocos.Services;
using System.Threading.Tasks;

namespace RoboBlocos
{
    /// <summary>
    /// Janela principal do IDE para desenvolvimento de projetos RoboBlocos
    /// </summary>
    public sealed partial class IDE : Window
    {
        public ProjectSettings CurrentProject { get; private set; }

        public IDE() : this(new ProjectSettings())
        {
        }

        public IDE(ProjectSettings projectSettings)
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            CurrentProject = projectSettings;
            UpdateWindowTitle();
        }

        /// <summary>
        /// Atualiza o t�tulo da janela com o nome do projeto atual
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (txtProgramName != null)
            {
                txtProgramName.Text = CurrentProject.ProjectName;
            }
        }

        /// <summary>
        /// Cria uma nova inst�ncia do IDE com um projeto espec�fico
        /// </summary>
        /// <param name="projectPath">Caminho para o arquivo de projeto</param>
        /// <returns>Nova inst�ncia do IDE</returns>
        public static async Task<IDE> CreateWithProjectAsync(string projectPath = null)
        {
            ProjectSettings settings;

            if (!string.IsNullOrEmpty(projectPath) && ProjectService.ProjectExists(projectPath))
            {
                try
                {
                    settings = await ProjectService.LoadProjectAsync(projectPath);
                }
                catch
                {
                    // Se o carregamento falhar, cria um novo projeto
                    settings = new ProjectSettings();
                }
            }
            else
            {
                settings = new ProjectSettings();
            }

            return new IDE(settings);
        }

        /// <summary>
        /// Manipula o evento de voltar da barra de t�tulo, perguntando se o usu�rio deseja salvar
        /// </summary>
        private async void NavViewTitleBar_BackRequested(TitleBar sender, object args)
        {
            // Pergunta ao usu�rio se deseja salvar antes de voltar
            ContentDialog dialog = new ContentDialog();

            dialog.XamlRoot = this.Content.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Salvar projeto antes de sair?";
            dialog.Content = "Deseja salvar as altera��es no projeto antes de voltar ao menu principal?";
            dialog.PrimaryButtonText = "Salvar e Sair";
            dialog.SecondaryButtonText = "Sair sem Salvar";
            dialog.CloseButtonText = "Cancelar";
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await ProjectService.SaveProjectAsync(CurrentProject);
                    GoBackToMainWindow();
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync("Erro ao salvar", $"N�o foi poss�vel salvar o projeto: {ex.Message}");
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                GoBackToMainWindow();
            }
        }

        /// <summary>
        /// Clique no bot�o Salvar
        /// </summary>
        private async void SaveProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ProjectService.SaveProjectAsync(CurrentProject);
                await ShowInfoAsync("Projeto salvo", "As altera��es foram salvas com sucesso.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Erro ao salvar", $"N�o foi poss�vel salvar o projeto: {ex.Message}");
            }
        }

        /// <summary>
        /// Clique no bot�o Renomear
        /// </summary>
        private async void RenameProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var input = new TextBox { Text = CurrentProject.ProjectName, Width = 300 };

            var dialog = new ContentDialog
            {
                Title = "Renomear projeto",
                Content = input,
                PrimaryButtonText = "Renomear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var newName = input.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                await ShowErrorAsync("Nome inv�lido", "Informe um nome v�lido.");
                return;
            }

            var updated = await ProjectService.RenameProjectAsync(CurrentProject, newName);
            if (updated == null)
            {
                await ShowErrorAsync("Falha ao renomear", "N�o foi poss�vel renomear o projeto.");
                return;
            }

            CurrentProject = updated;
            UpdateWindowTitle();
        }

        /// <summary>
        /// Volta para a janela principal fechando a janela atual
        /// </summary>
        private void GoBackToMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }

        /// <summary>
        /// Manipula o clique no bot�o de configura��es do rob�
        /// </summary>
        private void RobotSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsEditor = new SettingsEditor(CurrentProject);
            settingsEditor.Activate();
        }

        /// <summary>
        /// Exibe uma mensagem de erro em um di�logo
        /// </summary>
        /// <param name="title">T�tulo do di�logo</param>
        /// <param name="message">Mensagem de erro a ser exibida</param>
        private async Task ShowErrorAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();
        }

        private async Task ShowInfoAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            await dialog.ShowAsync();
        }
    }
}
