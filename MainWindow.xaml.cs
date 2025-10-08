using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Services;
using RoboBlocos.Utilities;
using System.Threading.Tasks;
using System;
using Windows.Storage.Pickers;
using WinRT.Interop;
using RoboBlocos.Models;

namespace RoboBlocos
{
    /// <summary>
    /// Janela principal da aplicação, permite operações principais e exibe a lista de projetos recentes
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 1000;
            presenter.PreferredMinimumHeight = 400;

            AppWindow.SetPresenter(presenter);

            // Carregar projetos recentes quando a janela é inicializada
            LoadRecentProjects();
        }

        /// <summary>
        /// Manipula o clique no botão de novo programa
        /// </summary>
        private void NewProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Criar um novo projeto sem salvá-lo (estado = New)
                var projectSettings = ProjectUtilities.CreateDefaultProject(
                    ProjectUtilities.GetUniqueProjectName("Novo Programa"));
                
                // Define explicitamente o estado como New
                projectSettings.State = ProjectState.New;

                // Fechar a janela principal e abrir o IDE
                var ide = new IDE(projectSettings);
                ide.Activate();
                this.Close();
            }
            catch (Exception)
            {
                _ = JanelaUtilities.ShowSimpleDialogAsync(this, "Erro", "Não foi possível criar um novo projeto.");
            }
        }

        /// <summary>
        /// Manipula o clique no botão de importar programa
        /// </summary>
        private async void ImportProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                    return;

                // Validar e carregar o projeto
                var path = folder.Path;
                if (!ProjectService.ProjectExists(path))
                {
                    await JanelaUtilities.ShowSimpleDialogAsync(this, "Projeto inválido", "A pasta selecionada não contém um projeto RoboBlocos válido.");
                    return;
                }

                var settings = await ProjectService.LoadProjectAsync(path);
                if (settings == null)
                {
                    await JanelaUtilities.ShowSimpleDialogAsync(this, "Erro", "Não foi possível carregar o projeto selecionado.");
                    return;
                }

                // Abrir IDE com o projeto (estado será Saved após LoadProjectAsync)
                var ide = new IDE(settings);
                ide.Activate();
                this.Close();
            }
            catch (Exception)
            {
                await JanelaUtilities.ShowSimpleDialogAsync(this, "Erro", "Falha ao importar o projeto.");
            }
        }
    }
}
