using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using RoboBlocos.Models;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RoboBlocos
{
    /// <summary>
    /// Dialog para exportar código gerado
    /// </summary>
    public sealed partial class CodeExporter : ContentDialog
    {
        private readonly string _code;
        private readonly ProjectSettings _projectSettings;
        private readonly Window _parentWindow;
        private DispatcherTimer _feedbackTimer;

        public CodeExporter(Window parentWindow, ProjectSettings projectSettings, string code)
        {
            InitializeComponent();

            _parentWindow = parentWindow;
            _projectSettings = projectSettings;
            _code = code;

            // Configurar manipuladores de eventos
            this.PrimaryButtonClick += CodeExporter_PrimaryButtonClick;
            this.SecondaryButtonClick += CodeExporter_SecondaryButtonClick;
            this.Loaded += CodeExporter_Loaded;
        }

        private void CodeExporter_Loaded(object sender, RoutedEventArgs e)
        {
            // Configurar o código no editor
            CodeTextBlock.Text = _code;

            // Adicionar números de linha
            var lines = _code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var maxLineNumberWidth = lines.Length.ToString().Length;

            for (int i = 1; i <= lines.Length; i++)
            {
                var lineNumber = new TextBlock
                {
                    Text = i.ToString().PadLeft(maxLineNumberWidth),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 133, 133, 133)),
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                LineNumbersPanel.Children.Add(lineNumber);
            }

            // Configurar texto de informação
            InfoTextBlock.Text = $"{lines.Length} linhas | Selecione o texto para copiar";
        }

        private void CodeScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            // Sincronizar scroll dos números de linha com o código
            LineNumbersPanel.Margin = new Thickness(0, -CodeScrollViewer.VerticalOffset, 0, 0);
        }

        private async void CodeExporter_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Prevenir fechamento automático
            args.Cancel = true;

            try
            {
                var savePicker = new FileSavePicker();
                var hwnd = WindowNative.GetWindowHandle(_parentWindow);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Arquivo NQC", new[] { ".nqc" });
                savePicker.FileTypeChoices.Add("Arquivo de Texto", new[] { ".txt" });
                savePicker.SuggestedFileName = $"{_projectSettings.ProjectName}_codigo";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, _code);
                    ShowFeedback($"✓ Arquivo salvo em: {file.Name}", true);
                }
            }
            catch (Exception ex)
            {
                ShowFeedback($"✗ Erro ao salvar: {ex.Message}", false);
            }
        }

        private void CodeExporter_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Prevenir fechamento automático
            args.Cancel = true;

            try
            {
                var dataPackage = new DataPackage();
                dataPackage.SetText(_code);
                Clipboard.SetContent(dataPackage);
                ShowFeedback("✓ Código copiado para a área de transferência!", true);
            }
            catch (Exception ex)
            {
                ShowFeedback($"✗ Erro ao copiar: {ex.Message}", false);
            }
        }

        private void ShowFeedback(string message, bool isSuccess)
        {
            // Atualizar texto de feedback
            InfoTextBlock.Text = message;
            InfoTextBlock.Foreground = new SolidColorBrush(
                isSuccess 
                    ? Microsoft.UI.Colors.LightGreen 
                    : Microsoft.UI.Colors.OrangeRed
            );

            // Cancelar timer anterior se existir
            if (_feedbackTimer != null)
            {
                _feedbackTimer.Stop();
                _feedbackTimer = null;
            }

            // Criar novo timer para restaurar texto após 2 segundos
            _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _feedbackTimer.Tick += (s, e) =>
            {
                var lines = _code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                InfoTextBlock.Text = $"{lines.Length} linhas | Selecione o texto para copiar";
                InfoTextBlock.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                _feedbackTimer.Stop();
            };
            _feedbackTimer.Start();
        }
    }
}
