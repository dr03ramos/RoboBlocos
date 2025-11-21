using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using RoboBlocos.Models;
using RoboBlocos.Services;
using RoboBlocos.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Windowing;

namespace RoboBlocos
{
    /// <summary>
    /// Janela para visualizar e editar tutoriais
    /// </summary>
    public sealed partial class TutorialsWindow : Window
    {
        private List<Tutorial> _allTutorials = new();
        private Tutorial? _currentTutorial;
        private bool _isEditModeEnabled = false;
        private const string EDIT_PASSWORD = "admin123"; // Senha para modo de edição
        private ObservableCollection<TutorialStepViewModel> _editingSteps = new();

        public TutorialsWindow()
        {
            InitializeComponent();

            // Configurar janela
            this.ExtendsContentIntoTitleBar = true;
            this.Title = "Tutoriais do RoboBlocos";

            OverlappedPresenter presenter = OverlappedPresenter.Create();
            presenter.PreferredMinimumWidth = 900;
            presenter.PreferredMinimumHeight = 600;
            AppWindow.SetPresenter(presenter);

            // Aplicar tema
            ThemeUtilities.ApplyTheme(this);

            // Carregar tutoriais
            _ = LoadTutorialsAsync();
        }

        /// <summary>
        /// Manipula o evento de voltar da TitleBar
        /// </summary>
        private void TitleBar_BackRequested(TitleBar sender, object args)
        {
            this.Close();
        }

        /// <summary>
        /// Carrega todos os tutoriais
        /// </summary>
        private async Task LoadTutorialsAsync()
        {
            try
            {
                _allTutorials = await TutorialService.LoadTutorialsAsync();
                UpdateTutorialsList();
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro", $"Não foi possível carregar os tutoriais: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualiza a lista de tutoriais exibida
        /// </summary>
        private void UpdateTutorialsList()
        {
            var filtered = FilterTutorials(_allTutorials);
            var viewModels = filtered.Select(t => new TutorialViewModel(t, _isEditModeEnabled)).ToList();
            TutorialsList.ItemsSource = viewModels;
        }

        /// <summary>
        /// Atualiza a visibilidade dos botões de edição/exclusão
        /// </summary>
        private void UpdateEditButtonsVisibility()
        {
            // Atualizar a lista para refletir o novo estado
            UpdateTutorialsList();

            // Atualizar visibilidade do botão "Novo Tutorial"
            NewTutorialButton.Visibility = _isEditModeEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Filtra tutoriais com base no critério de busca
        /// </summary>
        private List<Tutorial> FilterTutorials(List<Tutorial> tutorials)
        {
            var filtered = tutorials.AsEnumerable();

            // Filtro de busca
            var searchText = SearchBox?.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(t =>
                    t.Title.ToLower().Contains(searchText) ||
                    t.Description.ToLower().Contains(searchText));
            }

            return filtered.ToList();
        }

        /// <summary>
        /// Botão de modo de edição
        /// </summary>
        private async void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditModeEnabled)
            {
                // Solicitar senha
                var passwordBox = new PasswordBox { Width = 300, PlaceholderText = "Digite a senha" };
                var dialog = new ContentDialog
                {
                    Title = "Modo de Edição",
                    Content = passwordBox,
                    PrimaryButtonText = "Entrar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };
                ThemeUtilities.ApplyThemeToDialog(dialog);

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (passwordBox.Password == EDIT_PASSWORD)
                    {
                        _isEditModeEnabled = true;
                        EditModeButton.Content = new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            Children =
                            {
                                new FontIcon { Glyph = "\uE8D8", FontSize = 16 },
                                new TextBlock { Text = "Sair do Modo de Edição" }
                            }
                        };
                        UpdateEditButtonsVisibility();
                    }
                    else
                    {
                        await JanelaUtilities.ShowErrorDialogAsync(this, "Senha Incorreta", "A senha digitada está incorreta.");
                    }
                }
            }
            else
            {
                // Desabilitar modo de edição
                _isEditModeEnabled = false;
                EditModeButton.Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FontIcon { Glyph = "\uE72E", FontSize = 16 },
                        new TextBlock { Text = "Modo de Edição" }
                    }
                };
                UpdateEditButtonsVisibility();

