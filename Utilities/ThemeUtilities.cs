using System;
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
                
                // Atualizar os botões de legenda da janela (minimize, maximize, close)
                UpdateWindowCaptionButtons(window);
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
                
                // Atualizar os botões de legenda da janela
                UpdateWindowCaptionButtons(window);
                
                System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Tema alternado para: {newTheme}");
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

        /// <summary>
        /// Aplica o tema atual a um ContentDialog
        /// </summary>
        /// <param name="dialog">O diálogo a ser tematizado</param>
        public static void ApplyThemeToDialog(ContentDialog dialog)
        {
            // Aplicar o tema atual ao diálogo
            dialog.RequestedTheme = _currentTheme;
            System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Tema aplicado ao diálogo: {_currentTheme}");
        }

        /// <summary>
        /// Atualiza os botões de legenda da janela (minimize, maximize, close) para corresponder ao tema
        /// </summary>
        /// <param name="window">A janela cujos botões de legenda serão atualizados</param>
        private static void UpdateWindowCaptionButtons(Window window)
        {
            try
            {
                if (window.Content is FrameworkElement rootElement)
                {
                    var actualTheme = rootElement.ActualTheme;
                    
                    // Determinar se devemos usar tema claro ou escuro para os botões
                    bool useLightTheme = actualTheme == ElementTheme.Light;
                    
                    // Atualizar a cor dos botões de legenda
                    if (window.AppWindow?.TitleBar != null)
                    {
                        var titleBar = window.AppWindow.TitleBar;
                        
                        if (useLightTheme)
                        {
                            // Tema claro: botões escuros em fundo claro
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(20, 0, 0, 0);
                            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
                            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(40, 0, 0, 0);
                        }
                        else
                        {
                            // Tema escuro: botões claros em fundo escuro
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(20, 255, 255, 255);
                            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(40, 255, 255, 255);
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Botões de legenda atualizados para tema: {(useLightTheme ? "Claro" : "Escuro")}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeUtilities] Erro ao atualizar botões de legenda: {ex.Message}");
            }
        }
    }
}
