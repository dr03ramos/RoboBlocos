using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Models;
using System;

namespace RoboBlocos
{
    /// <summary>
    /// Dialog de edição de configurações do projeto
    /// </summary>
    public sealed partial class SettingsEditor : ContentDialog
    {
        public ProjectSettings ProjectSettings { get; private set; }

        public SettingsEditor() : this(new ProjectSettings())
        {
        }

        public SettingsEditor(ProjectSettings projectSettings)
        {
            InitializeComponent();

            // Inicializa as configurações do projeto
            ProjectSettings = projectSettings;

            // Configura os manipuladores de eventos para os botões
            this.PrimaryButtonClick += SettingsEditor_PrimaryButtonClick;
            this.SecondaryButtonClick += SettingsEditor_SecondaryButtonClick;

            // Atualiza a UI a partir das configurações quando carregado
            this.Loaded += (s, e) => UpdateUIFromSettings();
        }

        private void SettingsEditor_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Atualiza as configurações do projeto a partir da UI
            UpdateSettingsFromUI();
            
            // Marca o projeto como modificado se estava salvo
            if (ProjectSettings.State == ProjectState.Saved)
            {
                ProjectSettings.State = ProjectState.Modified;
            }
            
            // Dialog fecha automaticamente
        }

        private void SettingsEditor_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Cancelar - não há mudanças necessárias
            // Dialog fecha automaticamente
        }

        /// <summary>
        /// Atualiza o arquivo com as configurações do projeto a partir dos controles da UI
        /// </summary>
        private void UpdateSettingsFromUI()
        {
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
        }
    }
}
