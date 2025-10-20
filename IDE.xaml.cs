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

                // Exibir o código gerado em um diálogo
                await ShowGeneratedCodeDialogAsync(generatedCode);
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao exportar", $"Não foi possível exportar o código: {ex.Message}");
            }
        }

        /// <summary>
        /// Exibe um diálogo com o código gerado
        /// </summary>
        /// <param name="code">Código JavaScript gerado</param>
        private async Task ShowGeneratedCodeDialogAsync(string code)
        {
            var scrollViewer = new ScrollViewer
            {
                MaxHeight = 400,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var textBlock = new TextBlock
            {
                Text = code,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 12,
                IsTextSelectionEnabled = true,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(10)
            };

            scrollViewer.Content = textBlock;

            var dialog = new ContentDialog
            {
                Title = "Código JavaScript Gerado",
                Content = scrollViewer,
                CloseButtonText = "Fechar",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
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

                // Adicionar log para mudança de nomenclatura
                AddLog($"Alteração de nomenclatura para {nomenclatura}.", LogSeverity.Informational, LogCategory.Interface);
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

            // Configurar timer para remover após 5 segundos
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
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
