using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RoboBlocos
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsEditor : Window
    {
        public SettingsEditor()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            // Define o tamanho da janela
            AppWindow.Resize(new SizeInt32(450, 800));

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;

            AppWindow.SetPresenter(presenter);
        }
    }
}
