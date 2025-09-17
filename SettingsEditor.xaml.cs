using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using RoboBlocos.Models;
using RoboBlocos.Services;
using System;
using System.Threading.Tasks;

namespace RoboBlocos
{
    /// <summary>
    /// Janela de edi��o de configura��es do projeto
    /// </summary>
    public sealed partial class SettingsEditor : Window
    {
        public ProjectSettings ProjectSettings { get; private set; }

        public SettingsEditor() : this(new ProjectSettings())
        {
        }

        public SettingsEditor(ProjectSettings projectSettings)
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            // Define o tamanho da janela
            AppWindow.Resize(new SizeInt32(450, 850));

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;

            AppWindow.SetPresenter(presenter);

            // Inicializa as configura��es do projeto
            ProjectSettings = projectSettings;

            // Atualiza o t�tulo da janela
            UpdateWindowTitle();

            // Atualiza a UI a partir das configura��es
            this.Activated += (s, e) =>
            {
                UpdateUIFromSettings();
            };
        }

        private void UpdateWindowTitle()
        {
            if (IDETitleBar != null)
            {
                IDETitleBar.Title = $"RoboBlocos: {ProjectSettings.ProjectName}";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveProjectAsync();
        }

        private async Task SaveProjectAsync()
        {
            try
            {
                // Atualiza as configura��es a partir da UI
                UpdateSettingsFromUI();

                // Salva no arquivo
                await ProjectService.SaveProjectAsync(ProjectSettings);

                // Mostra mensagem de sucesso
                await ShowMessageAsync("Sucesso", "Configura��es salvas com sucesso!");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Erro", $"Erro ao salvar configura��es: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualiza o arquivo com as configura��es do projeto a partir dos controles da UI
        /// </summary>
        private void UpdateSettingsFromUI()
        {
            // Atualiza a partir dos controles da UI
            // Configura��es do Rob�
            if (cmbModel?.SelectedItem is ComboBoxItem modelItem)
            {
                ProjectSettings.RobotSettings.Model = modelItem.Content?.ToString() ?? "RCX2";
            }

            ProjectSettings.RobotSettings.FirmwareOption = rbRecommended?.IsChecked == true 
                ? FirmwareOption.Recommended 
                : FirmwareOption.ChooseFile;

            if (txtCustomFirmware != null)
            {
                ProjectSettings.RobotSettings.CustomFirmwarePath = txtCustomFirmware.Text;
            }

            // Configura��es de Conex�o
            if (txtSerialPort != null)
            {
                ProjectSettings.ConnectionSettings.SerialPort = txtSerialPort.Text;
            }

            if (numConnectionAttempts != null)
            {
                ProjectSettings.ConnectionSettings.ConnectionAttempts = (int)numConnectionAttempts.Value;
            }

            // Configura��es de Log
            if (chkLogFirmware != null)
            {
                ProjectSettings.LoggingSettings.LogFirmwareDownload = chkLogFirmware.IsChecked == true;
            }

            if (chkLogErrors != null)
            {
                ProjectSettings.LoggingSettings.LogCompilationErrors = chkLogErrors.IsChecked == true;
            }
        }

        /// <summary>
        /// Atualiza os controles da UI a partir do arquivo com as configura��es do projeto
        /// </summary>
        private void UpdateUIFromSettings()
        {
            // Configura��es do Rob�
            if (cmbModel != null)
            {
                // Encontra e seleciona o modelo correspondente
                for (int i = 0; i < cmbModel.Items.Count; i++)
                {
                    if (cmbModel.Items[i] is ComboBoxItem item && 
                        item.Content?.ToString() == ProjectSettings.RobotSettings.Model)
                    {
                        cmbModel.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (rbRecommended != null && rbChooseFile != null)
            {
                rbRecommended.IsChecked = ProjectSettings.RobotSettings.FirmwareOption == FirmwareOption.Recommended;
                rbChooseFile.IsChecked = ProjectSettings.RobotSettings.FirmwareOption == FirmwareOption.ChooseFile;
            }

            if (txtCustomFirmware != null)
            {
                txtCustomFirmware.Text = ProjectSettings.RobotSettings.CustomFirmwarePath;
            }

            // Configura��es de Conex�o
            if (txtSerialPort != null)
            {
                txtSerialPort.Text = ProjectSettings.ConnectionSettings.SerialPort;
            }

            if (numConnectionAttempts != null)
            {
                numConnectionAttempts.Value = ProjectSettings.ConnectionSettings.ConnectionAttempts;
            }

            // Configura��es de Log
            if (chkLogFirmware != null)
            {
                chkLogFirmware.IsChecked = ProjectSettings.LoggingSettings.LogFirmwareDownload;
            }

            if (chkLogErrors != null)
            {
                chkLogErrors.IsChecked = ProjectSettings.LoggingSettings.LogCompilationErrors;
            }

            // Atualiza o t�tulo da janela
            UpdateWindowTitle();
        }

        /// <summary>
        /// Exibe uma mensagem ao usu�rio
        /// </summary>
        /// <param name="title">T�tulo da mensagem</param>
        /// <param name="message">Conte�do da mensagem</param>
        private async Task<ContentDialogResult> ShowMessageAsync(string title, string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
                Title = title,
                Content = message,
                PrimaryButtonText = "OK",
                DefaultButton = ContentDialogButton.Primary
            };

            return await dialog.ShowAsync();
        }
    }
}
