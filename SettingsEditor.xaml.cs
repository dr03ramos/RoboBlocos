using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using RoboBlocos.Models;
using RoboBlocos.Services;
using System;
using System.Threading.Tasks;
using RoboBlocos.Utilities;

namespace RoboBlocos
{
    /// <summary>
    /// Janela de edição de configurações do projeto
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

            // Inicializa as configurações do projeto
            ProjectSettings = projectSettings;

            // Atualiza o título da janela
            UpdateWindowTitle();

            // Atualiza a UI a partir das configurações
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

        /// <summary>
        /// Manipula o clique no botão OK - atualiza as configurações e fecha a janela
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Atualiza as configurações do projeto a partir da UI
            UpdateSettingsFromUI();
            
            // Marca o projeto como modificado se estava salvo
            if (ProjectSettings.State == ProjectState.Saved)
            {
                ProjectSettings.State = ProjectState.Modified;
            }
            
            // Fecha a janela
            this.Close();
        }

        /// <summary>
        /// Atualiza o arquivo com as configurações do projeto a partir dos controles da UI
        /// </summary>
        private void UpdateSettingsFromUI()
        {
            // Atualiza a partir dos controles da UI
            // Configurações do Robô
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

            // Configurações de Conexão
            if (txtSerialPort != null)
            {
                ProjectSettings.ConnectionSettings.SerialPort = txtSerialPort.Text;
            }

            if (numConnectionAttempts != null)
            {
                ProjectSettings.ConnectionSettings.ConnectionAttempts = (int)numConnectionAttempts.Value;
            }

            // Configurações de Log
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
        /// Atualiza os controles da UI a partir do arquivo com as configurações do projeto
        /// </summary>
        private void UpdateUIFromSettings()
        {
            // Configurações do Robô
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

            // Configurações de Conexão
            if (txtSerialPort != null)
            {
                txtSerialPort.Text = ProjectSettings.ConnectionSettings.SerialPort;
            }

            if (numConnectionAttempts != null)
            {
                numConnectionAttempts.Value = ProjectSettings.ConnectionSettings.ConnectionAttempts;
            }

            // Configurações de Log
            if (chkLogFirmware != null)
            {
                chkLogFirmware.IsChecked = ProjectSettings.LoggingSettings.LogFirmwareDownload;
            }

            if (chkLogErrors != null)
            {
                chkLogErrors.IsChecked = ProjectSettings.LoggingSettings.LogCompilationErrors;
            }

            // Atualiza o título da janela
            UpdateWindowTitle();
        }
    }
}
