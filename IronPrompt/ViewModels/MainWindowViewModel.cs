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

using IronPrompt.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IronPrompt.ViewModels;

public enum MessagePartType
{
    Text,
    Code,
    Header,
    Separator,
    Table
}

public partial class TableRowViewModel : ObservableObject
{
    public ObservableCollection<string> Cells { get; } = new();
}

public partial class MessagePartViewModel : ObservableObject
{
    [ObservableProperty]
    private MessagePartType _type;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private int _headerLevel = 3;

    public ObservableCollection<string> TableHeaders { get; } = new();
    public ObservableCollection<TableRowViewModel> TableRows { get; } = new();
}

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _senderName = string.Empty;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private string _thinkingText = string.Empty;

    [ObservableProperty]
    private bool _isThinking;

    [ObservableProperty]
    private bool _hasThinking;

    [ObservableProperty]
    private bool _isWaiting;

    public ObservableCollection<MessagePartViewModel> Parts { get; } = new();
    public ObservableCollection<string> ImagePaths { get; } = new();

    private StringBuilder _rawAccumulator = new();
    private StringBuilder _thinkingAccumulator = new();

    public string GetRawContent() => _rawAccumulator.ToString();

    public void SetRawContent(string content)
    {
        _rawAccumulator.Clear();
        _thinkingAccumulator.Clear();
        AppendAndParse(content);
    }

    private static bool IsSeparatorLine(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("|") || !trimmed.EndsWith("|")) return false;
        
        foreach (var c in trimmed)
        {
            if (c != '|' && c != '-' && c != ':' && c != ' ' && c != '\r' && c != '\n' && c != '\t')
            {
                return false;
            }
        }
        return true;
    }

    public void AppendAndParse(string newText, string thinkingText = "")
    {
        IsWaiting = false;

        // If we got dedicated thinking text from Ollama's "thinking" field
        if (!string.IsNullOrEmpty(thinkingText))
        {
            HasThinking = true;
            IsThinking = true;
            _thinkingAccumulator.Append(thinkingText);
            ThinkingText = _thinkingAccumulator.ToString().Trim();
            return;
        }

        // If we were thinking but now we receive actual response text, transition IsThinking to false
        if (IsThinking && string.IsNullOrEmpty(thinkingText) && !string.IsNullOrEmpty(newText))
        {
            IsThinking = false;
        }

        _rawAccumulator.Append(newText);
        string fullText = _rawAccumulator.ToString();

        string actualContent = fullText;
        string thinking = ThinkingText;
        bool isThinkingActive = IsThinking;
        bool hasThinkingText = HasThinking;

        int thinkStart = fullText.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        int tagLength = 7;
        int endTagLength = 8;
        string endTag = "</think>";

        if (thinkStart < 0)
        {
            thinkStart = fullText.IndexOf("<thought>", StringComparison.OrdinalIgnoreCase);
            if (thinkStart >= 0)
            {
                tagLength = 9;
                endTagLength = 10;
                endTag = "</thought>";
            }
            else
            {
                thinkStart = fullText.IndexOf("<thinking>", StringComparison.OrdinalIgnoreCase);
                if (thinkStart >= 0)
                {
                    tagLength = 10;
                    endTagLength = 11;
                    endTag = "</thinking>";
                }
            }
        }

        if (thinkStart >= 0)
        {
            hasThinkingText = true;
            int thinkEnd = fullText.IndexOf(endTag, thinkStart, StringComparison.OrdinalIgnoreCase);
            if (thinkEnd >= 0)
            {
                thinking = fullText.Substring(thinkStart + tagLength, thinkEnd - (thinkStart + tagLength)).Trim();
                actualContent = fullText.Substring(thinkEnd + endTagLength);
                isThinkingActive = false;
            }
            else
            {
                thinking = fullText.Substring(thinkStart + tagLength).Trim();
                actualContent = string.Empty;
                isThinkingActive = true;
            }
        }

        if (ThinkingText != thinking) ThinkingText = thinking;
        if (IsThinking != isThinkingActive) IsThinking = isThinkingActive;
        if (HasThinking != hasThinkingText) HasThinking = hasThinkingText;

        var codeSegments = actualContent.Split(new[] { "```" }, StringSplitOptions.None);
        var newParts = new System.Collections.Generic.List<MessagePartViewModel>();

        for (int i = 0; i < codeSegments.Length; i++)
        {
            var segmentText = codeSegments[i];
            bool isCodeMode = i % 2 != 0;

            if (isCodeMode)
            {
                if (segmentText.Contains('\n'))
                {
                    int firstNewLine = segmentText.IndexOf('\n');
                    segmentText = segmentText.Substring(firstNewLine + 1);
                }
                newParts.Add(new MessagePartViewModel
                {
                    Type = MessagePartType.Code,
                    Content = segmentText
                });
            }
            else
            {
                var lines = segmentText.Split('\n');
                var currentParagraph = new StringBuilder();
                bool inTableMode = false;
                MessagePartViewModel? currentTablePart = null;

                for (int j = 0; j < lines.Length; j++)
                {
                    var line = lines[j];
                    var trimmedLine = line.Trim();

                    // If currently reading table rows
                    if (inTableMode)
                    {
                        if (trimmedLine.StartsWith("|") && trimmedLine.EndsWith("|"))
                        {
                            var cells = line.Split('|');
                            var rowVm = new TableRowViewModel();
                            for (int k = 1; k < cells.Length - 1; k++)
                            {
                                rowVm.Cells.Add(cells[k].Trim());
                            }
                            currentTablePart!.TableRows.Add(rowVm);
                            continue;
                        }
                        else
                        {
                            // Table has ended, add it and switch off table mode
                            newParts.Add(currentTablePart!);
                            currentTablePart = null;
                            inTableMode = false;
                            // Fall through to process this line as normal text
                        }
                    }

                    // Check if a new table block starts
                    if (!inTableMode && trimmedLine.StartsWith("|") && trimmedLine.EndsWith("|") && j + 1 < lines.Length && IsSeparatorLine(lines[j + 1]))
                    {
                        // Flush any pending text paragraph
                        if (currentParagraph.Length > 0)
                        {
                            newParts.Add(new MessagePartViewModel
                            {
                                Type = MessagePartType.Text,
                                Content = currentParagraph.ToString().TrimEnd('\r', '\n')
                            });
                            currentParagraph.Clear();
                        }

                        currentTablePart = new MessagePartViewModel { Type = MessagePartType.Table };
                        
                        // Parse headers
                        var cells = line.Split('|');
                        for (int k = 1; k < cells.Length - 1; k++)
                        {
                            currentTablePart.TableHeaders.Add(cells[k].Trim());
                        }

                        j++; // Skip the separator line
                        inTableMode = true;
                        continue;
                    }

                    // Normal markdown line processing
                    if (trimmedLine == "---" || trimmedLine == "___" || trimmedLine == "***")
                    {
                        if (currentParagraph.Length > 0)
                        {
                            newParts.Add(new MessagePartViewModel
                            {
                                Type = MessagePartType.Text,
                                Content = currentParagraph.ToString().TrimEnd('\r', '\n')
                            });
                            currentParagraph.Clear();
                        }
                        
                        newParts.Add(new MessagePartViewModel
                        {
                            Type = MessagePartType.Separator,
                            Content = "---"
                        });
                    }
                    else if (trimmedLine.StartsWith("#### ") || trimmedLine.StartsWith("### ") || trimmedLine.StartsWith("## ") || trimmedLine.StartsWith("# "))
                    {
                        if (currentParagraph.Length > 0)
                        {
                            newParts.Add(new MessagePartViewModel
                            {
                                Type = MessagePartType.Text,
                                Content = currentParagraph.ToString().TrimEnd('\r', '\n')
                            });
                            currentParagraph.Clear();
                        }

                        int level = 3;
                        if (trimmedLine.StartsWith("# ")) level = 1;
                        else if (trimmedLine.StartsWith("## ")) level = 2;
                        else if (trimmedLine.StartsWith("#### ")) level = 4;

                        var headerText = trimmedLine.TrimStart('#').Trim();

                        newParts.Add(new MessagePartViewModel
                        {
                            Type = MessagePartType.Header,
                            Content = headerText,
                            HeaderLevel = level
                        });
                    }
                    else
                    {
                        currentParagraph.Append(line);
                        if (j < lines.Length - 1)
                        {
                            currentParagraph.Append('\n');
                        }
                    }
                }

                // If table is still open when segment ends
                if (inTableMode && currentTablePart != null)
                {
                    newParts.Add(currentTablePart);
                }

                if (currentParagraph.Length > 0)
                {
                    var remainingText = currentParagraph.ToString().TrimEnd('\r', '\n');
                    if (!string.IsNullOrEmpty(remainingText))
                    {
                        newParts.Add(new MessagePartViewModel
                        {
                            Type = MessagePartType.Text,
                            Content = remainingText
                        });
                    }
                }
            }
        }

        for (int i = 0; i < newParts.Count; i++)
        {
            if (i < Parts.Count)
            {
                if (Parts[i].Type != newParts[i].Type || 
                    Parts[i].Content != newParts[i].Content || 
                    Parts[i].HeaderLevel != newParts[i].HeaderLevel)
                {
                    Parts[i].Type = newParts[i].Type;
                    Parts[i].Content = newParts[i].Content;
                    Parts[i].HeaderLevel = newParts[i].HeaderLevel;
                }

                // Sync collections if this is a Table part
                if (Parts[i].Type == MessagePartType.Table)
                {
                    Parts[i].TableHeaders.Clear();
                    foreach (var h in newParts[i].TableHeaders)
                    {
                        Parts[i].TableHeaders.Add(h);
                    }

                    Parts[i].TableRows.Clear();
                    foreach (var r in newParts[i].TableRows)
                    {
                        Parts[i].TableRows.Add(r);
                    }
                }
            }
            else
            {
                Parts.Add(newParts[i]);
            }
        }

        while (Parts.Count > newParts.Count)
        {
            Parts.RemoveAt(Parts.Count - 1);
        }
    }
}

