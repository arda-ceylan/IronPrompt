// Copyright 2026 Arda Ceylan
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using IronPrompt.ViewModels;

namespace IronPrompt.Views
{
    public partial class MainWindow : Window
    {
        private readonly string _settingsPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), 
            "IronPrompt", 
            "window_settings.json"
        );

        private ScrollViewer? _chatScrollViewer;
        private ChatSessionViewModel? _currentSession;

        public MainWindow()
        {
            InitializeComponent();
            
            _chatScrollViewer = this.FindControl<ScrollViewer>("ChatScrollViewer");

            var promptTextBox = this.FindControl<TextBox>("PromptTextBox");
            if (promptTextBox != null)
            {
                promptTextBox.AddHandler(InputElement.KeyDownEvent, PromptTextBox_KeyDown, RoutingStrategies.Tunnel, true);
            }

            this.DataContextChanged += (sender, args) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.PropertyChanged += Vm_PropertyChanged;
                    UpdateSession(vm.SelectedSession);
                }
            };
        }

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            LoadWindowSettings();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (_currentSession != null && _chatScrollViewer != null)
            {
                _currentSession.ScrollOffset = _chatScrollViewer.Offset.Y;
                _currentSession.Save();
            }
            SaveWindowSettings();
            base.OnClosing(e);
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedSession))
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    UpdateSession(vm.SelectedSession);
                }
            }
        }

        private void UpdateSession(ChatSessionViewModel? newSession)
        {
            if (_currentSession != null && _chatScrollViewer != null)
            {
                _currentSession.ScrollOffset = _chatScrollViewer.Offset.Y;
                _currentSession.Save();
            }

            _currentSession = newSession;

            if (_currentSession != null && _chatScrollViewer != null)
            {
                var targetOffset = _currentSession.ScrollOffset;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    if (_chatScrollViewer != null)
                    {
                        _chatScrollViewer.Offset = new Vector(_chatScrollViewer.Offset.X, targetOffset);
                    }
                }, Avalonia.Threading.DispatcherPriority.Render);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
                {
                    WindowState = WindowState.Normal;
                    e.Handled = true;
                }
            }
        }

        private void LoadWindowSettings()
        {
            try
            {
                if (System.IO.File.Exists(_settingsPath))
                {
                    var json = System.IO.File.ReadAllText(_settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize(json, IronPrompt.Models.OllamaJsonContext.Default.WindowSettingsData);
                    if (settings != null)
                    {
                        if (settings.Width.HasValue && settings.Height.HasValue)
                        {
                            Width = settings.Width.Value;
                            Height = settings.Height.Value;
                        }

                        if (settings.X.HasValue && settings.Y.HasValue)
                        {
                            var pos = new PixelPoint((int)settings.X.Value, (int)settings.Y.Value);
                            if (Screens != null)
                            {
                                var screen = Screens.ScreenFromPoint(pos);
                                if (screen != null)
                                {
                                    Position = pos;
                                }
                            }
                            else
                            {
                                Position = pos;
                            }
                        }

                        if (settings.WindowState == 2)
                        {
                            WindowState = WindowState.Maximized;
                        }
                        else
                        {
                            WindowState = WindowState.Normal;
                        }

                        if (DataContext is MainWindowViewModel vm && !string.IsNullOrEmpty(settings.Language))
                        {
                            vm.CurrentLanguage = settings.Language;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading window settings: {ex.Message}");
            }
        }

        private void SaveWindowSettings()
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                var settings = new IronPrompt.Models.WindowSettingsData();
                settings.WindowState = WindowState == WindowState.Maximized ? 2 : 0;
                settings.X = Position.X;
                settings.Y = Position.Y;
                settings.Width = Width;
                settings.Height = Height;
                
                if (DataContext is MainWindowViewModel vm)
                {
                    settings.Language = vm.CurrentLanguage;
                }

                var json = System.Text.Json.JsonSerializer.Serialize(settings, IronPrompt.Models.OllamaJsonContext.Default.WindowSettingsData);
                System.IO.File.WriteAllText(_settingsPath, json);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving window settings: {ex.Message}");
            }
        }

        private void ItemsControl_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (e.HeightChanged && sender is Control control)
            {
                var scrollViewer = control.Parent as ScrollViewer;
                if (DataContext is MainWindowViewModel vm && vm.IsGenerating)
                {
                    scrollViewer?.ScrollToEnd();
                }
            }
        }

        private void PromptTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // If Shift is NOT pressed, send message and suppress the default enter behavior
                if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    e.Handled = true; // Mark as handled to prevent TextBox from inserting a newline
                    
                    if (DataContext is MainWindowViewModel vm)
                    {
                        if (vm.SendMessageCommand.CanExecute(null))
                        {
                            vm.SendMessageCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public async void CopyCodeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string codeText)
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(codeText);
                    
                    if (button.Content is StackPanel panel)
                    {
                        TextBlock? iconBlock = null;
                        TextBlock? textBlock = null;
                        
                        foreach (var child in panel.Children)
                        {
                            if (child is TextBlock tb)
                            {
                                if (iconBlock == null) iconBlock = tb;
                                else textBlock = tb;
                            }
                        }

                        if (iconBlock != null && textBlock != null && DataContext is MainWindowViewModel vm)
                        {
                            iconBlock.Text = "✓";
                            textBlock.Text = vm.KopyalandiText;
                            
                            await System.Threading.Tasks.Task.Delay(2000);
                            
                            iconBlock.Text = "📋";
                            textBlock.Text = vm.KopyalaText;
                        }
                    }
                }
            }
        }

        private void LanguageButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string languageCode)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.CurrentLanguage = languageCode;
                }
            }
        }
    }
}