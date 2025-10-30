using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RoboBlocos.Utilities
{
    /// <summary>
    /// Classe auxiliar para preservar e aplicar o tema da aplicação durante transições de janela
    /// </summary>
    public static class ThemeUtilities
    {
        private static ElementTheme _currentTheme = ElementTheme.Default;

        /// <summary>
        /// Obtém o tema atual da aplicação
        /// </summary>
        public static ElementTheme CurrentTheme => _currentTheme;

        /// <summary>
        /// Captura o tema atual de uma janela e armazena globalmente
        /// </summary>
        /// <param name="window">A janela da qual capturar o tema</param>
        public static void CaptureTheme(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                _currentTheme = rootElement.ActualTheme;
                System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Tema capturado: {_currentTheme}");
            }
        }

        /// <summary>
        /// Aplica o tema armazenado a uma janela
        /// </summary>
        /// <param name="window">A janela à qual aplicar o tema</param>
        public static void ApplyTheme(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = _currentTheme;
                System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Tema aplicado: {_currentTheme}");
            }
        }

        /// <summary>
        /// Transfere o tema de uma janela de origem para uma janela de destino
        /// </summary>
        /// <param name="sourceWindow">A janela da qual copiar o tema</param>
        /// <param name="targetWindow">A janela à qual aplicar o tema</param>
        public static void TransferTheme(Window sourceWindow, Window targetWindow)
        {
            CaptureTheme(sourceWindow);
            ApplyTheme(targetWindow);
        }

        /// <summary>
        /// Define o tema da aplicação e o aplica a uma janela
        /// </summary>
        /// <param name="window">A janela à qual aplicar o tema</param>
        /// <param name="theme">O tema a ser definido</param>
        public static void SetTheme(Window window, ElementTheme theme)
        {
            _currentTheme = theme;
            ApplyTheme(window);
        }

        /// <summary>
        /// Obtém o tema atual de uma janela específica
        /// </summary>
        /// <param name="window">A janela cujo tema será verificado</param>
        /// <returns>O tema atual (Light ou Dark)</returns>
        public static ElementTheme GetCurrentTheme(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                return rootElement.ActualTheme;
            }
            return ElementTheme.Default;
        }

        /// <summary>
        /// Alterna entre tema claro e escuro para a janela especificada
        /// </summary>
        /// <param name="window">A janela cujo tema será alternado</param>
        public static void ToggleTheme(Window window)
        {
            if (window.Content is FrameworkElement rootElement)
            {
                var currentTheme = rootElement.ActualTheme;
                var newTheme = currentTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                rootElement.RequestedTheme = newTheme;
                
                // Atualizar o tema global
                _currentTheme = newTheme;
            }
        }

        /// <summary>
        /// Atualiza o ícone e o texto de um botão de tema baseado no tema atual
        /// </summary>
        /// <param name="button">O botão a ser atualizado</param>
        /// <param name="currentTheme">O tema atual</param>
        public static void UpdateThemeButton(AppBarButton button, ElementTheme currentTheme)
        {
            if (currentTheme != ElementTheme.Light)
            {
                button.Icon = new FontIcon { Glyph = "\uE706" }; // Sol
                button.Label = "Claro";
            }
            else
            {
                button.Icon = new FontIcon { Glyph = "\uE708" }; // Lua
                button.Label = "Escuro";
            }
        }
    }
}