public partial class ChatSessionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    partial void OnSubtitleChanged(string value)
    {
        Save();
    }

    [ObservableProperty]
    private double _scrollOffset;

    [ObservableProperty]
    private bool _isEditing;

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    private Action<ChatSessionViewModel>? _saveCallback;

    public void Initialize(Action<ChatSessionViewModel> saveCallback)
    {
        _saveCallback = saveCallback;
    }

    public void Save()
    {
        _saveCallback?.Invoke(this);
    }

    public ChatSessionData ToData()
    {
        var data = new ChatSessionData
        {
            Id = this.Id,
            Title = this.Title,
            Subtitle = this.Subtitle,
            ScrollOffset = this.ScrollOffset,
            Messages = new System.Collections.Generic.List<ChatMessageData>()
        };

        foreach (var msg in this.Messages)
        {
            var msgData = new ChatMessageData
            {
                SenderName = msg.SenderName,
                IsUser = msg.IsUser,
                RawContent = msg.GetRawContent()
            };
            foreach (var path in msg.ImagePaths)
            {
                msgData.ImagePaths.Add(path);
            }
            data.Messages.Add(msgData);
        }

        return data;
    }

    [RelayCommand]
    private void StartRename()
    {
        IsEditing = true;
    }

    [RelayCommand]
    private void EndRename()
    {
        IsEditing = false;
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = MainWindowViewModel.CurrentLanguageStatic == "tr" ? "İsimsiz Sohbet" : "Unnamed Chat";
        }
        Save();
    }
}

