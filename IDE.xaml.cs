using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace RoboBlocos
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class IDE : Window
    {
        public IDE()
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
        }
        private async void NavViewTitleBar_BackRequested(TitleBar sender, object args)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.XamlRoot = this.Content.XamlRoot;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = "Clicou para voltar";
            dialog.PrimaryButtonText = "OK";
            dialog.DefaultButton = ContentDialogButton.Primary;

            var result = await dialog.ShowAsync();
        }

        private void RobotSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingseditor = new SettingsEditor();
            settingseditor.Activate();
        }
    }
}