                // Voltar ao estado vazio se estava editando
                if (EditorScrollViewer.Visibility == Visibility.Visible)
                {
                    ShowEmptyState();
                }
            }
        }

        /// <summary>
        /// Botão de novo tutorial
        /// </summary>
        private void NewTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditModeEnabled) return;

            _currentTutorial = new Tutorial
            {
                Title = "Novo Tutorial",
                Description = "Descrição do tutorial",
                Steps = new List<TutorialStep>()
            };

            ShowEditor();
        }

        /// <summary>
        /// Mudança no texto da busca
        /// </summary>
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            UpdateTutorialsList();
        }

        /// <summary>
        /// Botão de ver tutorial
        /// </summary>
        private void ViewTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TutorialViewModel viewModel)
            {
                var tutorial = _allTutorials.FirstOrDefault(t => t.Id == viewModel.Id);
                if (tutorial != null)
                {
                    _currentTutorial = tutorial;
                    ShowViewer();
                }
            }
        }

        /// <summary>
        /// Botão de editar tutorial
        /// </summary>
        private void EditTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditModeEnabled) return;

            if (sender is Button button && button.Tag is TutorialViewModel viewModel)
            {
                var tutorial = _allTutorials.FirstOrDefault(t => t.Id == viewModel.Id);
                if (tutorial != null)
                {
                    _currentTutorial = tutorial;
                    ShowEditor();
                }
            }
        }

        /// <summary>
        /// Botão de excluir tutorial
        /// </summary>
        private async void DeleteTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditModeEnabled) return;

            if (sender is Button button && button.Tag is TutorialViewModel viewModel)
            {
                var tutorial = _allTutorials.FirstOrDefault(t => t.Id == viewModel.Id);
                if (tutorial != null)
                {
                    var confirm = await JanelaUtilities.ShowConfirmationDialogAsync(
                        this,
                        "Excluir Tutorial",
                        $"Tem certeza que deseja excluir o tutorial '{tutorial.Title}'? Esta ação não pode ser desfeita.",
                        "Excluir",
                        "Cancelar");

                    if (confirm)
                    {
                        try
                        {
                            await TutorialService.DeleteTutorialAsync(tutorial.Id);
                            await LoadTutorialsAsync();
                            
                            // Se estava visualizando/editando este tutorial, voltar ao estado vazio
                            if (_currentTutorial?.Id == tutorial.Id)
                            {
                                ShowEmptyState();
                            }
                        }
                        catch (Exception ex)
                        {
                            await JanelaUtilities.ShowErrorDialogAsync(this, "Erro", $"Não foi possível excluir o tutorial: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Exibe o visualizador de tutorial com todos os passos em sequência
        /// </summary>
        private void ShowViewer()
        {
            if (_currentTutorial == null) return;

            EmptyStatePanel.Visibility = Visibility.Collapsed;
            EditorScrollViewer.Visibility = Visibility.Collapsed;
            ViewerScrollViewer.Visibility = Visibility.Visible;

            // Configurar cabeçalho
            ViewerTitle.Text = _currentTutorial.Title;
            ViewerDescription.Text = _currentTutorial.Description;
            ViewerStepsCount.Text = $"{_currentTutorial.Steps.Count} passos";

            // Limpar container de passos
            AllStepsContainer.Children.Clear();

            // Adicionar todos os passos em sequência
            foreach (var step in _currentTutorial.Steps.OrderBy(s => s.Order))
            {
                var stepPanel = CreateStepPanel(step);
                AllStepsContainer.Children.Add(stepPanel);
            }
        }

        /// <summary>
        /// Cria um painel visual para um passo do tutorial
        /// </summary>
        private StackPanel CreateStepPanel(TutorialStep step)
        {
            var panel = new StackPanel { Spacing = 16 };

            // Título do passo
            var title = new TextBlock
            {
                Text = $"{step.Order}. {step.Title}",
                Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(title);

            // Conteúdo do passo
            var content = new TextBlock
            {
                Text = step.Content,
                Style = (Style)Application.Current.Resources["BodyTextBlockStyle"],
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.9
            };
            panel.Children.Add(content);

            // Imagem (se houver)
            if (!string.IsNullOrEmpty(step.ImagePath))
            {
                try
                {
                    var fullPath = TutorialService.GetFullImagePath(_currentTutorial!.Id, step.ImagePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(fullPath)),
                            Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                            MaxHeight = 400
                        };
                        panel.Children.Add(image);
                    }
                }
                catch
                {
                    // Ignorar erros ao carregar imagem
                }
            }

            // Código (se houver)
            if (!string.IsNullOrEmpty(step.CodeSnippet))
            {
                var codeContainer = new Border
                {
                    Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"],
                    BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16)
                };

                var codeScrollViewer = new ScrollViewer
                {
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 300
                };

                var codeBlock = new TextBlock
                {
                    Text = step.CodeSnippet,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 13,
                    TextWrapping = TextWrapping.NoWrap
                };

                codeScrollViewer.Content = codeBlock;
                codeContainer.Child = codeScrollViewer;
                panel.Children.Add(codeContainer);
            }

            // Adicionar separador visual entre passos
            var separator = new Border
            {
                Height = 1,
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                Opacity = 0.3,
                Margin = new Thickness(0, 16, 0, 0)
            };
            panel.Children.Add(separator);

            return panel;
        }

        /// <summary>
        /// Exibe o editor de tutorial
        /// </summary>
        private void ShowEditor()
        {
            if (_currentTutorial == null || !_isEditModeEnabled) return;

            EmptyStatePanel.Visibility = Visibility.Collapsed;
            ViewerScrollViewer.Visibility = Visibility.Collapsed;
            EditorScrollViewer.Visibility = Visibility.Visible;

            // Preencher campos
            EditTitle.Text = _currentTutorial.Title;
            EditDescription.Text = _currentTutorial.Description;

            // Preencher passos
            _editingSteps.Clear();
            for (int i = 0; i < _currentTutorial.Steps.Count; i++)
            {
                var step = _currentTutorial.Steps[i];
                _editingSteps.Add(new TutorialStepViewModel
                {
                    Order = step.Order,
                    Title = step.Title,
                    Content = step.Content,
                    ImagePath = step.ImagePath,
                    CodeSnippet = step.CodeSnippet
                });
            }

            StepsList.ItemsSource = _editingSteps;
        }

        /// <summary>
        /// Exibe o estado vazio
        /// </summary>
        private void ShowEmptyState()
        {
            _currentTutorial = null;
            EmptyStatePanel.Visibility = Visibility.Visible;
            ViewerScrollViewer.Visibility = Visibility.Collapsed;
            EditorScrollViewer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Botão de salvar tutorial
        /// </summary>
        private async void SaveTutorialButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTutorial == null) return;

            // Validação
            if (string.IsNullOrWhiteSpace(EditTitle.Text))
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro", "O título não pode estar vazio.");
                return;
            }

            try
            {
                // Atualizar metadados
                _currentTutorial.Title = EditTitle.Text.Trim();
                _currentTutorial.Description = EditDescription.Text.Trim();

                // Atualizar passos
                _currentTutorial.Steps.Clear();
                for (int i = 0; i < _editingSteps.Count; i++)
                {
                    var stepVm = _editingSteps[i];
                    _currentTutorial.Steps.Add(new TutorialStep
                    {
                        Order = i + 1,
                        Title = stepVm.Title,
                        Content = stepVm.Content,
                        ImagePath = stepVm.ImagePath,
                        CodeSnippet = stepVm.CodeSnippet
                    });
                }

                // Salvar
                await TutorialService.SaveTutorialAsync(_currentTutorial);
                await LoadTutorialsAsync();

                await JanelaUtilities.ShowInfoDialogAsync(this, "Sucesso", "Tutorial salvo com sucesso!");

                ShowEmptyState();
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro", $"Não foi possível salvar o tutorial: {ex.Message}");
            }
        }

        /// <summary>
        /// Botão de cancelar edição
        /// </summary>
        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            ShowEmptyState();
        }

        /// <summary>
        /// Botão de adicionar passo
        /// </summary>
        private void AddStepButton_Click(object sender, RoutedEventArgs e)
        {
            _editingSteps.Add(new TutorialStepViewModel
            {
                Order = _editingSteps.Count + 1,
                Title = $"Passo {_editingSteps.Count + 1}",
                Content = "Digite o conteúdo do passo aqui."
            });
        }

        /// <summary>
        /// Botão de mover passo para cima
        /// </summary>
        private void MoveStepUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TutorialStepViewModel step)
            {
                var index = _editingSteps.IndexOf(step);
                if (index > 0)
                {
                    _editingSteps.Move(index, index - 1);
                    UpdateStepNumbers();
                }
            }
        }

        /// <summary>
        /// Botão de mover passo para baixo
        /// </summary>
        private void MoveStepDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TutorialStepViewModel step)
            {
                var index = _editingSteps.IndexOf(step);
                if (index < _editingSteps.Count - 1)
                {
                    _editingSteps.Move(index, index + 1);
                    UpdateStepNumbers();
                }
            }
        }

        /// <summary>
        /// Botão de excluir passo
        /// </summary>
        private async void DeleteStepButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TutorialStepViewModel step)
            {
                var confirm = await JanelaUtilities.ShowConfirmationDialogAsync(
                    this,
                    "Excluir Passo",
                    $"Tem certeza que deseja excluir o passo '{step.Title}'?",
                    "Excluir",
                    "Cancelar");

                if (confirm)
                {
                    _editingSteps.Remove(step);
                    UpdateStepNumbers();
                }
            }
        }

        /// <summary>
        /// Atualiza a numeração dos passos
        /// </summary>
        private void UpdateStepNumbers()
        {
            for (int i = 0; i < _editingSteps.Count; i++)
            {
                _editingSteps[i].Order = i + 1;
            }
        }

        /// <summary>
        /// Botão de selecionar imagem
        /// </summary>
        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTutorial == null || sender is not Button button || button.Tag is not TutorialStepViewModel step)
                return;

            try
            {
                var picker = new FileOpenPicker();
                var hwnd = WindowNative.GetWindowHandle(this);
                InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".gif");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var imageName = $"step{step.Order}";
                    var relativePath = await TutorialService.SaveTutorialImageAsync(
                        _currentTutorial.Id,
                        file.Path,
                        imageName);

                    step.ImagePath = relativePath;
                }
            }
            catch (Exception ex)
            {
                await JanelaUtilities.ShowErrorDialogAsync(this, "Erro", $"Não foi possível salvar a imagem: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// ViewModel para exibir um tutorial na lista
    /// </summary>
    public class TutorialViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string StepsCount { get; set; }
        public Visibility ShowEditButtons { get; set; }

        public TutorialViewModel(Tutorial tutorial, bool showEditButtons = false)
        {
            Id = tutorial.Id;
            Title = tutorial.Title;
            Description = tutorial.Description;
            StepsCount = tutorial.Steps.Count.ToString();
            ShowEditButtons = showEditButtons ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// ViewModel para editar passos de tutorial
    /// </summary>
    public class TutorialStepViewModel
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public string? CodeSnippet { get; set; }

        public string StepNumber => $"Passo {Order}";
    }
}
