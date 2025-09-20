using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

namespace RoboBlocos.Utilities
{
    /// <summary>
    /// Classe utilitária para operações comuns de janela e diálogos no RoboBlocos.
    /// </summary>
    public static class JanelaUtilities
    {
        /// <summary>
        /// Exibe um diálogo simples de informação com um botão OK.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <param name="title">O título do diálogo.</param>
        /// <param name="content">O conteúdo/mensagem do diálogo.</param>
        public static async Task ShowSimpleDialogAsync(Window window, string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = window.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Exibe um diálogo de confirmação com botões personalizáveis.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <param name="title">O título do diálogo.</param>
        /// <param name="content">O conteúdo/mensagem do diálogo.</param>
        /// <param name="primaryText">Texto do botão primário (confirmar).</param>
        /// <param name="cancelText">Texto do botão de cancelar/fechar.</param>
        /// <returns>True se o botão primário foi clicado, false caso contrário.</returns>
        public static async Task<bool> ShowConfirmationDialogAsync(Window window, string title, string content, string primaryText, string cancelText)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                CloseButtonText = cancelText,
                XamlRoot = window.Content.XamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        /// <summary>
        /// Exibe um diálogo de erro.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <param name="title">O título do diálogo.</param>
        /// <param name="message">A mensagem de erro.</param>
        public static async Task ShowErrorDialogAsync(Window window, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = window.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Exibe um diálogo informativo.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <param name="title">O título do diálogo.</param>
        /// <param name="message">A mensagem de informação.</param>
        public static async Task ShowInfoDialogAsync(Window window, string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = window.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Exibe um diálogo para renomear com uma caixa de texto de entrada.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <param name="currentName">O nome atual para pré-preencher na entrada.</param>
        /// <returns>O novo nome se confirmado, ou null se cancelado.</returns>
        public static async Task<string?> ShowRenameDialogAsync(Window window, string currentName)
        {
            var input = new TextBox { Text = currentName, Width = 300, PlaceholderText = "Digite o novo nome" };
            var dialog = new ContentDialog
            {
                Title = "Renomear projeto",
                Content = input,
                PrimaryButtonText = "Renomear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = window.Content.XamlRoot
            };

            // Habilitar o botão 'Renomear' apenas quando houver texto válido
            void UpdatePrimaryEnabled()
            {
                var text = input.Text?.Trim();
                dialog.IsPrimaryButtonEnabled = !string.IsNullOrWhiteSpace(text);
            }

            UpdatePrimaryEnabled();
            input.TextChanged += (s, e) => UpdatePrimaryEnabled();

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return null;

            var newName = input.Text?.Trim();
            return newName;
        }

        /// <summary>
        /// Exibe um diálogo de confirmação para salvar antes de sair.
        /// </summary>
        /// <param name="window">A janela à qual o diálogo estará associado (para XamlRoot).</param>
        /// <returns>O resultado do diálogo: Primary (Salvar e Sair), Secondary (Sair sem Salvar) ou None (Cancelar).</returns>
        public static async Task<ContentDialogResult> ShowSaveBeforeExitDialogAsync(Window window)
        {
            var dialog = new ContentDialog
            {
                Content = "Deseja salvar as alterações no projeto antes de voltar ao menu principal?",
                PrimaryButtonText = "Salvar e Sair",
                SecondaryButtonText = "Sair sem Salvar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = window.Content.XamlRoot
            };
            return await dialog.ShowAsync();
        }
    }
}