public partial class MainWindowViewModel : ObservableObject
{
    public static string CurrentLanguageStatic { get; set; } = 
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", System.StringComparison.OrdinalIgnoreCase) ? "tr" : "en";

    [ObservableProperty]
    private string _currentLanguage = 
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("tr", System.StringComparison.OrdinalIgnoreCase) ? "tr" : "en"; // "tr" or "en"

    partial void OnCurrentLanguageChanged(string value)
    {
        CurrentLanguageStatic = value;
        RefreshLocalizedStrings();
    }

    public void RefreshLocalizedStrings()
    {
        OnPropertyChanged(nameof(SohbetlerTitle));
        OnPropertyChanged(nameof(YeniSohbetButton));
        OnPropertyChanged(nameof(YenidenAdlandirMenu));
        OnPropertyChanged(nameof(SohbetiSilMenu));
        OnPropertyChanged(nameof(GemmaSorPlaceholder));
        OnPropertyChanged(nameof(GonderButton));
        OnPropertyChanged(nameof(SeciliSohbetYok));
        OnPropertyChanged(nameof(ModelLabel));
        OnPropertyChanged(nameof(DusunmeSureciHeader));
        OnPropertyChanged(nameof(DusunuyorText));
        OnPropertyChanged(nameof(TamamlandiText));
        OnPropertyChanged(nameof(GemmaYanitHazirliyor));
        OnPropertyChanged(nameof(KopyalaText));
        OnPropertyChanged(nameof(KopyalandiText));
        OnPropertyChanged(nameof(KodBloguHeader));
        OnPropertyChanged(nameof(OllamaOfflineWarning));
        OnPropertyChanged(nameof(ImageAttachTooltip));
        OnPropertyChanged(nameof(SettingsTitle));
        OnPropertyChanged(nameof(AutoScrollLabel));
        OnPropertyChanged(nameof(LanguageLabel));
        OnPropertyChanged(nameof(CloseButton));
        OnPropertyChanged(nameof(ModelVisionWarningText));
        
        if (SelectedSession != null && SelectedSession.Id == "loading")
        {
            SelectedSession.Title = CurrentLanguage == "tr" ? "Yükleniyor..." : "Loading...";
        }
    }

