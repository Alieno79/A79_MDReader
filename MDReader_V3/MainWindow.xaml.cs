using MDReader.Services;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using System.IO;

namespace MDReader;

public partial class MainWindow : Window
{
    private readonly MarkdownService _markdownService = new();
    private string _currentHtml = "";
    private bool _webViewReady;
    private int _totalMatches;
    private int _currentMatchIndex;
    private string _lastQuery = "";
    private bool _darkMode;
    private static readonly string SettingsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDReader", "settings.json");

    private string? _currentFilePath;
    private string _originalText = "";
    private bool _isModified;
    private bool _editorVisible;
    private bool _horizontalLayout;
    private int _savedSelStart = -1;
    private int _savedSelLen = -1;
    private bool _isUpdatingSel;
    private bool _isTempFile;
    private string? _fromTxtOrigPath;

    public MainWindow()
    {
        InitializeComponent();
        HideToolbarOverflowButton();
        LoadSettings();
        btnDarkMode.IsChecked = _darkMode;
        ApplyWpfDarkMode();
        UpdateModifiedState();
        _editorVisible = false;
        _horizontalLayout = false;
        btnToggleEditor.IsChecked = false;
        lblLayout.Text = "\u2194";
        UpdateContentLayout();
        Loaded += MainWindow_Loaded;
    }

