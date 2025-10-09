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

namespace RoboBlocos
{
    /// <summary>
    /// Janela principal do IDE para desenvolvimento de projetos RoboBlocos
    /// </summary>
    public sealed partial class IDE : Window
    {
        public ProjectSettings CurrentProject { get; private set; }

        public IDE() : this(new ProjectSettings())
        {
        }

        public IDE(ProjectSettings projectSettings)
        {
            InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 1024;
            presenter.PreferredMinimumHeight = 768;

            AppWindow.SetPresenter(presenter);

            CurrentProject = projectSettings;
            UpdateWindowTitle();

            // Inicializa o WebView2 para carregar conteúdo local e comunicação bidirecional
            _ = InitializeWebViewAsync();
        }

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

        private bool _blocklyReady = false;
        private bool _workspaceLoaded = false;

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
            // Se o projeto está salvo, voltar diretamente
            if (CurrentProject.State == ProjectState.Saved)
            {
                GoBackToMainWindow();
                return;
            }

            // Se o projeto é novo e nunca foi modificado, voltar sem salvar
            if (CurrentProject.State == ProjectState.New)
            {
                GoBackToMainWindow();
                return;
            }

            // Projeto tem modificações, perguntar se deseja salvar
            var result = await JanelaUtilities.ShowSaveBeforeExitDialogAsync(this);

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await SaveProjectInternalAsync();
                    GoBackToMainWindow();
                }
                catch (Exception ex)
                {
                    await JanelaUtilities.ShowErrorDialogAsync(this, "Erro ao salvar", $"Não foi possível salvar o projeto: {ex.Message}");
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                GoBackToMainWindow();
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
                // Primeiro, obter o XML do workspace atual
                var workspaceXml = await GetWorkspaceXmlFromJavaScriptAsync();

                // Salvar o workspace XML
                await ProjectService.SaveWorkspaceXmlAsync(CurrentProject, workspaceXml);

                // Salvar as configurações do projeto
                await ProjectService.SaveProjectAsync(CurrentProject);

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
            var mainWindow = new MainWindow();
            mainWindow.Activate();
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
                    // Remover aspas do JSON
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
                // Não mostrar erro ao usuário, apenas continuar com workspace vazio
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

                // O resultado vem como string JSON, precisamos deserializar
                if (!string.IsNullOrEmpty(result) && result != "null" && result != "\"\"")
                {
                    // Remover aspas do JSON
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
    }
}
