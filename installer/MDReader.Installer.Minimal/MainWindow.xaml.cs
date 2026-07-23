using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MDReader.Installer;

public partial class MainWindow : Window
{
    private const string AppName = "MDReader";
    private const string ProgId = "MDReader.MD";
    private const string TargetExe = "MDReader.exe";
    private const string PublishDir = "publish";
    private const string TrackingFileName = "MDReader-install-path.txt";

    private string? _sourceDir;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        string? installedPath = ReadTrackingFile();
        if (installedPath is not null && Directory.Exists(installedPath))
        {
            txtPath.Text = installedPath;
            btnInstalla.Content = "Aggiorna";
        }
        else
        {
            txtPath.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                AppName);
        }

        _sourceDir = ResolveSourceDir();
        txtSource.Text = _sourceDir is not null
            ? $"Origine file: {_sourceDir}"
            : $"Attenzione: cartella '{PublishDir}' non trovata. Copia il programma nella directory del progetto.";
    }

    private static string? ResolveSourceDir()
    {
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (exeDir is null) return null;

        var dir = new DirectoryInfo(exeDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, PublishDir);
            if (File.Exists(Path.Combine(candidate, TargetExe)))
                return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    private void btnSfoglia_Click(object sender, RoutedEventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Scegli la cartella di destinazione per MDReader";
        dlg.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            txtPath.Text = Path.Combine(dlg.SelectedPath, "A79_MDReader");
    }

    private void btnInstalla_Click(object sender, RoutedEventArgs e)
    {
        var destDir = txtPath.Text.Trim();
        if (string.IsNullOrWhiteSpace(destDir))
        {
            System.Windows.MessageBox.Show(
                "Seleziona una cartella di destinazione.", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!destDir.EndsWith("A79_MDReader", StringComparison.OrdinalIgnoreCase))
            destDir = Path.Combine(destDir, "A79_MDReader");

        if (_sourceDir is null)
        {
            System.Windows.MessageBox.Show(
                $"Cartella '{PublishDir}' non trovata. Assicurati che l'installer sia nella directory del progetto.",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Validate path
        try
        {
            var testDir = Path.GetFullPath(destDir);
            if (!Directory.Exists(testDir))
                Directory.CreateDirectory(testDir);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Percorso non valido:\n{ex.Message}", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Check if already installed
        var installedExeCheck = Path.Combine(destDir, TargetExe);
        if (File.Exists(installedExeCheck))
        {
            Log($"Installazione esistente trovata in {destDir}");
            var procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(TargetExe));
            if (procs.Length > 0)
            {
                Log($"ERRORE: {TargetExe} in esecuzione ({procs.Length} processi)");
                System.Windows.MessageBox.Show(
                    $"MDReader è attualmente in esecuzione.\n\nChiudilo prima di reinstallare.",
                    "Processo in esecuzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                foreach (var p in procs) p.Dispose();
                return;
            }
            foreach (var p in procs) p.Dispose();
            Log("Nessun processo attivo. Sovrascrittura file...");
        }

        btnInstalla.IsEnabled = false;
        Log($"=== INSTALL ===");
        Log($"Destinazione: {destDir}");
        Log($"Source dir: {_sourceDir ?? "(null)"}");
        Log($"Log path: {LogPath}");
        try
        {
            if (!IsDotNet10Installed())
            {
                Log("NET 10 Runtime NON trovato");
                var result = System.Windows.MessageBox.Show(
                    ".NET 10 Desktop Runtime non trovato!\n\n" +
                    "Questa applicazione richiede .NET 10 per funzionare.\n\n" +
                    "Vuoi scaricare il runtime ora dal sito Microsoft?",
                    ".NET Runtime mancante",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);

                if (result == MessageBoxResult.Yes)
                    Process.Start("https://dotnet.microsoft.com/en-us/download/dotnet/10.0");
                else if (result == MessageBoxResult.No)
                    System.Windows.MessageBox.Show(
                        "Scarica la versione self-contained (senza dipendenze esterne)\n" +
                        "dalla cartella MDReader_Installer e usa MDReader.Installer.exe.",
                        "Usa versione self-contained", MessageBoxButton.OK, MessageBoxImage.Information);
                Log("Installazione annullata (NET Runtime mancante)");
                return;
            }

            Log("NET 10 Runtime trovato. Avvio installazione...");
            Install(destDir);
            Log("Installazione completata con successo");
            System.Windows.MessageBox.Show(
                $"Installazione completata!\n\nCartella: {destDir}\nStart Menu: {AppName}",
                AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            Log("MsgBox conferma mostrato");
        }
        catch (UnauthorizedAccessException)
        {
            Log($"ERRORE Accesso negato: {destDir}");
            System.Windows.MessageBox.Show(
                "Permessi insufficienti per scrivere in questa cartella.\n\n" +
                "Scegli una cartella nelle tue aree utente (es. C:\\Users\\...) oppure " +
                "avvia l'installatore come amministratore (tasto destro → Esegui come amministratore).",
                "Accesso negato", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Log($"ERRORE: {ex.GetType().Name}: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Errore durante l'installazione:\n{ex.GetType().Name}: {ex.Message}",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnInstalla.IsEnabled = true;
        }
    }

    private void btnEsci_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static bool IsDotNet10Installed()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var runtimeDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.NETCore.App");
        if (!Directory.Exists(runtimeDir))
        {
            // also check ProgramFiles(x86)
            programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            runtimeDir = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.NETCore.App");
            if (!Directory.Exists(runtimeDir))
                return false;
        }

        foreach (var dir in Directory.EnumerateDirectories(runtimeDir))
        {
            if (Path.GetFileName(dir).StartsWith("10."))
                return true;
        }
        return false;
    }

    private string InstallerDir =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private string TrackingFilePath =>
        Path.Combine(InstallerDir, TrackingFileName);

    private string LogPath =>
        Path.Combine(InstallerDir, "install-log.txt");

    private void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {msg}\n"); }
        catch { }
    }

    private void btnDisinstalla_Click(object sender, RoutedEventArgs e)
    {
        var installedPath = ReadTrackingFile();
        if (installedPath is null)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.Description = "Seleziona la cartella dove è installato MDReader";
            dlg.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            installedPath = dlg.SelectedPath;
        }

        var result = System.Windows.MessageBox.Show(
            $"Rimuovere MDReader da:\n{installedPath}\n\n" +
            "Verranno eliminate: cartella, associazioni file e collegamento Start Menu.",
            "Conferma disinstallazione",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
            return;

        btnDisinstalla.IsEnabled = false;
        try
        {
            Uninstall(installedPath);
            System.Windows.MessageBox.Show(
                "Disinstallazione completata.", AppName,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Errore durante la disinstallazione:\n{ex.Message}",
                "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            btnDisinstalla.IsEnabled = true;
        }
    }

    private void SaveTrackingFile(string installPath)
    {
        try
        {
            var path = TrackingFilePath;
            Log($"SaveTrackingFile: path={path}");
            var dir = Path.GetDirectoryName(path);
            if (dir is not null)
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(path, installPath);
                Log($"SaveTrackingFile: scritto '{installPath}' in {path}");
            }
            else
            {
                Log($"SaveTrackingFile: dir NULL per path={path}");
            }
        }
        catch (Exception ex)
        {
            Log($"SaveTrackingFile ERRORE: {ex.Message}");
        }
    }

    private string? ReadTrackingFile()
    {
        try
        {
            var path = TrackingFilePath;
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
        }
        catch { }
        return null;
    }

    private void Uninstall(string targetDir)
    {
        RemoveAssociations();
        RemoveStartMenuShortcut();
        try
        {
            var path = TrackingFilePath;
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, recursive: true);
    }

    private void ClearUserChoice(string ext)
    {
        Log($"  ClearUserChoice({ext}): tentativo...");
        var keyPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{ext}\UserChoice";
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true))
            {
                if (key is not null)
                {
                    key.DeleteValue("Hash", throwOnMissingValue: false);
                    key.SetValue("Progid", ProgId, RegistryValueKind.String);
                    Log($"  ClearUserChoice({ext}): ProgId sovrascritto, Hash rimosso");
                }
                else
                {
                    Log($"  ClearUserChoice({ext}): UserChoice non esiste");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"  ClearUserChoice({ext}) ERRORE scrittura: {ex.GetType().Name}: {ex.Message}");
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
                Log($"  ClearUserChoice({ext}): eliminato con DeleteSubKeyTree");
            }
            catch (Exception ex2)
            {
                Log($"  ClearUserChoice({ext}) DeleteSubKeyTree fallito: {ex2.Message}");
            }
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private void NotifyAssocChanged()
    {
        Log("  NotifyAssocChanged: SHChangeNotify...");
        try { SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero); Log("  NotifyAssocChanged: OK"); }
        catch (Exception ex) { Log($"  NotifyAssocChanged ERRORE: {ex.Message}"); }
    }

    private static void RemoveAssociations()
    {
        // Remove ProgID
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + ProgId, throwOnMissingSubKey: false); }
        catch { }

        // Remove .md -> ProgId reference
        try
        {
            using var mdKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.md", writable: true);
            if (mdKey is not null)
            {
                using var openWith = mdKey.OpenSubKey("OpenWithProgids", writable: true);
                openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
                if (mdKey.GetValue("") is string val && val == ProgId)
                    mdKey.DeleteValue("");
            }
        }
        catch { }

        // Remove .markdown -> ProgId reference
        try
        {
            using var mdkKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\.markdown", writable: true);
            if (mdkKey is not null)
            {
                using var openWith = mdkKey.OpenSubKey("OpenWithProgids", writable: true);
                openWith?.DeleteValue(ProgId, throwOnMissingValue: false);
                if (mdkKey.GetValue("") is string val && val == ProgId)
                    mdkKey.DeleteValue("");
            }
        }
        catch { }

        // Remove RegisteredApplications entry
        try
        {
            using var regApps = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", writable: true);
            regApps?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch { }

        // Remove Capabilities
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\" + AppName, throwOnMissingSubKey: false); }
        catch { }
    }

    private static void RemoveStartMenuShortcut()
    {
        try
        {
            var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            var shortcutDir = Path.Combine(startMenu, "Programs", AppName);
            var shortcutPath = Path.Combine(shortcutDir, $"{AppName}.lnk");
            if (File.Exists(shortcutPath))
                File.Delete(shortcutPath);
            if (Directory.Exists(shortcutDir) && Directory.GetFiles(shortcutDir).Length == 0)
                Directory.Delete(shortcutDir, recursive: false);
        }
        catch { }
    }

    private void Install(string destDir)
    {
        Log("Install(): creazione cartella destinazione...");
        Directory.CreateDirectory(destDir);

        int fileCount = 0;
        foreach (var srcPath in Directory.EnumerateFiles(_sourceDir!, "*", SearchOption.AllDirectories))
        {
            var relPath = GetRelativePath(_sourceDir!, srcPath);
            var dstPath = Path.Combine(destDir, relPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath)!);
            File.Copy(srcPath, dstPath, overwrite: true);
            fileCount++;
        }
        Log($"Install(): copiati {fileCount} file");

        var installedExe = Path.Combine(destDir, TargetExe);
        if (!File.Exists(installedExe))
        {
            Log($"ERRORE: {TargetExe} non trovato in {destDir}");
            throw new FileNotFoundException($"'{TargetExe}' non trovato dopo la copia.", installedExe);
        }
        Log($"Install(): {TargetExe} verificato -> {installedExe}");

        if (chkAssocia.IsChecked == true)
        {
            Log("Install(): registrazione associazioni...");
            RegisterAssociations(installedExe);
            ClearUserChoice(".md");
            ClearUserChoice(".markdown");
            NotifyAssocChanged();
            Log("Install(): associazioni completate");
        }

        Log("Install(): creazione shortcut Start Menu...");
        CreateStartMenuShortcut(installedExe);

        Log("Install(): salvataggio tracking file...");
        SaveTrackingFile(destDir);
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        if (!basePath.EndsWith("\\"))
            basePath += "\\";

        if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(basePath.Length);

        // fallback: just return the filename
        return Path.GetFileName(fullPath);
    }

    private void RegisterAssociations(string exePath)
    {
        Log("  RegisterAssociations: apertura Software\\Classes...");
        var classes = Registry.CurrentUser.OpenSubKey("Software\\Classes", writable: true)
                      ?? Registry.CurrentUser.CreateSubKey("Software\\Classes");
        Log("  RegisterAssociations: classes aperta");

        try
        {
            Log("  RegisterAssociations: creazione ProgID...");
            using (var progIdKey = classes!.CreateSubKey(ProgId))
            {
                progIdKey.SetValue("", "Documento Markdown");
                using var iconKey = progIdKey.CreateSubKey("DefaultIcon");
                iconKey.SetValue("", $"{exePath},0");
                using var commandKey = progIdKey.CreateSubKey("shell\\open\\command");
                commandKey.SetValue("", $"\"{exePath}\" \"%1\"");
            }
            Log("  RegisterAssociations: ProgID OK");
        }
        catch (Exception ex) { Log($"  ERRORE ProgID: {ex.Message}"); }

        try
        {
            Log("  RegisterAssociations: .md...");
            using (var mdKey = classes.CreateSubKey(".md"))
            {
                mdKey.SetValue("", ProgId);
                using var mdProgIds = mdKey.CreateSubKey("OpenWithProgids");
                mdProgIds.SetValue(ProgId, "");
            }
            Log("  RegisterAssociations: .md OK");
        }
        catch (Exception ex) { Log($"  ERRORE .md: {ex.Message}"); }

        try
        {
            Log("  RegisterAssociations: .markdown...");
            using (var mdkKey = classes.CreateSubKey(".markdown"))
            {
                mdkKey.SetValue("", ProgId);
                using var mdkProgIds = mdkKey.CreateSubKey("OpenWithProgids");
                mdkProgIds.SetValue(ProgId, "");
            }
            Log("  RegisterAssociations: .markdown OK");
        }
        catch (Exception ex) { Log($"  ERRORE .markdown: {ex.Message}"); }

        try
        {
            Log("  RegisterAssociations: RegisteredApplications...");
            using (var regApps = Registry.CurrentUser.CreateSubKey("Software\\RegisteredApplications"))
                regApps.SetValue(AppName, $"Software\\{AppName}\\Capabilities");
            Log("  RegisterAssociations: RegisteredApplications OK");

            Log("  RegisterAssociations: Capabilities...");
            using (var caps = Registry.CurrentUser.CreateSubKey($"Software\\{AppName}\\Capabilities"))
            {
                caps.SetValue("ApplicationName", AppName);
                using var fileAssoc = caps.CreateSubKey("FileAssociations");
                fileAssoc.SetValue(".md", ProgId);
                fileAssoc.SetValue(".markdown", ProgId);
            }
            Log("  RegisterAssociations: Capabilities OK");
        }
        catch (Exception ex) { Log($"  ERRORE RegisteredApplications/Capabilities: {ex.Message}"); }
    }

    private static void CreateStartMenuShortcut(string targetPath)
    {
        var startMenu = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        var shortcutDir = Path.Combine(startMenu, "Programs", AppName);
        Directory.CreateDirectory(shortcutDir);

        var shortcutPath = Path.Combine(shortcutDir, $"{AppName}.lnk");

        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
            throw new InvalidOperationException("Impossibile creare il collegamento (WScript.Shell non disponibile).");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
        shortcut.Description = "MDReader – Visualizzatore Markdown";
        shortcut.Save();
    }
}
