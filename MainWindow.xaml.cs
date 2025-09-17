using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using RoboBlocos.Models;
using RoboBlocos.Utilities;

namespace RoboBlocos
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
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

        }

        private async void NewProgramButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new project with default settings
            ProjectSettings projectSettings = ProjectUtilities.CreateDefaultProject(ProjectUtilities.GetUniqueProjectName("Novo Programa"));

            // Close the main window and open the IDE window
            var ide = new IDE(projectSettings);
            ide.Activate();
            this.Close();
        }
    }
}
