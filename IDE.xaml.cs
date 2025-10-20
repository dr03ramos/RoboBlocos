using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RoboBlocos.Models;
using RoboBlocos.Services;
using System.Threading.Tasks;
using RoboBlocos.Utilities;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Windowing;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media;
using System.Linq;

namespace RoboBlocos
{
    /// <summary>
    /// Janela principal do IDE para desenvolvimento de projetos RoboBlocos
    /// </summary>
    public sealed partial class IDE : Window
    {
        public ProjectSettings CurrentProject { get; private set; }

        /// <summary>
        /// Indica se o Blockly está pronto para uso.
        /// </summary>
        private bool _blocklyReady = false;

        /// <summary>
        /// Indica se o workspace já foi carregado.
        /// </summary>
        private bool _workspaceLoaded = false;

        /// <summary>
        /// Indica se a nomenclatura foi inicializada (para evitar log na primeira seleção)
        /// </summary>
        private bool _nomenclatureInitialized = false;

        /// <summary>
        /// Inicializa uma nova instância da classe IDE com configurações específicas do projeto.
        /// </summary>
        /// <param name="projectSettings">As configurações do projeto a serem usadas.</param>
        public IDE(ProjectSettings projectSettings)
        {
            InitializeComponent();

            this.AppWindow.Closing += IDE_Closing;

            this.ExtendsContentIntoTitleBar = true;

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 1024;
            presenter.PreferredMinimumHeight = 768;

            AppWindow.SetPresenter(presenter);

            CurrentProject = projectSettings;
            UpdateWindowTitle();

            _ = InitializeWebViewAsync();
        }

        /// <summary>
        /// Inicializa o WebView2 para carregar o conteúdo do Blockly e configurar a comunicação entre o C# e o JavaScript
        /// </summary>
        private async Task InitializeWebViewAsync()
        {
            try
            {
                await MyWebView2.EnsureCoreWebView2Async();

                // Adicionar handler para mensagens do console JavaScript (útil para debug)
                MyWebView2.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                MyWebView2.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);

                string baseDir = AppContext.BaseDirectory;
                // Aponta para a pasta correta onde o conteúdo está sendo copiado (Blockly)
                string blocklyPath = Path.Combine(baseDir, "Blockly");

                // Define o mapeamento do host virtual para a pasta de conteúdo real
                if (Directory.Exists(blocklyPath))
                {
                    MyWebView2.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        hostName: "roboblocos.local",
                        folderPath: blocklyPath,
                        accessKind: CoreWebView2HostResourceAccessKind.Allow);

                    // Aguardar a navegação ser concluída antes de carregar o workspace
                    MyWebView2.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

                    // Navegar para a página local (raiz mapeada aponta para Blockly)
                    MyWebView2.CoreWebView2.Navigate("https://roboblocos.local/index.html");
                }
                else
                {
                    // Fallback: tentar navegar via file:// diretamente, caso o mapeamento falhe
                    string fallbackFile = Path.Combine(baseDir, "Blockly", "index.html");
                    if (File.Exists(fallbackFile))
                    {
                        MyWebView2.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                        MyWebView2.Source = new Uri(fallbackFile);
                    }
                    else
                    {
                        await JanelaUtilities.ShowErrorDialogAsync(this, "Conteúdo não encontrado", $"Arquivo não encontrado: {fallbackFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro WebView2", $"Falha ao inicializar o WebView2: {ex.Message}");
            }
        }

        /// <summary>
        /// Evento chamado quando a navegação do WebView2 é concluída
        /// </summary>
        private async void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"[OnNavigationCompleted] IsSuccess={args.IsSuccess}, WebErrorStatus={args.WebErrorStatus}");
            
