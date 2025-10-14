using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT;

namespace RoboBlocos.Utilities
{
    /// <summary>
    /// Classe auxiliar para preservar e aplicar o tamanho e a posição da janela durante transições de janela
    /// </summary>
    public static class TamanhoJanelaUtilities
    {
        /// <summary>
        /// Captura a posição atual, tamanho e estado de uma janela
        /// </summary>
        /// <param name="window">A janela da qual capturar os limites</param>
        /// <returns>WindowBounds contendo posição, tamanho e estado, ou null se a captura falhar</returns>
        public static WindowDetails? CaptureWindowBounds(Window window)
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    var presenter = appWindow.Presenter as OverlappedPresenter;
                    bool isMaximized = presenter?.State == OverlappedPresenterState.Maximized;

                    return new WindowDetails
                    {
                        Position = appWindow.Position,
                        Size = appWindow.Size,
                        IsMaximized = isMaximized
                    };
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao capturar os limites da janela: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Aplica os limites salvos a uma janela de destino
        /// </summary>
        /// <param name="targetWindow">A janela à qual aplicar os limites</param>
        /// <param name="bounds">Os limites a aplicar</param>
        /// <returns>True se os limites foram aplicados com sucesso, false caso contrário</returns>
        public static bool ApplyWindowBounds(Window targetWindow, WindowDetails? bounds)
        {
            if (bounds == null)
                return false;

            try
            {
                // Garante que a janela esteja ativada para ter um HWND válido
                targetWindow.Activate();

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(targetWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    var presenter = appWindow.Presenter as OverlappedPresenter;
                    
                    if (presenter != null)
                    {
                        // Se a janela estava maximizada, restaure-a primeiro para definir os limites normais
                        // Isso garante que quando o usuário desmaximizar, ela retorne ao tamanho/posição corretos
                        if (bounds.IsMaximized)
                        {
                            // Define os limites restaurados (posição e tamanho do estado normal)
                            // Estes serão usados quando o usuário desmaximizar a janela
                            presenter.Restore();
                            appWindow.Move(bounds.Position);
                            appWindow.Resize(bounds.Size);
                            
                            // Agora maximiza a janela
                            presenter.Maximize();
                        }
                        else
                        {
                            presenter.Restore();
                            appWindow.Move(bounds.Position);
                            appWindow.Resize(bounds.Size);
                        }
                        
                        return true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Falha ao aplicar os limites da janela: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Transfere os limites de uma janela de origem para uma janela de destino
        /// </summary>
        /// <param name="sourceWindow">A janela da qual copiar os limites</param>
        /// <param name="targetWindow">A janela à qual aplicar os limites</param>
        /// <returns>True se a transferência foi bem-sucedida, false caso contrário</returns>
        public static bool TransferWindowBounds(Window sourceWindow, Window targetWindow)
        {
            var bounds = CaptureWindowBounds(sourceWindow);
            return ApplyWindowBounds(targetWindow, bounds);
        }
    }

    /// <summary>
    /// Representa informações de posição, tamanho e estado da janela
    /// </summary>
    public class WindowDetails
    {
        /// <summary>
        /// A posição da janela (canto superior esquerdo) em coordenadas da tela
        /// </summary>
        public PointInt32 Position { get; set; }
        
        /// <summary>
        /// O tamanho da janela (largura e altura)
        /// </summary>
        public SizeInt32 Size { get; set; }
        
        /// <summary>
        /// Se a janela estava maximizada
        /// </summary>
        public bool IsMaximized { get; set; }
    }
}