    private async Task UpdatePreview()
    {
        try
        {
            string text = txtEditor.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                emptyState.Visibility = Visibility.Visible;
                if (_webViewReady && webView.CoreWebView2 != null)
                    webView.CoreWebView2.NavigateToString("<html><body></body></html>");
                _currentHtml = "";
                return;
            }

            emptyState.Visibility = Visibility.Collapsed;
            var result = _markdownService.ConvertToHtml(text);
            _currentHtml = result.Html;
            tocList.ItemsSource = result.TocEntries;

            if (!_webViewReady)
            {
                await webView.EnsureCoreWebView2Async();
                if (!_webViewReady) return;
            }

            _lastQuery = "";
            _totalMatches = 0;
            _currentMatchIndex = -1;
            lblMatchCount.Content = "0/0";
            btnPrevMatch.IsEnabled = false;
            btnNextMatch.IsEnabled = false;

            webView.NavigateToString(_currentHtml);
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore anteprima: {ex.Message}";
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            var path = args[1];
            if (IsAllowedFile(path) && File.Exists(path))
                await LoadFile(path);
        }
    }

    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_darkMode && _webViewReady)
            await webView.ExecuteScriptAsync($"toggleDarkMode({JsonSerializer.Serialize(true)})");
    }

    private async void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        _webViewReady = e.IsSuccess;
        if (!_webViewReady || webView.CoreWebView2 == null) return;
        webView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            if (args.TryGetWebMessageAsString() == "printDone")
            {
                if (_savedPreviewBg != null) borderPreview.Background = _savedPreviewBg;
                if (_savedPreviewFg != null) lblPreview.Foreground = _savedPreviewFg;
                btnRefreshPreviewHeader.Visibility = Visibility.Visible;
                DismissPrintOverlay();
                webView.Margin = new Thickness(0);
            }
        };
        if (_darkMode)
            await webView.ExecuteScriptAsync($"toggleDarkMode({JsonSerializer.Serialize(_darkMode)})");
    }

    private async void BtnOpen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "File supportati (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|Markdown (*.md;*.markdown)|*.md;*.markdown|Testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
            Title = "Apri file"
        };

        if (dlg.ShowDialog() != true) return;
        await LoadFile(dlg.FileName);
    }

    private async Task LoadFile(string filePath)
    {
        try
        {
            statusFile.Text = "Caricamento in corso...";
            string content = File.ReadAllText(filePath);

            string ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            if (ext == ".txt")
            {
                content = content.Replace("\r\n", "\n");
                var lines = content.Split('\n');
                var sb = new StringBuilder();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (i > 0) sb.Append('\n');
                    if (lines[i].Length > 0)
                        sb.Append(lines[i] + "  ");
                }
                content = sb.ToString();

                string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MDReader");
                Directory.CreateDirectory(dir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string tempPath = System.IO.Path.Combine(dir, $"tempFromTXT_{timestamp}.md");
                File.WriteAllText(tempPath, content);
                _currentFilePath = tempPath;
                _isTempFile = true;
                _fromTxtOrigPath = filePath;
                statusFile.Text = $"Convertito da {System.IO.Path.GetFileName(filePath)}. Salva per scegliere formato.";
            }
            else
            {
                _currentFilePath = filePath;
                _fromTxtOrigPath = null;
            }

            txtEditor.Text = content;
            _originalText = content;
            _isModified = false;
            UpdateModifiedState();
            await UpdatePreview();
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore: {ex.Message}";
        }
    }

    private async void BtnNew_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MDReader");
            Directory.CreateDirectory(dir);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempPath = System.IO.Path.Combine(dir, $"temporaneo_{timestamp}.md");
            File.WriteAllText(tempPath, "");
            _currentFilePath = tempPath;
            _isTempFile = true;
            _isModified = false;
            _originalText = "";
            txtEditor.Text = "";
            btnToggleEditor.IsChecked = true;
            BtnToggleEditor_Click(sender, e);
            txtEditor.Focus();
            statusFile.Text = "Nuovo file temporaneo. Salva per scegliere la destinazione.";
            await UpdatePreview();
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore: {ex.Message}";
        }
    }

    private void BtnGuide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath is null) { statusFile.Text = "Errore: percorso eseguibile non trovato."; return; }
            var guidePath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(exePath)!, "Guida.md");
            if (File.Exists(guidePath))
                System.Diagnostics.Process.Start(exePath, guidePath);
            else
                statusFile.Text = "File Guida.md non trovato.";
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore: {ex.Message}";
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (_fromTxtOrigPath != null)
        {
            var result = System.Windows.MessageBox.Show(
                "Salvare come Markdown (.md) o come testo (.txt)?\n\n" +
                "  Sì    → salva come .md (apre finestra per scegliere destinazione)\n" +
                "  No    → salva come .txt (sostituisce il file originale)\n" +
                "  Annulla → non salvare",
                "Salva file",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
                return;

            if (result == MessageBoxResult.Yes)
            {
                await SaveAs();
                return;
            }

            // Save as .txt
            await SaveToFile(_fromTxtOrigPath);
            if (_isTempFile && _currentFilePath != null && File.Exists(_currentFilePath))
                File.Delete(_currentFilePath);
            _currentFilePath = _fromTxtOrigPath;
            _isTempFile = false;
            _fromTxtOrigPath = null;
            statusFile.Text = $"Salvato: {_currentFilePath}";
            return;
        }

        if (_isTempFile)
        {
            await SaveAs();
            return;
        }
        if (_currentFilePath != null)
            await SaveToFile(_currentFilePath);
        else
            await SaveAs();
    }

    private async void BtnSaveAs_Click(object sender, RoutedEventArgs e)
    {
        await SaveAs();
    }

    private async Task SaveAs()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Markdown (*.md)|*.md|Testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
            Title = "Salva file",
            FileName = _currentFilePath != null ? System.IO.Path.GetFileName(_currentFilePath) : "documento.md"
        };

        if (dlg.ShowDialog() == true)
        {
            string? oldTemp = _isTempFile ? _currentFilePath : null;
            _currentFilePath = dlg.FileName;
            _isTempFile = false;
            _fromTxtOrigPath = null;
            await SaveToFile(_currentFilePath);
            if (oldTemp != null && File.Exists(oldTemp))
                File.Delete(oldTemp);
        }
    }

    private async Task SaveToFile(string path)
    {
        try
        {
            File.WriteAllText(path, txtEditor.Text);
            _originalText = txtEditor.Text;
            _isModified = false;
            UpdateModifiedState();
            statusFile.Text = $"Salvato: {path}";
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore salvataggio: {ex.Message}";
        }
    }

    private void TxtEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        _isModified = _currentFilePath != null && txtEditor.Text != _originalText;
        UpdateModifiedState();
    }

    private void TxtEditor_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (!_isUpdatingSel)
        {
            _savedSelStart = -1;
            _savedSelLen = -1;
        }
        UpdateCursorPosition();
    }

    private void UpdateCursorPosition()
    {
        int caret = txtEditor.CaretIndex;
        string text = txtEditor.Text;
        if (string.IsNullOrEmpty(text))
        {
            statusCursor.Text = "Ln 1  Col 1";
            return;
        }

        int lineNumber = 1;
        int colNumber = 1;
        for (int i = 0; i < caret && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lineNumber++;
                colNumber = 1;
            }
            else
            {
                colNumber++;
            }
        }

        statusCursor.Text = $"Ln {lineNumber}  Col {colNumber}";
    }

    private void UpdateModifiedState()
    {
        if (_isModified)
        {
            string name = _currentFilePath != null ? System.IO.Path.GetFileName(_currentFilePath) : "nuovo";
            Title = $"* {name} - MDReader";
            statusModified.Text = "Modificato";
        }
        else
        {
            string name = _currentFilePath != null ? System.IO.Path.GetFileName(_currentFilePath) : "MDReader";
            Title = _currentFilePath != null ? $"{name} - MDReader" : "MDReader";
            statusModified.Text = "";
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && IsAllowedFile(files[0]))
            {
                var pos = e.GetPosition(this);
                double tocW = tocColumn.ActualWidth > 0 ? tocColumn.ActualWidth : tocColumn.MinWidth;
                bool overToc = pos.X < tocW;

                if (overToc)
                {
                    e.Effects = DragDropEffects.Copy;
                    tocDropOverlay.Visibility = Visibility.Visible;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                    tocDropOverlay.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
                HideDropOverlays();
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
            HideDropOverlays();
        }
        e.Handled = true;
    }


    private void Window_DragLeave(object sender, DragEventArgs e)
    {
        HideDropOverlays();
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        HideDropOverlays();

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files?.Length > 0 && IsAllowedFile(files[0]))
            {
                if (_currentFilePath == null)
                {
                    await LoadFile(files[0]);
                }
                else
                {
                    var exePath = Environment.ProcessPath;
                    if (exePath is null) { statusFile.Text = "Errore: percorso eseguibile non trovato."; return; }
                    System.Diagnostics.Process.Start(exePath, files[0]);
                }
            }
        }
    }

    private void HideDropOverlays()
    {
        tocDropOverlay.Visibility = Visibility.Collapsed;
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isModified)
        {
            var result = MessageBox.Show("Il documento contiene modifiche non salvate. Salvare prima di uscire?",
                "MDReader", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                BtnSave_Click(this, new RoutedEventArgs());
                if (_isModified)
                {
                    e.Cancel = true;
                    return;
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        webView.Dispose();
        Environment.Exit(0);
    }

    private static bool IsAllowedFile(string path)
    {
        string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext is ".md" or ".markdown" or ".mdown" or ".mdwn" or ".txt";
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentHtml))
        {
            statusFile.Text = "Nessun documento da copiare";
            return;
        }

        try
        {
            string htmlData = FormatHtmlForClipboard(_currentHtml);
            Clipboard.SetText(htmlData, TextDataFormat.Html);
            statusFile.Text = "Contenuto copiato negli appunti";
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore copia: {ex.Message}";
        }
    }

    private Brush? _savedPreviewBg;
    private Brush? _savedPreviewFg;

    private async void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        if (!_webViewReady || string.IsNullOrEmpty(_currentHtml)) return;

        _savedPreviewBg = borderPreview.Background;
        _savedPreviewFg = lblPreview.Foreground;
        var hideColor = new SolidColorBrush(Color.FromRgb(0x1e, 0x1e, 0x1e));
        borderPreview.Background = _darkMode ? hideColor : Brushes.White;
        lblPreview.Foreground = _darkMode ? hideColor : Brushes.White;
        btnRefreshPreviewHeader.Visibility = Visibility.Collapsed;

        await webView.ExecuteScriptAsync(@"
(function(){
    var o=document.createElement('div');
    o.id='_mdDim';
    o.style.cssText='position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.3);pointer-events:none;';
    document.body.appendChild(o);
    window.onafterprint=function(){
        var e=document.getElementById('_mdDim');
        if(e)e.remove();
        window.chrome.webview.postMessage('printDone');
    };
    setTimeout(function(){
        o.style.display='none';
        setTimeout(function(){
            window.print();
            o.style.display='';
        },10);
    },100);
})();");
    }

    private void PrintOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DismissPrintOverlay();
    }

    private void DismissPrintOverlay()
    {
        printOverlay.Visibility = Visibility.Collapsed;
        printOverlay.IsHitTestVisible = false;
    }

    private void BtnTop_Click(object sender, RoutedEventArgs e)
    {
        if (!_webViewReady) return;
        _ = webView.ExecuteScriptAsync("scrollToTop()");
    }

    private async void BtnDarkMode_Click(object sender, RoutedEventArgs e)
    {
        _darkMode = btnDarkMode.IsChecked == true;
        await ApplyDarkMode();
        SaveSettings();
    }

    private async Task ApplyDarkMode()
    {
        ApplyWpfDarkMode();
        if (_webViewReady && !string.IsNullOrEmpty(_currentHtml))
            await webView.ExecuteScriptAsync($"toggleDarkMode({JsonSerializer.Serialize(_darkMode)})");
    }

    private void HideToolbarOverflowButton()
    {
        foreach (var tray in new[] { toolbarTray, formattingTray })
        {
            if (VisualTreeHelper.GetChild(tray, 0) is ToolBar tb)
            {
                foreach (var item in tb.Items)
                    if (item is DependencyObject dep)
                        ToolBar.SetOverflowMode(dep, OverflowMode.Never);
            }
        }
    }

    private void ApplyWpfDarkMode()
    {
        lblDarkMode.Text = _darkMode ? "Chiaro" : "Scuro";
        var darkBg = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        var darkText = new SolidColorBrush(Color.FromRgb(212, 212, 212));
        var lightBg = new SolidColorBrush(Color.FromRgb(240, 240, 240));
        var mediumBg = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        var btnBrush = _darkMode ? darkText : Brushes.Black;

        toolbarTray.Background = _darkMode ? darkBg : SystemColors.MenuBarBrush;
        formattingTray.Background = _darkMode ? darkBg : SystemColors.MenuBarBrush;
        var borderBrush = _darkMode ? mediumBg : new SolidColorBrush(Color.FromRgb(204, 204, 204));
        borderToolbar.BorderBrush = borderBrush;
        borderToolbar.Background = _darkMode ? darkBg : SystemColors.MenuBarBrush;
        borderFormatting.BorderBrush = borderBrush;
        borderFormatting.Background = _darkMode ? darkBg : SystemColors.MenuBarBrush;
        borderStatus.BorderBrush = borderBrush;

        foreach (var tray in new[] { toolbarTray, formattingTray })
        {
            foreach (var item in LogicalTreeHelper.GetChildren(tray))
            {
                if (item is ToolBar tb)
                {
                    tb.Background = _darkMode ? mediumBg : Brushes.Transparent;
                    foreach (var child in tb.Items)
                    {
                        if (child is Button btn)
                            btn.Foreground = btnBrush;
                        else if (child is ToggleButton tbtn)
                            tbtn.Foreground = btnBrush;
                        else if (child is Label lbl)
                            lbl.Foreground = btnBrush;
                    }
                }
            }
        }

        statusBar.Background = _darkMode ? darkBg : Brushes.Transparent;
        statusFile.Foreground = _darkMode ? darkText : Brushes.Black;
        statusFileItem.Background = _darkMode ? darkBg : Brushes.Transparent;
        statusModified.Foreground = _darkMode ? darkText : Brushes.Black;
        statusModifiedItem.Background = _darkMode ? darkBg : Brushes.Transparent;
        statusCursor.Foreground = _darkMode ? darkText : Brushes.Black;

        borderEditor.Background = _darkMode ? mediumBg : lightBg;
        lblEditor.Foreground = _darkMode ? darkText : Brushes.Black;

        borderPreview.Background = _darkMode ? mediumBg : lightBg;
        lblPreview.Foreground = _darkMode ? darkText : Brushes.Black;
        txtEditor.Background = _darkMode ? darkBg : Brushes.White;
        txtEditor.Foreground = _darkMode ? darkText : Brushes.Black;
        txtSearch.Background = _darkMode ? new SolidColorBrush(Color.FromRgb(60, 60, 60)) : Brushes.White;
        txtSearch.Foreground = _darkMode ? darkText : Brushes.Black;

        tocSplitter.Background = _darkMode ? mediumBg : new SolidColorBrush(Color.FromRgb(224, 224, 224));
        editorSplitter.Background = _darkMode ? mediumBg : new SolidColorBrush(Color.FromRgb(224, 224, 224));

        contentGrid.Background = _darkMode ? darkBg : Brushes.White;
        emptyState.Background = _darkMode ? darkBg : Brushes.White;
        emptyText.Foreground = _darkMode ? darkText : new SolidColorBrush(Color.FromRgb(136, 136, 136));

        tocPanel.Background = _darkMode ? darkBg : Brushes.White;
        borderTocHeader.Background = _darkMode ? mediumBg : lightBg;
        borderTocHeader.BorderBrush = _darkMode ? mediumBg : new SolidColorBrush(Color.FromRgb(204, 204, 204));
        lblToc.Foreground = _darkMode ? darkText : Brushes.Black;
        tocList.Background = _darkMode ? darkBg : Brushes.White;
        tocList.Foreground = _darkMode ? darkText : Brushes.Black;
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var data = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                _darkMode = data?.GetValueOrDefault("darkMode", false) ?? false;
            }
        }
        catch { _darkMode = false; }
    }

    private void SaveSettings()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new { darkMode = _darkMode });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private async void TocList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (tocList.SelectedItem is TocEntry entry && _webViewReady)
            await webView.ExecuteScriptAsync($"scrollToId({JsonSerializer.Serialize(entry.Id)})");
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            BtnNew_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.S)
        {
            BtnSave_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.O)
        {
            BtnOpen_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.F)
        {
            txtSearch.Focus();
            txtSearch.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.B)
        {
            BtnBold_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.I)
        {
            BtnItalic_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.C && !txtEditor.IsFocused)
        {
            BtnCopy_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.P)
        {
            BtnRefreshPreview_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.P)
        {
            BtnPrint_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.Home)
        {
            BtnTop_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3 && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
        {
            BtnPrevMatch_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F3)
        {
            if (_totalMatches > 0)
                BtnNextMatch_Click(sender, e);
            else
                DoSearch();
            e.Handled = true;
            return;
        }
    }

    private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DoSearch();
            e.Handled = true;
        }
    }

    private async void DoSearch()
    {
        string query = txtSearch.Text.Trim();
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(_currentHtml))
        {
            statusFile.Text = "Nessun documento aperto o query vuota";
            return;
        }

        try
        {
            _lastQuery = query;
            if (!_webViewReady)
            {
                await webView.EnsureCoreWebView2Async();
                _webViewReady = true;
            }

            string json = await webView.ExecuteScriptAsync($"findText({JsonSerializer.Serialize(query)})");
            if (int.TryParse(json, out int count))
            {
                _totalMatches = count;
                _currentMatchIndex = count > 0 ? 0 : -1;

                if (count > 0)
                    await webView.ExecuteScriptAsync($"focusMatch(0)");

                UpdateMatchUI();
            }
        }
        catch (Exception ex)
        {
            statusFile.Text = $"Errore ricerca: {ex.Message}";
        }
    }

    private async void BtnNextMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_totalMatches == 0) return;
        _currentMatchIndex = (_currentMatchIndex + 1) % _totalMatches;
        await webView.ExecuteScriptAsync($"focusMatch({_currentMatchIndex})");
        UpdateMatchUI();
    }

    private async void BtnPrevMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_totalMatches == 0) return;
        _currentMatchIndex = (_currentMatchIndex - 1 + _totalMatches) % _totalMatches;
        await webView.ExecuteScriptAsync($"focusMatch({_currentMatchIndex})");
        UpdateMatchUI();
    }

    private void UpdateMatchUI()
    {
        if (_totalMatches > 0)
        {
            lblMatchCount.Content = $"{_currentMatchIndex + 1}/{_totalMatches}";
            btnPrevMatch.IsEnabled = true;
            btnNextMatch.IsEnabled = true;
            statusFile.Text = $"{_totalMatches} occorrenze trovate";
        }
        else
        {
            lblMatchCount.Content = "0/0";
            btnPrevMatch.IsEnabled = false;
            btnNextMatch.IsEnabled = false;
            statusFile.Text = "Nessuna occorrenza trovata";
        }
    }

    private static string WrapLine(string line, string prefix, string suffix)
    {
        foreach (string p in new[] { "- ", "* ", "+ ", "> ", "# ", "## ", "### ", "#### ", "##### ", "###### " })
        {
            if (line.StartsWith(p))
                return p + prefix + line.Substring(p.Length) + suffix;
        }
        var m = Regex.Match(line, @"^(\d+\. )(.*)$");
        if (m.Success)
            return m.Groups[1].Value + prefix + m.Groups[2].Value + suffix;
        return prefix + line + suffix;
    }

    private static bool TryFindOuterWrap(string text, int contentStart, int contentLen, string prefix, string suffix, out int outerStart, out int outerLen)
    {
        outerStart = -1;
        outerLen = 0;

        if (prefix == suffix)
        {
            int count = 0;
            int lastPrefix = -1;
            for (int i = 0; i <= contentStart - prefix.Length; i++)
            {
                if (i + prefix.Length <= text.Length && text.Substring(i, prefix.Length) == prefix)
                {
                    count++;
                    lastPrefix = i;
                }
            }
            if (count % 2 == 1 && lastPrefix >= 0)
            {
                int matchCount = count;
                for (int i = contentStart + contentLen; i <= text.Length - prefix.Length; i++)
                {
                    if (text.Substring(i, prefix.Length) == prefix)
                    {
                        matchCount++;
                        if (matchCount % 2 == 0)
                        {
                            outerStart = lastPrefix;
                            outerLen = i + prefix.Length - lastPrefix;
                            return true;
                        }
                    }
                }
            }
        }
        else
        {
            int lastOpen = text.LastIndexOf(prefix, contentStart - 1);
            int firstClose = text.IndexOf(suffix, contentStart + contentLen);
            if (lastOpen >= 0 && firstClose >= 0 && lastOpen + prefix.Length < firstClose)
            {
                outerStart = lastOpen;
                outerLen = firstClose + suffix.Length - lastOpen;
                return true;
            }
        }
        return false;
    }

    private static string UnwrapLine(string line, string prefix, string suffix)
    {
        foreach (string p in new[] { "- ", "* ", "+ ", "> ", "# ", "## ", "### ", "#### ", "##### ", "###### " })
        {
            if (line.StartsWith(p))
            {
                string rest = line.Substring(p.Length);
                if (rest.Length >= prefix.Length + suffix.Length && rest.StartsWith(prefix) && rest.EndsWith(suffix))
                    return p + rest.Substring(prefix.Length, rest.Length - prefix.Length - suffix.Length);
                return line;
            }
        }
        var m = Regex.Match(line, @"^(\d+\. )(.*)$");
        if (m.Success)
        {
            string rest = m.Groups[2].Value;
            if (rest.Length >= prefix.Length + suffix.Length && rest.StartsWith(prefix) && rest.EndsWith(suffix))
                return m.Groups[1].Value + rest.Substring(prefix.Length, rest.Length - prefix.Length - suffix.Length);
            return line;
        }
        if (line.Length >= prefix.Length + suffix.Length && line.StartsWith(prefix) && line.EndsWith(suffix))
            return line.Substring(prefix.Length, line.Length - prefix.Length - suffix.Length);
        return line;
    }

    private void InsertWrap(string prefix, string suffix)
    {
        int start;
        int len;

        if (_savedSelStart >= 0 && _savedSelLen > 0)
        {
            start = _savedSelStart;
            len = _savedSelLen;
        }
        else
        {
            start = txtEditor.SelectionStart;
            len = txtEditor.SelectionLength;
        }

        if (len == 0)
        {
            txtEditor.Text = txtEditor.Text.Insert(start, prefix + suffix);
            txtEditor.CaretIndex = start + prefix.Length;
            _savedSelStart = -1;
            _savedSelLen = -1;
            return;
        }

        string text = txtEditor.Text;
        int leading = 0;
        while (leading < len && char.IsWhiteSpace(text[start + leading]))
            leading++;
        int trailing = 0;
        while (trailing < len - leading && char.IsWhiteSpace(text[start + len - 1 - trailing]))
            trailing++;

        int contentStart = start + leading;
        int contentLen = len - leading - trailing;

        if (contentLen == 0)
        {
            _savedSelStart = -1;
            _savedSelLen = -1;
            return;
        }

        string content = text.Substring(contentStart, contentLen);
        string formatted;
        int newStart;
        int newLen;

        if (content.Contains('\n'))
        {
            string sep = content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = content.Split(new[] { sep }, StringSplitOptions.None);
            bool allWrapped = lines.All(l =>
            {
                string test = l;
                foreach (string p in new[] { "- ", "* ", "+ ", "> ", "# ", "## ", "### ", "#### ", "##### ", "###### " })
                    if (test.StartsWith(p)) { test = test.Substring(p.Length); break; }
                var m2 = Regex.Match(test, @"^(\d+\. )(.*)$");
                if (m2.Success) test = m2.Groups[2].Value;
                return test.Length >= prefix.Length + suffix.Length && test.StartsWith(prefix) && test.EndsWith(suffix);
            });
            bool anyWrapped = lines.Any(l => l.Contains(prefix) || (prefix != suffix && l.Contains(suffix)));
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append(sep);
                if (allWrapped)
                    sb.Append(UnwrapLine(lines[i], prefix, suffix));
                else if (anyWrapped)
                {
                    string stripped = lines[i].Replace(prefix, "").Replace(suffix, "");
                    sb.Append(WrapLine(stripped, prefix, suffix));
                }
                else
                    sb.Append(WrapLine(lines[i], prefix, suffix));
            }
            formatted = sb.ToString();
            newStart = contentStart;
            newLen = formatted.Length;
        }
        else
        {
            bool isWrapped = content.Length >= prefix.Length + suffix.Length
                && content.StartsWith(prefix) && content.EndsWith(suffix);

            bool isInsideTags = !isWrapped
                && contentStart >= prefix.Length
                && text.Substring(contentStart - prefix.Length, prefix.Length) == prefix
                && contentStart + contentLen + suffix.Length <= text.Length
                && text.Substring(contentStart + contentLen, suffix.Length) == suffix;

            if (isWrapped)
            {
                formatted = UnwrapLine(content, prefix, suffix);
                newStart = contentStart;
                newLen = formatted.Length;
            }
            else if (isInsideTags)
            {
                int expStart = contentStart - prefix.Length;
                int expLen = contentLen + prefix.Length + suffix.Length;
                string expanded = text.Substring(expStart, expLen);
                formatted = UnwrapLine(expanded, prefix, suffix);
                newStart = expStart;
                newLen = formatted.Length;
                contentStart = expStart;
                contentLen = expLen;
            }
            else if (TryFindOuterWrap(text, contentStart, contentLen, prefix, suffix, out int outerStart, out int outerLen))
            {
                string expanded = text.Substring(outerStart, outerLen);
                formatted = UnwrapLine(expanded, prefix, suffix);
                newStart = outerStart;
                newLen = formatted.Length;
                contentStart = outerStart;
                contentLen = outerLen;
            }
            else if (content.Contains(prefix) || (prefix != suffix && content.Contains(suffix)))
            {
                string stripped = content.Replace(prefix, "").Replace(suffix, "");
                formatted = prefix + stripped + suffix;
                newStart = contentStart + prefix.Length;
                newLen = stripped.Length;
            }
            else
            {
                formatted = prefix + content + suffix;
                newStart = contentStart + prefix.Length;
                newLen = contentLen;
            }
        }

        txtEditor.Text = text.Remove(contentStart, contentLen)
            .Insert(contentStart, formatted);

        _isUpdatingSel = true;
        txtEditor.SelectionStart = newStart;
        txtEditor.SelectionLength = newLen;
        _isUpdatingSel = false;

        _savedSelStart = newStart;
        _savedSelLen = newLen;
    }

    private void BtnBold_Click(object sender, RoutedEventArgs e)
    {
        InsertWrap("**", "**");
    }

    private void BtnItalic_Click(object sender, RoutedEventArgs e)
    {
        InsertWrap("_", "_");
    }

    private void BtnUnderline_Click(object sender, RoutedEventArgs e)
    {
        InsertWrap("<u>", "</u>");
    }

    private void BtnStrikethrough_Click(object sender, RoutedEventArgs e)
    {
        InsertWrap("~~", "~~");
    }

    private void InsertLinePrefix(string prefix, bool numbered = false)
    {
        int selStart, selLen;
        if (_savedSelStart >= 0 && _savedSelLen > 0)
        {
            selStart = _savedSelStart;
            selLen = _savedSelLen;
        }
        else
        {
            selStart = txtEditor.SelectionStart;
            selLen = txtEditor.SelectionLength;
        }

        string text = txtEditor.Text;

        if (selLen == 0)
        {
            int lineStart = selStart > 0 ? text.LastIndexOf('\n', selStart - 1) + 1 : 0;
            string linePrefix = numbered ? "1. " : prefix;
            txtEditor.Text = text.Insert(lineStart, linePrefix);
            txtEditor.CaretIndex = lineStart + linePrefix.Length;
            _savedSelStart = -1;
            _savedSelLen = -1;
            return;
        }

        int end = selStart + selLen;

        int firstLine = selStart > 0 ? text.LastIndexOf('\n', selStart - 1) + 1 : 0;

        int lastLineEnd = end < text.Length ? text.IndexOf('\n', end) : -1;
        if (lastLineEnd == -1) lastLineEnd = text.Length;

        string block = text.Substring(firstLine, lastLineEnd - firstLine);
        string[] lines = block.Split('\n');

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(numbered ? $"{i + 1}. " : prefix);
            sb.Append(lines[i]);
        }

        string newBlock = sb.ToString();
        txtEditor.Text = text.Remove(firstLine, lastLineEnd - firstLine).Insert(firstLine, newBlock);

        _isUpdatingSel = true;
        txtEditor.SelectionStart = firstLine;
        txtEditor.SelectionLength = newBlock.Length;
        _isUpdatingSel = false;
        _savedSelStart = firstLine;
        _savedSelLen = newBlock.Length;
    }

    private void BtnH1_Click(object sender, RoutedEventArgs e) { InsertLinePrefix("# "); }
    private void BtnH2_Click(object sender, RoutedEventArgs e) { InsertLinePrefix("## "); }
    private void BtnH3_Click(object sender, RoutedEventArgs e) { InsertLinePrefix("### "); }

    private void BtnUl_Click(object sender, RoutedEventArgs e)
    {
        InsertLinePrefix("- ");
    }

    private void BtnOl_Click(object sender, RoutedEventArgs e)
    {
        InsertLinePrefix("", true);
    }

    private void BtnBq_Click(object sender, RoutedEventArgs e)
    {
        InsertLinePrefix("> ");
    }

    private void BtnCode_Click(object sender, RoutedEventArgs e)
    {
        int start;
        int len;

        if (_savedSelStart >= 0 && _savedSelLen > 0)
        {
            start = _savedSelStart;
            len = _savedSelLen;
        }
        else
        {
            start = txtEditor.SelectionStart;
            len = txtEditor.SelectionLength;
        }

        if (len == 0)
        {
            InsertWrap("`", "`");
            return;
        }

        string text = txtEditor.Text;
        int leading = 0;
        while (leading < len && char.IsWhiteSpace(text[start + leading]))
            leading++;
        int trailing = 0;
        while (trailing < len - leading && char.IsWhiteSpace(text[start + len - 1 - trailing]))
            trailing++;

        int contentStart = start + leading;
        int contentLen = len - leading - trailing;

        if (contentLen == 0)
        {
            _savedSelStart = -1;
            _savedSelLen = -1;
            return;
        }

        if (contentLen < 20)
        {
            InsertWrap("`", "`");
            return;
        }

        string content = text.Substring(contentStart, contentLen);
        txtEditor.Text = text.Remove(contentStart, contentLen)
            .Insert(contentStart, "```\n" + content + "\n```");

        _isUpdatingSel = true;
        txtEditor.SelectionStart = contentStart + 4;
        txtEditor.SelectionLength = contentLen;
        _isUpdatingSel = false;
        _savedSelStart = txtEditor.SelectionStart;
        _savedSelLen = contentLen;
    }

    private void BtnLink_Click(object sender, RoutedEventArgs e)
    {
        string selected = txtEditor.SelectedText;
        if (string.IsNullOrEmpty(selected))
            InsertWrap("[", "](url)");
        else
            InsertWrap("[", "](url)");
    }

    private void BtnImage_Click(object sender, RoutedEventArgs e)
    {
        string selected = txtEditor.SelectedText;
        if (string.IsNullOrEmpty(selected))
            InsertWrap("![", "](url)");
        else
            InsertWrap("![", "](url)");
    }

    private void BtnHr_Click(object sender, RoutedEventArgs e)
    {
        txtEditor.Focus();
        Keyboard.Focus(txtEditor);

        int caret = txtEditor.CaretIndex;
        string text = txtEditor.Text;
        int lineStart = text.LastIndexOf('\n', caret - 1);
        if (lineStart == -1)
            lineStart = 0;
        else
            lineStart++;

        string insert = "\n---\n";
        txtEditor.SelectionStart = lineStart;
        txtEditor.SelectionLength = 0;
        txtEditor.SelectedText = insert;
        txtEditor.CaretIndex = lineStart + insert.Length;
        _savedSelStart = -1;
        _savedSelLen = -1;
    }

    private void BtnColor_Click(object sender, RoutedEventArgs e)
    {
        colorPopup.IsOpen = true;
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string color)
        {
            InsertColor(color);
            colorPopup.IsOpen = false;
        }
    }

    private void InsertColor(string color)
    {
        int start;
        int len;

        if (_savedSelStart >= 0 && _savedSelLen > 0)
        {
            start = _savedSelStart;
            len = _savedSelLen;
        }
        else
        {
            start = txtEditor.SelectionStart;
            len = txtEditor.SelectionLength;
        }

        string prefix = $"<span style=\"color:{color}\">";
        string suffix = "</span>";

        if (len == 0)
        {
            string insert = prefix + "testo" + suffix;
            txtEditor.Text = txtEditor.Text.Insert(start, insert);
            _isUpdatingSel = true;
            txtEditor.SelectionStart = start + prefix.Length;
            txtEditor.SelectionLength = 4;
            _isUpdatingSel = false;
            _savedSelStart = txtEditor.SelectionStart;
            _savedSelLen = 4;
            return;
        }

        string text = txtEditor.Text;
        int leading = 0;
        while (leading < len && char.IsWhiteSpace(text[start + leading]))
            leading++;
        int trailing = 0;
        while (trailing < len - leading && char.IsWhiteSpace(text[start + len - 1 - trailing]))
            trailing++;

        int contentStart = start + leading;
        int contentLen = len - leading - trailing;

        if (contentLen == 0)
        {
            _savedSelStart = -1;
            _savedSelLen = -1;
            return;
        }

        string content = text.Substring(contentStart, contentLen);

        if (contentLen > 0)
        {
            string beforeContent = text.Substring(0, contentStart);
            int spanOpen = beforeContent.LastIndexOf("<span style=\"color:", StringComparison.Ordinal);
            if (spanOpen >= 0)
            {
                int openEnd = beforeContent.IndexOf('>', spanOpen);
                if (openEnd == contentStart - 1)
                {
                    string afterContent = text.Substring(contentStart + contentLen);
                    if (afterContent.StartsWith("</span>"))
                    {
                        string newSpan = $"<span style=\"color:{color}\">";
                        int totalLen = (contentStart + contentLen + 7) - spanOpen;
                        txtEditor.Text = text.Remove(spanOpen, totalLen).Insert(spanOpen, newSpan + content + "</span>");
                        int newSelStart = spanOpen + newSpan.Length;
                        _isUpdatingSel = true;
                        txtEditor.SelectionStart = newSelStart;
                        txtEditor.SelectionLength = contentLen;
                        _isUpdatingSel = false;
                        _savedSelStart = newSelStart;
                        _savedSelLen = contentLen;
                        return;
                    }
                }
            }
        }
        string formatted;
        int newStart;
        int newLen;

        if (content.Contains('\n'))
        {
            string sep = content.Contains("\r\n") ? "\r\n" : "\n";
            var lines = content.Split(new[] { sep }, StringSplitOptions.None);
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append(sep);
                sb.Append(WrapLine(lines[i], prefix, suffix));
            }
            formatted = sb.ToString();
            newStart = contentStart;
            newLen = formatted.Length;
        }
        else
        {
            formatted = prefix + content + suffix;
            newStart = contentStart + prefix.Length;
            newLen = contentLen;
        }

        txtEditor.Text = text.Remove(contentStart, contentLen)
            .Insert(contentStart, formatted);

        _isUpdatingSel = true;
        txtEditor.SelectionStart = newStart;
        txtEditor.SelectionLength = newLen;
        _isUpdatingSel = false;

        _savedSelStart = newStart;
        _savedSelLen = newLen;
    }

    private void BtnToggleEditor_Click(object sender, RoutedEventArgs e)
    {
        _editorVisible = btnToggleEditor.IsChecked == true;
        UpdateContentLayout();
    }

    private void BtnToggleLayout_Click(object sender, RoutedEventArgs e)
    {
        _horizontalLayout = !_horizontalLayout;
        lblLayout.Text = _horizontalLayout ? "\u2195" : "\u2194";
        UpdateContentLayout();
    }

    private void UpdateContentLayout()
    {
        bool editing = _editorVisible;

        borderFormatting.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
        editorPanel.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;

        contentArea.ColumnDefinitions.Clear();
        contentArea.RowDefinitions.Clear();

        Grid.SetRow(editorPanel, 0);
        Grid.SetColumn(editorPanel, 0);
        Grid.SetRow(editorSplitter, 0);
        Grid.SetColumn(editorSplitter, 0);
        Grid.SetRow(printFrame, 0);
        Grid.SetColumn(printFrame, 0);

        if (editing && _horizontalLayout)
        {
            contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            contentArea.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            contentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(editorPanel, 0);
            Grid.SetColumn(editorPanel, 0);
            Grid.SetRow(editorSplitter, 1);
            Grid.SetColumn(editorSplitter, 0);
            Grid.SetRow(printFrame, 2);
            Grid.SetColumn(printFrame, 0);

            editorSplitter.Visibility = Visibility.Visible;
            editorSplitter.Height = 4;
            editorSplitter.Width = double.NaN;
            editorSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            editorSplitter.VerticalAlignment = VerticalAlignment.Center;
            editorSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        }
        else if (editing)
        {
            contentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            contentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(editorPanel, 0);
            Grid.SetColumn(editorPanel, 0);
            Grid.SetRow(editorSplitter, 0);
            Grid.SetColumn(editorSplitter, 1);
            Grid.SetRow(printFrame, 0);
            Grid.SetColumn(printFrame, 2);

            editorSplitter.Visibility = Visibility.Visible;
            editorSplitter.Width = 4;
            editorSplitter.Height = double.NaN;
            editorSplitter.HorizontalAlignment = HorizontalAlignment.Center;
            editorSplitter.VerticalAlignment = VerticalAlignment.Stretch;
            editorSplitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
        }
        else
        {
            contentArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            editorSplitter.Visibility = Visibility.Collapsed;
        }

        if (!editing)
            txtSearch.Focus();
    }

    private async void BtnRefreshPreview_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEditor.Text))
        {
            statusFile.Text = "Nessun contenuto da visualizzare";
            return;
        }
        await UpdatePreview();
        statusFile.Text = "Anteprima aggiornata";
    }

    private async void BtnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        if (!_webViewReady) return;
        webView.ZoomFactor = Math.Min(webView.ZoomFactor + 0.1, 5.0);
        UpdateZoomLabel();
    }

    private async void BtnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        if (!_webViewReady) return;
        webView.ZoomFactor = Math.Max(webView.ZoomFactor - 0.1, 0.25);
        UpdateZoomLabel();
    }

    private async void BtnZoomReset_Click(object sender, RoutedEventArgs e)
    {
        if (!_webViewReady) return;
        webView.ZoomFactor = 1.0;
        UpdateZoomLabel();
    }

    private void UpdateZoomLabel()
    {
        lblZoom.Content = $"{(int)(webView.ZoomFactor * 100)}%";
    }

    private static string FormatHtmlForClipboard(string html)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Version:0.9");
        sb.AppendLine("StartHTML:0000000000");
        sb.AppendLine("EndHTML:0000000000");
        sb.AppendLine("StartFragment:0000000000");
        sb.AppendLine("EndFragment:0000000000");
        sb.AppendLine("StartSelection:0000000000");
        sb.AppendLine("EndSelection:0000000000");
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<HTML>");
        sb.AppendLine("<HEAD>");
        sb.AppendLine("<TITLE>MDReader</TITLE>");
        sb.AppendLine("</HEAD>");
        sb.AppendLine("<BODY>");
        sb.AppendLine("<!--StartFragment-->");
        sb.Append(html);
        sb.AppendLine();
        sb.AppendLine("<!--EndFragment-->");
        sb.AppendLine("</BODY>");
        sb.AppendLine("</HTML>");

        string result = sb.ToString();

        int startHtml = result.IndexOf("<!DOCTYPE html>", StringComparison.Ordinal);
        int endHtml = result.Length;
        int startFrag = result.IndexOf("<!--StartFragment-->", StringComparison.Ordinal) + "<!--StartFragment-->".Length;
        int endFrag = result.IndexOf("<!--EndFragment-->", StringComparison.Ordinal);

        string[] lines = result.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        lines[1] = $"StartHTML:{startHtml:D10}";
        lines[2] = $"EndHTML:{endHtml:D10}";
        lines[3] = $"StartFragment:{startFrag:D10}";
        lines[4] = $"EndFragment:{endFrag:D10}";
        lines[5] = $"StartSelection:{startFrag:D10}";
        lines[6] = $"EndSelection:{endFrag:D10}";

        return string.Join("\r\n", lines);
    }
}