            if (args.IsSuccess)
            {
                // Aguardar o Blockly enviar mensagem de pronto (com timeout de 2 segundos)
                var startTime = DateTime.Now;
                while (!_blocklyReady && (DateTime.Now - startTime).TotalMilliseconds < 2000)
                {
                    await Task.Delay(50);
                }

                // Se ainda não carregou (timeout ou outro motivo), tentar carregar agora
                if (!_workspaceLoaded)
                {
                    System.Diagnostics.Debug.WriteLine("[OnNavigationCompleted] Timeout ou Blockly não enviou mensagem, carregando workspace...");
                    _workspaceLoaded = true;
                    await LoadWorkspaceAsync();
                }
                
                // Remover o handler para não executar múltiplas vezes
                MyWebView2.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[OnNavigationCompleted] Navegação falhou: {args.WebErrorStatus}");
            }
        }

        /// <summary>
        /// Manipula mensagens recebidas do JavaScript via WebView2.
        /// </summary>
        /// <param name="sender">O objeto que enviou o evento.</param>
        /// <param name="e">Argumentos do evento de mensagem recebida.</param>
        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Usar TryGetWebMessageAsString para obter o JSON sem escape duplo
                var json = e.TryGetWebMessageAsString();
                System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Mensagem recebida: {json}");
                
                var message = JsonSerializer.Deserialize<BlocklyMessage>(json);
                System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Tipo da mensagem: {message?.Type}");

                if (message?.Type == "blocklyReady")
                {
                    System.Diagnostics.Debug.WriteLine("[OnWebMessageReceived] Blockly está pronto!");
                    _blocklyReady = true;
                    
                    // Carregar o workspace imediatamente
                    if (!_workspaceLoaded)
                    {
                        _workspaceLoaded = true;
                        await LoadWorkspaceAsync();
                    }
                }
                else if (message?.Type == "codeGenerated" || message?.Type == "workspaceChanged")
                {
                    System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Marcando projeto como modificado (tipo: {message?.Type})");
                    // Marcar projeto como modificado quando houver mudanças no Blockly
                    MarkAsModified();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Tipo de mensagem desconhecido ou nulo: {message?.Type}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Erro ao processar mensagem do JS: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OnWebMessageReceived] Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Marca o projeto como modificado
        /// </summary>
        private void MarkAsModified()
        {
            System.Diagnostics.Debug.WriteLine($"[MarkAsModified] Estado atual: {CurrentProject.State}");
            
            if (CurrentProject.State == ProjectState.Saved)
            {
                CurrentProject.State = ProjectState.Modified;
                System.Diagnostics.Debug.WriteLine("[MarkAsModified] Projeto marcado como Modified");
            }
            else if (CurrentProject.State == ProjectState.New)
            {
                CurrentProject.State = ProjectState.Modified;
                System.Diagnostics.Debug.WriteLine("[MarkAsModified] Projeto New marcado como Modified");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MarkAsModified] Projeto já está como: {CurrentProject.State}");
            }
        }

        /// <summary>
        /// Atualiza o título da janela com o nome do projeto atual
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (txtProgramName != null)
            {
                txtProgramName.Text = CurrentProject.ProjectName;
            }
        }

        /// <summary>
        /// Manipula a lógica de saída, solicitando salvar se modificado
        /// </summary>
        /// <returns>Verdadeiro se deve prosseguir com a saída, falso caso contrário</returns>
        private async Task<bool> HandleExitAsync()
        {
            if (CurrentProject.State == ProjectState.Saved)
            {
                return true;
            }
            if (CurrentProject.State == ProjectState.New)
            {
                return true;
            }
            var result = await JanelaUtilities.ShowSaveBeforeExitDialogAsync(this);
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await SaveProjectInternalAsync();
                    return true;
                }
                catch (Exception ex)
                {
                    await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o projeto: {ex.Message}");
                    return false;
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Cria uma nova instância do IDE com um projeto específico
        /// </summary>
        /// <param name="projectPath">Caminho para o arquivo de projeto</param>
        /// <returns>Nova instância do IDE</returns>
        public static async Task<IDE> CreateWithProjectAsync(string projectPath = null)
        {
            ProjectSettings settings;

            if (!string.IsNullOrEmpty(projectPath) && ProjectService.ProjectExists(projectPath))
            {
                try
                {
                    settings = await ProjectService.LoadProjectAsync(projectPath);
                }
                catch
                {
                    // Se o carregamento falhar, cria um novo projeto
                    settings = new ProjectSettings();
                }
            }
            else
            {
                settings = new ProjectSettings();
            }

            return new IDE(settings);
        }

        /// <summary>
        /// Manipula o evento de voltar da barra de título, perguntando se o usuário deseja salvar
        /// </summary>
        private async void NavViewTitleBar_BackRequested(TitleBar sender, object args)
        {
            if (await HandleExitAsync())
            {
                GoBackToMainWindow();
            }
        }

        /// <summary>
        /// Manipula o evento de fechamento da janela (ex.: via botão X)
        /// </summary>
        private async void IDE_Closing(AppWindow sender, AppWindowClosingEventArgs e)
        {
            e.Cancel = true;
            if (await HandleExitAsync())
            {
                // Prosseguir com o fechamento e abrir MainWindow
                var bounds = TamanhoJanelaUtilities.CaptureWindowBounds(this);
                var mainWindow = new MainWindow();
                TamanhoJanelaUtilities.ApplyWindowBounds(mainWindow, bounds);
                this.Close();
            }
        }

        /// <summary>
        /// Clique no botão Salvar
        /// </summary>
        private async void SaveProgramButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SaveProjectInternalAsync();
                await JanelaUtilities.ShowInfoDialogAsync(this, "Projeto salvo", "As alterações foram salvas com sucesso.");
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o projeto: {ex.Message}");
            }
        }

        /// <summary>
        /// Salva o projeto e atualiza seu estado para Saved
        /// </summary>
        private async Task SaveProjectInternalAsync()
        {
            try
            {
                await ProjectService.SaveProjectAsync(CurrentProject);

                var workspaceXml = await GetWorkspaceXmlFromJavaScriptAsync();
                await ProjectService.SaveWorkspaceXmlAsync(CurrentProject, workspaceXml);

                CurrentProject.State = ProjectState.Saved;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Falha ao salvar o projeto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clique no botão Renomear
        /// </summary>
        private async void RenameProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var newName = await JanelaUtilities.ShowRenameDialogAsync(this, CurrentProject.ProjectName);

            // Se cancelado, não mostrar mensagem
            if (newName is null)
                return;

            // Validação adicional por segurança
            if (string.IsNullOrWhiteSpace(newName))
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Nome inválido", "Informe um nome válido.");
                return;
            }

            var updated = await ProjectService.RenameProjectAsync(CurrentProject, newName);
            if (updated == null)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Falha ao renomear", "Não foi possível renomear o projeto.");
                return;
            }

            CurrentProject = updated;
            UpdateWindowTitle();
            
            // Renomear marca o projeto como modificado (mudança foi salva pelo RenameProjectAsync)
            CurrentProject.State = ProjectState.Saved;
        }

        /// <summary>
        /// Volta para a janela principal fechando a janela atual
        /// </summary>
        private void GoBackToMainWindow()
        {
            // Capturar bounds da janela IDE atual antes de fechar
            var bounds = TamanhoJanelaUtilities.CaptureWindowBounds(this);

            var mainWindow = new MainWindow();
            
            // Aplicar os bounds capturados à MainWindow
            TamanhoJanelaUtilities.ApplyWindowBounds(mainWindow, bounds);
            
            this.Close();
        }

        /// <summary>
        /// Manipula o clique no botão de configurações do robô
        /// </summary>
        private async void RobotSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var updatedSettings = await JanelaUtilities.ShowSettingsDialogAsync(this, CurrentProject);
            
            // Sempre atualizar CurrentProject - será as configurações modificadas (OK) ou as originais (Cancelar)
            CurrentProject = updatedSettings;
            
            System.Diagnostics.Debug.WriteLine("Diálogo de configurações fechado");
        }

        /// <summary>
        /// Manipula o clique no botão de exportar código
        /// </summary>
        private async void ExportCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obter o código JavaScript gerado do Blockly
                var generatedCode = await GetGeneratedCodeFromJavaScriptAsync();

                if (string.IsNullOrWhiteSpace(generatedCode))
                {
                    await JanelaUtilities.ShowInfoDialogAsync(this, "Código vazio", "Não há blocos para gerar código. Adicione alguns blocos ao seu programa primeiro.");
                    return;
                }

                // Adicionar log de sucesso
                AddLog("Código gerado com sucesso", LogSeverity.Success, LogCategory.Obrigatórios);

                // Exibir o código gerado em um diálogo
                await ShowGeneratedCodeDialogAsync(generatedCode);
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao exportar", $"Não foi possível exportar o código: {ex.Message}");
            }
        }

        /// <summary>
        /// Manipula o clique no botão de enviar para o robô
        /// </summary>
        private async void SendToRobotButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Log 1: Tentando conectar ao robô
                string robotModel = CurrentProject.RobotSettings.Model;
                string serialPort = CurrentProject.ConnectionSettings.SerialPort;
                AddLog($"Tentando conectar ao robô... {robotModel} na porta {serialPort}...", LogSeverity.Informational, LogCategory.Obrigatórios);

                // Simular tentativa de conexão (aguardar um momento)
                await Task.Delay(1000);

                // Log 2: Não foi possível conectar
                AddLog("Não foi possível conectar ao robô. Tente colocá-lo mais perto do sensor.", LogSeverity.Error, LogCategory.Obrigatórios);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao enviar para o robô: {ex.Message}");
            }
        }

        /// <summary>
        /// Exibe um diálogo com o código gerado
        /// </summary>
        /// <param name="code">Código JavaScript gerado</param>
        private async Task ShowGeneratedCodeDialogAsync(string code)
        {
            // Container principal
            var mainGrid = new Grid
            {
                Width = 800,
                Height = 600,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };
            
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Título e descrição
            var headerPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            
            var titleText = new TextBlock
            {
                Text = "Código JavaScript Gerado",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            
            var descriptionText = new TextBlock
            {
                Text = "Este é o código gerado a partir dos seus blocos. Você pode copiar ou salvar este código.",
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                TextWrapping = TextWrapping.Wrap
            };
            
            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(descriptionText);
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Área do editor de código
            var codeEditorBorder = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 30, 30, 30)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 60, 60, 60)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var codeEditorGrid = new Grid();
            codeEditorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            codeEditorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Painel de numeração de linhas
            var lineNumbersPanel = new StackPanel
            {
                Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 20, 20, 20)),
                Padding = new Thickness(12, 12, 8, 12),
                VerticalAlignment = VerticalAlignment.Top
            };

            // Painel do código
            var codeScrollViewer = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };

            var codeTextBlock = new TextBlock
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.NoWrap,
                Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 212, 212, 212))
            };

            // Dividir o código em linhas e adicionar numeração
            var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var maxLineNumberWidth = lines.Length.ToString().Length;

            // Adicionar números de linha
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
                lineNumbersPanel.Children.Add(lineNumber);
            }

            codeTextBlock.Text = code;
            codeScrollViewer.Content = codeTextBlock;

            // Sincronizar scroll entre números de linha e código
            codeScrollViewer.ViewChanged += (s, e) =>
            {
                lineNumbersPanel.Margin = new Thickness(0, -codeScrollViewer.VerticalOffset, 0, 0);
            };

            Grid.SetColumn(lineNumbersPanel, 0);
            Grid.SetColumn(codeScrollViewer, 1);
            codeEditorGrid.Children.Add(lineNumbersPanel);
            codeEditorGrid.Children.Add(codeScrollViewer);

            codeEditorBorder.Child = codeEditorGrid;
            Grid.SetRow(codeEditorBorder, 1);
            mainGrid.Children.Add(codeEditorBorder);

            // Painel de informações
            var infoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 0, 0, 8)
            };
            
            var infoIcon = new FontIcon
            {
                Glyph = "\uE946",
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
            
            var infoText = new TextBlock
            {
                Text = $"{lines.Length} linhas | Selecione o texto para copiar",
                FontSize = 12,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
            
            infoPanel.Children.Add(infoIcon);
            infoPanel.Children.Add(infoText);
            Grid.SetRow(infoPanel, 2);
            mainGrid.Children.Add(infoPanel);

            // Criar o diálogo
            var dialog = new ContentDialog
            {
                Title = null, // Removemos o título padrão pois temos um customizado
                Content = mainGrid,
                PrimaryButtonText = "Salvar Arquivo",
                SecondaryButtonText = "Copiar Tudo",
                CloseButtonText = "Fechar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            // Handler para o botão Salvar
            dialog.PrimaryButtonClick += async (s, e) =>
            {
                e.Cancel = true; // Prevenir fechamento automático
                await SaveCodeToFileAsync(code);
            };

            // Handler para o botão Copiar
            dialog.SecondaryButtonClick += (s, e) =>
            {
                e.Cancel = true; // Prevenir fechamento automático
                CopyCodeToClipboard(code);
                
                // Feedback visual
                infoText.Text = "✓ Código copiado para a área de transferência!";
                infoText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.LightGreen);
                
                // Restaurar texto após 2 segundos
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (sender, args) =>
                {
                    infoText.Text = $"{lines.Length} linhas | Selecione o texto para copiar";
                    infoText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                    timer.Stop();
                };
                timer.Start();
            };

            await dialog.ShowAsync();
        }

        /// <summary>
        /// Salva o código gerado em um arquivo
        /// </summary>
        /// <param name="code">Código a ser salvo</param>
        private async Task SaveCodeToFileAsync(string code)
        {
            try
            {
                var savePicker = new FileSavePicker();
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Arquivo JavaScript", new[] { ".js" });
                savePicker.FileTypeChoices.Add("Arquivo de Texto", new[] { ".txt" });
                savePicker.SuggestedFileName = $"{CurrentProject.ProjectName}_codigo";

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    await FileIO.WriteTextAsync(file, code);
                    
                    AddLog($"Código salvo com sucesso em: {file.Path}", LogSeverity.Success, LogCategory.Interface);
                    await JanelaUtilities.ShowInfoDialogAsync(this, "Arquivo salvo", $"O código foi salvo com sucesso em:\n{file.Path}");
                }
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o arquivo: {ex.Message}");
            }
        }

        /// <summary>
        /// Copia o código para a área de transferência
        /// </summary>
        /// <param name="code">Código a ser copiado</param>
        private void CopyCodeToClipboard(string code)
        {
            try
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(code);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
                
                AddLog("Código copiado para a área de transferência", LogSeverity.Success, LogCategory.Interface);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao copiar para área de transferência: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém o código JavaScript gerado do Blockly
        /// </summary>
        /// <returns>Código JavaScript ou string vazia em caso de erro</returns>
        private async Task<string> GetGeneratedCodeFromJavaScriptAsync()
        {
            try
            {
                // Executar JavaScript para obter o código gerado
                var result = await MyWebView2.CoreWebView2.ExecuteScriptAsync(
                    @"(function() {
                        try {
                            if (typeof getGeneratedCode === 'function') {
                                return getGeneratedCode();
                            } else if (typeof generateCode === 'function') {
                                return generateCode();
                            } else if (typeof workspace !== 'undefined' && workspace) {
                                javascript.javascriptGenerator.addReservedWords('code');
                                return javascript.javascriptGenerator.workspaceToCode(workspace);
                            }
                            return '';
                        } catch (e) {
                            return '';
                        }
                    })()");

                // O resultado vem como string JSON, precisamos deserializar
                if (!string.IsNullOrEmpty(result) && result != "null" && result != "\"\"")
                {
                    return JsonSerializer.Deserialize<string>(result) ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter código gerado: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Carrega o workspace do Blockly a partir do arquivo XML salvo
        /// </summary>
        private async Task LoadWorkspaceAsync()
        {
            try
            {
                var xml = await ProjectService.LoadWorkspaceXmlAsync(CurrentProject);

                System.Diagnostics.Debug.WriteLine($"[LoadWorkspace] XML carregado: {xml?.Length ?? 0} caracteres");

                if (!string.IsNullOrEmpty(xml))
                {
                    // Escapar o XML para uso em JavaScript
                    var escapedXml = JsonSerializer.Serialize(xml);

                    // Executar JavaScript para carregar o workspace
                    var result = await MyWebView2.CoreWebView2.ExecuteScriptAsync(
                        $@"(function() {{
                            try {{
                                if (typeof loadWorkspaceFromXml === 'function' && typeof workspace !== 'undefined') {{
                                    loadWorkspaceFromXml({escapedXml});
                                    return 'success';
                                }} else {{
                                    return 'not ready';
                                }}
                            }} catch (e) {{
                                return 'error: ' + e.message;
                            }}
                        }})()");

                    System.Diagnostics.Debug.WriteLine($"[LoadWorkspace] Resultado: {result}");

                    if (result.Contains("success"))
                    {
                        System.Diagnostics.Debug.WriteLine("[LoadWorkspace] Workspace carregado com sucesso!");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadWorkspace] Falha ao carregar: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadWorkspace] Erro ao carregar workspace: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém o XML do workspace atual do JavaScript
        /// </summary>
        /// <returns>XML do workspace ou string vazia em caso de erro</returns>
        private async Task<string> GetWorkspaceXmlFromJavaScriptAsync()
        {
            try
            {
                // Executar JavaScript para obter o XML do workspace
                var result = await MyWebView2.CoreWebView2.ExecuteScriptAsync(
                    @"(function() {
                        try {
                            if (typeof workspace !== 'undefined' && workspace) {
                                var xml = Blockly.Xml.workspaceToDom(workspace);
                                var xmlText = Blockly.Xml.domToText(xml);
                                return xmlText;
                            }
                            return '';
                        } catch (e) {
                            return '';
                        }
                    })()");

                // O resultado vem como string JSON, então deserializa
                if (!string.IsNullOrEmpty(result) && result != "null" && result != "\"\"")
                {
                    return JsonSerializer.Deserialize<string>(result) ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao obter XML do workspace: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Classe auxiliar para deserializar mensagens do JavaScript
        /// </summary>
        private class BlocklyMessage
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("xml")]
            public string? Xml { get; set; }

            [JsonPropertyName("code")]
            public string? Code { get; set; }
        }

        /// <summary>
        /// Manipula a mudança de seleção no SelectorBar de nomenclatura
        /// </summary>
        private void NomenclatureSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            var selectedItem = sender.SelectedItem as SelectorBarItem;
            if (selectedItem != null)
            {
                string nomenclatura = selectedItem.Text;
                System.Diagnostics.Debug.WriteLine($"[NomenclatureSelectorBar] Nomenclatura selecionada: {nomenclatura}");

                // Só adicionar log após a inicialização (não na primeira seleção)
                if (_nomenclatureInitialized)
                {
                    AddLog($"Alteração de nomenclatura para {nomenclatura}.", LogSeverity.Informational, LogCategory.Interface);
                }
                else
                {
                    _nomenclatureInitialized = true;
                }
            }
        }

        /// <summary>
        /// Adiciona um registro de log à tela, se habilitado nas configurações
        /// </summary>
        private void AddLog(string message, LogSeverity severity, LogCategory category)
        {
            // Verificar se o log deve ser exibido baseado nas configurações
            bool shouldLog = category switch
            {
                LogCategory.FirmwareDownload => CurrentProject.LoggingSettings.LogFirmwareDownload,
                LogCategory.Interface => CurrentProject.LoggingSettings.LogInterface,
                LogCategory.Obrigatórios => CurrentProject.LoggingSettings.LogMandatory,
                _ => false
            };

            if (!shouldLog)
                return;

            // Criar InfoBar
            var infoBar = new InfoBar
            {
                IsOpen = true,
                IsIconVisible = true,
                IsClosable = false,
                Severity = severity switch
                {
                    LogSeverity.Informational => InfoBarSeverity.Informational,
                    LogSeverity.Success => InfoBarSeverity.Success,
                    LogSeverity.Error => InfoBarSeverity.Error,
                    _ => InfoBarSeverity.Informational
                },
                Message = message
            };

            // Adicionar ao StackPanel
            LogStackPanel.Children.Add(infoBar);

            // Configurar timer para remover após 8 segundos
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
            timer.Tick += (s, e) =>
            {
                var storyboard = new Storyboard();
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                Storyboard.SetTarget(fadeOut, infoBar);
                Storyboard.SetTargetProperty(fadeOut, "Opacity");
                storyboard.Children.Add(fadeOut);
                storyboard.Completed += (sender, args) =>
                {
                    LogStackPanel.Children.Remove(infoBar);
                    timer.Stop();
                };
                storyboard.Begin();
            };
            timer.Start();
        }

        /// <summary>
        /// Adiciona um registro de log à tela, se habilitado nas configurações
        /// </summary>
        private void AddLog(string message, string substring, LogSeverity severity, LogCategory category)
        {
            AddLog(message + new string(' ', 80) + substring, severity, category);
        }
    }
}
