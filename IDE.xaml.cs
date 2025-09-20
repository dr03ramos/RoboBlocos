using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Models;
using RoboBlocos.Services;
using System.Threading.Tasks;
using RoboBlocos.Utilities;

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
        /// Atualiza o título da janela com o nome do projeto atual
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (txtProgramName != null)
            {
                txtProgramName.Text = CurrentProject.ProjectName;
            }
        }

        /// <summary>
        /// Cria uma nova instância do IDE com um projeto específico
        /// </summary>
        /// <param name="projectPath">Caminho para o arquivo de projeto</param>
        /// <returns>Nova instância do IDE</returns>
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
        /// Manipula o evento de voltar da barra de título, perguntando se o usuário deseja salvar
        /// </summary>
        private async void NavViewTitleBar_BackRequested(TitleBar sender, object args)
        {
            var result = await JanelaUtilities.ShowSaveBeforeExitDialogAsync(this);

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await ProjectService.SaveProjectAsync(CurrentProject);
                    GoBackToMainWindow();
                }
                catch (Exception ex)
                {
                    await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o projeto: {ex.Message}");
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                GoBackToMainWindow();
            }
        }

        /// <summary>
        /// Clique no botão Salvar
        /// </summary>
        private async void SaveProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ProjectService.SaveProjectAsync(CurrentProject);
                await JanelaUtilities.ShowInfoDialogAsync(this, "Projeto salvo", "As alterações foram salvas com sucesso.");
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o projeto: {ex.Message}");
            }
        }

        /// <summary>
        /// Clique no botão Renomear
        /// </summary>
        private async void RenameProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var newName = await JanelaUtilities.ShowRenameDialogAsync(this, CurrentProject.ProjectName);

            // Se cancelado, não mostrar mensagem
            if (newName is null)
                return;

            // Validação adicional por segurança
            if (string.IsNullOrWhiteSpace(newName))
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Nome inválido", "Informe um nome válido.");
                return;
            }

            var updated = await ProjectService.RenameProjectAsync(CurrentProject, newName);
            if (updated == null)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Falha ao renomear", "Não foi possível renomear o projeto.");
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
        /// Manipula o clique no botão de configurações do robô
        /// </summary>
        private void RobotSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsEditor = new SettingsEditor(CurrentProject);
            settingsEditor.Activate();
        }
    }
}