    public string SohbetlerTitle => CurrentLanguage == "tr" ? "Sohbetler" : "Chats";
    public string YeniSohbetButton => CurrentLanguage == "tr" ? "Yeni Sohbet" : "New Chat";
    public string YenidenAdlandirMenu => CurrentLanguage == "tr" ? "Yeniden Adlandır" : "Rename";
    public string SohbetiSilMenu => CurrentLanguage == "tr" ? "Sohbeti Sil" : "Delete Chat";
    public string GemmaSorPlaceholder
    {
        get
        {
            var modelName = string.IsNullOrWhiteSpace(SelectedSession?.Subtitle) ? "gemma4:e4b" : SelectedSession.Subtitle;
            return CurrentLanguage == "tr" 
                ? $"{modelName} modeline sor..." 
                : $"Ask {modelName}...";
        }
    }
    public string GonderButton => CurrentLanguage == "tr" ? "Gönder" : "Send";
    public string SeciliSohbetYok => CurrentLanguage == "tr" ? "Seçili Sohbet Yok" : "No Chat Selected";
    public string ModelLabel => CurrentLanguage == "tr" ? "Model:" : "Model:";
    public string DusunmeSureciHeader => CurrentLanguage == "tr" ? "Düşünme Süreci" : "Thinking Process";
    public string DusunuyorText => CurrentLanguage == "tr" ? "Düşünüyor..." : "Thinking...";
    public string TamamlandiText => CurrentLanguage == "tr" ? "Tamamlandı" : "Completed";
    public string GemmaYanitHazirliyor
    {
        get
        {
            var modelName = string.IsNullOrWhiteSpace(SelectedSession?.Subtitle) ? "gemma4:e4b" : SelectedSession.Subtitle;
            return CurrentLanguage == "tr" 
                ? $"{modelName} yanıt hazırlıyor ve düşünüyor..." 
                : $"{modelName} is thinking and preparing a response...";
        }
    }
    public string KopyalaText => CurrentLanguage == "tr" ? "Kopyala" : "Copy";
    public string KopyalandiText => CurrentLanguage == "tr" ? "Kopyalandı!" : "Copied!";
    public string KodBloguHeader => CurrentLanguage == "tr" ? "KOD BLOĞU" : "CODE BLOCK";
    public string OllamaOfflineWarning => CurrentLanguage == "tr" 
        ? "Ollama çalışmıyor! Lütfen arka planda Ollama uygulamasını başlatın..." 
        : "Ollama is not running! Please start the Ollama application in the background...";
    public string ImageAttachTooltip => CurrentLanguage == "tr"
        ? "Görsel Ekle (Pano Ctrl+V / Sürükle Bırak)"
        : "Attach Image (Clipboard Ctrl+V / Drag & Drop)";

