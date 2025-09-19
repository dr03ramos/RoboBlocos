using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Services;
using RoboBlocos.Utilities;
using System.Threading.Tasks;
using System;

namespace RoboBlocos
{
    /// <summary>
    /// Janela principal da aplica��o, permite opera��es principais e exibe a lista de projetos recentes
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

            // Carregar projetos recentes quando a janela � inicializada
            LoadRecentProjects();
        }

        /// <summary>
        /// Manipula o clique no bot�o de novo programa
        /// </summary>
        private async void NewProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Criar um novo projeto usando o ProjectService
                var projectSettings = await ProjectService.CreateNewProjectAsync(
                    ProjectUtilities.GetUniqueProjectName("Novo Programa"));

                if (projectSettings != null)
                {
                    // Fechar a janela principal e abrir o IDE
                    var ide = new IDE(projectSettings);
                    ide.Activate();
                    this.Close();
                }
                else
                {
                    await ShowDialogAsync("Erro", "N�o foi poss�vel criar um novo projeto.");
                }
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Erro", "N�o foi poss�vel criar um novo projeto.");
            }
        }

        /// <summary>
        /// Exibe um di�logo simples com uma mensagem
        /// </summary>
        /// <param name="title">T�tulo do di�logo</param>
        /// <param name="content">Conte�do do di�logo</param>
        private async Task ShowDialogAsync(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Exibe um di�logo de confirma��o com bot�es personalizados
        /// </summary>
        /// <param name="title">T�tulo do di�logo</param>
        /// <param name="content">Conte�do do di�logo</param>
        /// <param name="primaryText">Texto do bot�o prim�rio</param>
        /// <param name="cancelText">Texto do bot�o de cancelar</param>
        /// <returns>True se o usu�rio confirmou, False caso contr�rio</returns>
        private async Task<bool> ShowConfirmationDialogAsync(string title, string content, string primaryText, string cancelText)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                CloseButtonText = cancelText,
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
    }
}