    public string SettingsTitle => CurrentLanguage == "tr" ? "Ayarlar" : "Settings";
    public string AutoScrollLabel => CurrentLanguage == "tr" ? "Modelden cevap geldikçe aşağı kaydır" : "Scroll down as model answers";
    public string LanguageLabel => CurrentLanguage == "tr" ? "Dil Seçimi:" : "Language Selection:";
    public string CloseButton => CurrentLanguage == "tr" ? "Kapat" : "Close";

    [ObservableProperty]
    private bool _isModelVisionCompatible = true;

    partial void OnIsModelVisionCompatibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsAttachmentEnabled));
    }

    [ObservableProperty]
    private bool _showModelVisionWarning;

    public string ModelVisionWarningText => CurrentLanguage == "tr"
        ? "Seçili model görsel girdisini desteklememektedir!"
        : "The selected model does not support image input!";

    private System.Threading.CancellationTokenSource? _warningCts;

    private void ShowVisionWarning()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ShowModelVisionWarning = true;
        });

        _warningCts?.Cancel();
        _warningCts = new System.Threading.CancellationTokenSource();
        var token = _warningCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(5000, token);
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ShowModelVisionWarning = false;
                });
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _autoScrollEnabled = true;

    public ObservableCollection<string> PendingImagePaths { get; } = new();

    [RelayCommand]
    private void RemovePendingImage(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Görsel temizleme hatası: {ex.Message}");
        }
        PendingImagePaths.Remove(path);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [ObservableProperty]
    private bool _isOllamaOnline = true;

    partial void OnIsOllamaOnlineChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSendPrompt));
        OnPropertyChanged(nameof(IsAttachmentEnabled));
    }

    partial void OnIsGeneratingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSendPrompt));
        OnPropertyChanged(nameof(IsAttachmentEnabled));
    }

    public bool CanSendPrompt => IsOllamaOnline && !IsGenerating;

    public bool IsAttachmentEnabled => IsModelVisionCompatible && CanSendPrompt;

    public ObservableCollection<ChatSessionViewModel> Sessions { get; } = new();

    [ObservableProperty]
    private ChatSessionViewModel? _selectedSession;

    public async Task<bool> TryAddPendingImage(string sourcePathOrBytes, byte[]? bytes = null)
    {
        var modelName = string.IsNullOrWhiteSpace(SelectedSession?.Subtitle) ? "gemma4:e4b" : SelectedSession.Subtitle;
        
        bool isOnline = false;
        try
        {
            using var response = await _httpClient.GetAsync("http://localhost:11434/");
            isOnline = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            isOnline = false;
        }

        if (!isOnline)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsOllamaOnline = false;
            });
            await StartOllamaConnectionCheck();
        }

        await CheckModelVisionCapability(modelName);

        if (!IsModelVisionCompatible)
        {
            ShowVisionWarning();
            return false;
        }

        AddPendingImage(sourcePathOrBytes, bytes);
        return true;
    }

    partial void OnSelectedSessionChanged(ChatSessionViewModel? oldValue, ChatSessionViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= SelectedSession_PropertyChanged;
        }
        if (newValue != null)
        {
            newValue.PropertyChanged += SelectedSession_PropertyChanged;
        }
        else
        {
            IsModelVisionCompatible = true;
        }
        OnPropertyChanged(nameof(GemmaSorPlaceholder));
        OnPropertyChanged(nameof(GemmaYanitHazirliyor));
    }

    private void SelectedSession_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatSessionViewModel.Subtitle))
        {
            OnPropertyChanged(nameof(GemmaSorPlaceholder));
            OnPropertyChanged(nameof(GemmaYanitHazirliyor));
        }
    }

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isGenerating;

    private int _sessionCounter = 1;

    private readonly string _storagePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "IronPrompt", 
        "Sessions"
    );

    private readonly string _imagesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "IronPrompt", 
        "Images"
    );

    public string AddPendingImage(string sourcePathOrBytes, byte[]? bytes = null)
    {
        try
        {
            if (!Directory.Exists(_imagesPath))
            {
                Directory.CreateDirectory(_imagesPath);
            }
            string extension = ".png";
            if (bytes == null)
            {
                extension = Path.GetExtension(sourcePathOrBytes);
                if (string.IsNullOrEmpty(extension)) extension = ".png";
            }
            string destPath = Path.Combine(_imagesPath, $"{Guid.NewGuid()}{extension}");
            if (bytes != null)
            {
                File.WriteAllBytes(destPath, bytes);
            }
            else
            {
                File.Copy(sourcePathOrBytes, destPath, true);
            }
            PendingImagePaths.Add(destPath);
            return destPath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Görsel ekleme hatası: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task CheckModelVisionCapability(string modelName)
    {
        if (string.IsNullOrEmpty(modelName))
        {
            IsModelVisionCompatible = false;
            return;
        }

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            string payload = $"{{\"name\":\"{modelName}\"}}";
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("http://localhost:11434/api/show", content);
            
            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                var showResponse = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.OllamaShowResponse);
                
                bool hasVision = false;
                if (showResponse?.Capabilities != null)
                {
                    foreach (var cap in showResponse.Capabilities)
                    {
                        if (cap.Equals("vision", StringComparison.OrdinalIgnoreCase))
                        {
                            hasVision = true;
                            break;
                        }
                    }
                }

                if (!hasVision && showResponse?.Details?.Families != null)
                {
                    foreach (var fam in showResponse.Details.Families)
                    {
                        if (fam.Equals("clip", StringComparison.OrdinalIgnoreCase))
                        {
                            hasVision = true;
                            break;
                        }
                    }
                }
                
                IsModelVisionCompatible = hasVision;
                return;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Vision kontrol hatası: {ex.Message}");
        }

        IsModelVisionCompatible = false;
    }

    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };

    private Task? _connectionCheckTask = null;

    private async Task StartOllamaConnectionCheck()
    {
        if (_connectionCheckTask == null)
        {
            _connectionCheckTask = RunConnectionCheckLoop();
        }
        await _connectionCheckTask;
    }

    private async Task RunConnectionCheckLoop()
    {
        try
        {
            while (true)
            {
                bool isOnline = false;
                try
                {
                    using var response = await _httpClient.GetAsync("http://localhost:11434/");
                    isOnline = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.OK;
                }
                catch
                {
                    isOnline = false;
                }

                // Update on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsOllamaOnline = isOnline;
                });

                if (isOnline) break;

                await Task.Delay(1500);
            }

            // Connection is established. Silently update capability checking.
            var modelName = string.IsNullOrWhiteSpace(SelectedSession?.Subtitle) ? "gemma4:e4b" : SelectedSession.Subtitle;
            await CheckModelVisionCapability(modelName);
        }
        finally
        {
            _connectionCheckTask = null;
        }
    }

    public MainWindowViewModel()
    {
        _selectedSession = new ChatSessionViewModel
        {
            Id = "loading",
            Title = _currentLanguage == "tr" ? "Yükleniyor..." : "Loading...",
            Subtitle = "gemma4:e4b"
        };
        _ = LoadSessionsAsync();
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            var files = Directory.GetFiles(_storagePath, "*.json");
            var fileInfos = new System.Collections.Generic.List<FileInfo>();
            foreach (var f in files)
            {
                fileInfos.Add(new FileInfo(f));
            }
            fileInfos.Sort((x, y) => x.LastWriteTime.CompareTo(y.LastWriteTime));

            var loadedSessions = new System.Collections.Generic.List<ChatSessionViewModel>();

            foreach (var fileInfo in fileInfos)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(fileInfo.FullName);
                    var data = JsonSerializer.Deserialize(json, OllamaJsonContext.Default.ChatSessionData);
                    if (data != null)
                    {
                        var vm = CreateSessionViewModel(data);
                        loadedSessions.Add(vm);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Oturum yükleme hatası ({fileInfo.Name}): {ex.Message}");
                }
            }

            // Cleanup orphaned image files
            _ = Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(_imagesPath))
                    {
                        var referencedImages = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var session in loadedSessions)
                        {
                            foreach (var msg in session.Messages)
                            {
                                foreach (var imgPath in msg.ImagePaths)
                                {
                                    referencedImages.Add(imgPath);
                                }
                            }
                        }

                        var imageFiles = Directory.GetFiles(_imagesPath);
                        foreach (var file in imageFiles)
                        {
                            if (!referencedImages.Contains(file))
                            {
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Orphaned file deletion failed: {ex.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Orphaned images cleanup error: {ex.Message}");
                }
            });

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Sessions.Clear();
                foreach (var session in loadedSessions)
                {
                    Sessions.Add(session);
                }

                if (Sessions.Count > 0)
                {
                    SelectedSession = Sessions[Sessions.Count - 1];
                }
                else
                {
                    CreateNewSession();
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Oturum yükleme genel hatası: {ex.Message}");
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Sessions.Count == 0)
                {
                    CreateNewSession();
                }
            });
        }
    }

    private ChatSessionViewModel CreateSessionViewModel(ChatSessionData data)
    {
        var vm = new ChatSessionViewModel
        {
            Id = data.Id,
            Title = data.Title,
            Subtitle = data.Subtitle,
            ScrollOffset = data.ScrollOffset
        };

        foreach (var msg in data.Messages)
        {
            var msgVm = new ChatMessageViewModel
            {
                SenderName = msg.SenderName,
                IsUser = msg.IsUser,
                IsWaiting = false
            };
            msgVm.SetRawContent(msg.RawContent);
            if (msg.ImagePaths != null)
            {
                foreach (var path in msg.ImagePaths)
                {
                    msgVm.ImagePaths.Add(path);
                }
            }
            vm.Messages.Add(msgVm);
        }

        vm.Initialize(async s => await SaveSessionAsync(s));
        return vm;
    }

    public async Task SaveSessionAsync(ChatSessionViewModel session)
    {
        try
        {
            var data = session.ToData();
            var filePath = Path.Combine(_storagePath, $"{session.Id}.json");
            var json = JsonSerializer.Serialize(data, OllamaJsonContext.Default.ChatSessionData);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Oturum kaydetme hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CreateNewSession()
    {
        string defaultTitle = CurrentLanguage == "tr" ? "Yeni Sohbet" : "New Chat";
        var newSession = new ChatSessionViewModel
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"{defaultTitle} {_sessionCounter++}",
            Subtitle = "gemma4:e4b"
        };
        newSession.Initialize(async s => await SaveSessionAsync(s));

        Sessions.Add(newSession);
        SelectedSession = newSession;

        _ = SaveSessionAsync(newSession);
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        await StartOllamaConnectionCheck();
        if (string.IsNullOrWhiteSpace(InputText) || SelectedSession == null || IsGenerating || !IsOllamaOnline) return;

        var modelName = string.IsNullOrWhiteSpace(SelectedSession.Subtitle) ? "gemma4:e4b" : SelectedSession.Subtitle;

        if (PendingImagePaths.Count > 0)
        {
            await CheckModelVisionCapability(modelName);
            if (!IsModelVisionCompatible)
            {
                ShowVisionWarning();
                return;
            }
        }

        var prompt = InputText;
        InputText = string.Empty;

        var senderName = CurrentLanguage == "tr" ? "Sen" : "You";
        var userMessage = new ChatMessageViewModel { SenderName = senderName, IsUser = true, IsWaiting = false };
        userMessage.AppendAndParse(prompt);
        foreach (var imgPath in PendingImagePaths)
        {
            userMessage.ImagePaths.Add(imgPath);
        }
        SelectedSession.Messages.Add(userMessage);
        PendingImagePaths.Clear();

        var gemmaMessage = new ChatMessageViewModel { SenderName = modelName, IsUser = false, IsWaiting = true };
        SelectedSession.Messages.Add(gemmaMessage);

        try
        {
            IsGenerating = true;
            using var client = new HttpClient();
            
            // Build the full chat history request
            var requestData = new OllamaChatRequest
            {
                Model = modelName,
                Stream = true
            };

            foreach (var msg in SelectedSession.Messages)
            {
                if (msg == gemmaMessage) continue;

                string content = msg.GetRawContent();
                if (!msg.IsUser && msg.HasThinking && !string.IsNullOrEmpty(msg.ThinkingText))
                {
                    content = $"<think>\n{msg.ThinkingText}\n</think>\n{content}";
                }

                var ollamaMsg = new OllamaChatMessage
                {
                    Role = msg.IsUser ? "user" : "assistant",
                    Content = content
                };

                if (msg.ImagePaths.Count > 0)
                {
                    ollamaMsg.Images = new System.Collections.Generic.List<string>();
                    foreach (var path in msg.ImagePaths)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                byte[] bytes = File.ReadAllBytes(path);
                                string base64 = Convert.ToBase64String(bytes);
                                ollamaMsg.Images.Add(base64);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Base64 conversion failed: {ex.Message}");
                        }
                    }
                }

                requestData.Messages.Add(ollamaMsg);
            }

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData, OllamaJsonContext.Default.OllamaChatRequest),
                Encoding.UTF8,
                "application/json"
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat") { Content = jsonContent };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var chunk = JsonSerializer.Deserialize(line, OllamaJsonContext.Default.OllamaChatResponseChunk);
                if (chunk != null && chunk.Message != null && (!string.IsNullOrEmpty(chunk.Message.Content) || !string.IsNullOrEmpty(chunk.Message.Thinking)))
                {
                    gemmaMessage.AppendAndParse(chunk.Message.Content, chunk.Message.Thinking);
                }

                if (chunk != null && chunk.Done) break;
            }

            if ((SelectedSession.Title.StartsWith("Yeni Sohbet") || SelectedSession.Title.StartsWith("New Chat")) && prompt.Length > 15)
            {
                SelectedSession.Title = prompt.Substring(0, 15) + "...";
            }

            await SaveSessionAsync(SelectedSession);
        }
        catch (Exception ex)
        {
            string errPrefix = CurrentLanguage == "tr" ? "Hata" : "Error";
            gemmaMessage.AppendAndParse($"{errPrefix}: {ex.Message}");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void DeleteSession(ChatSessionViewModel session)
    {
        if (session == null) return;

        if (SelectedSession == session)
        {
            SelectedSession = null;
        }

        Sessions.Remove(session);

        _ = Task.Run(() =>
        {
            try
            {
                foreach (var msg in session.Messages)
                {
                    foreach (var path in msg.ImagePaths)
                    {
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                        }
                    }
                }

                var filePath = Path.Combine(_storagePath, $"{session.Id}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Oturum silme hatası: {ex.Message}");
            }
        });

        if (Sessions.Count == 0)
        {
            CreateNewSession();
        }
    }
}