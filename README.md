# MDReader V3

Visualizzatore ed editor Markdown per Windows con anteprima live WebView2, TOC navigabile, editing con toolbar formattazione, supporto .txt con conversione Markdown, drag & drop, multi-istanza, modalità scura. Built con WPF + .NET 10.

## Caratteristiche

- **Anteprima live** con Markdig → HTML → WebView2
- **Editor integrato** con toolbar formattazione (grassetto, corsivo, heading, liste, codice, link, immagini, colori, …)
- **Indice (TOC)** sempre visibile, navigazione cliccabile
- **Drag & drop** file .md / .txt sull'Indice per apertura in nuova istanza
- **Multi-istanza**: ogni file si apre in finestra indipendente
- **Supporto .txt**: apertura → conversione automatica in Markdown; al salvataggio scegli se mantenere .txt o salvare come .md
- **Modalità scura** persistente
- **Zoom** anteprima (0.25× – 5×)
- **Ricerca** con evidenziazione e navigazione (F3 / Maiusc+F3)
- **Layout** editor verticale / orizzontale commutabile

## Requisiti

- Windows 10/11 (x64, x86, ARM64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

## Installazione

Esegui `MDReader.Installer.exe`. L'installer:
- Rileva il runtime .NET 10 (se mancante offre il download)
- Copia i file in `%LOCALAPPDATA%\Programs\MDReader`
- Registra le associazioni per .md / .markdown
- Crea collegamento nel menu Start
- Alla prossima esecuzione rileva l'installazione e mostra **Aggiorna** invece di **Installa**

## Compilazione

### Minimal (framework-dependent)
```powershell
# App
dotnet publish -c Release v3_Distribution\MDReader_V3\MDReader_V3.csproj

# Installer (controlla presenza .NET Runtime)
dotnet build -c Release v3_Distribution\installer\MDReader.Installer.Minimal\MDReader.Installer.Minimal.csproj
```

### Full (self-contained)
```powershell
# App (include runtime .NET 10)
dotnet publish -c Release -r win-x64 --self-contained true v3_Distribution\MDReader_V3\MDReader_V3.csproj

# Installer (senza controllo runtime) – prima modifica il sorgente:
#   in installer\MDReader.Installer.Minimal\MainWindow.xaml.cs
#   rimuovi il blocco if (!IsDotNet10Installed()) { ... }
dotnet build -c Release v3_Distribution\installer\MDReader.Installer.Minimal\MDReader.Installer.Minimal.csproj

# Copia Guida.md nella cartella publish/
Copy-Item v3_Distribution\MDReader_V3\Guida.md bin\Release\net10.0-windows\publish\

# Raccogli tutto per la distribuzione:
#   MDReader.Installer.exe  +  setup.ico  +  publish\  →  cartella di distribuzione
```

## Versioni

### Minimal (consigliata)
- Framework-dependent (richiede .NET 10 Runtime ~60 MB separati)
- Installer leggero (~28 KB)
- Aggiornamenti più veloci (solo DLL app)

### Full (self-contained)
- Include il runtime .NET 10 nell'installazione (~60 MB aggiuntivi)
- Nessuna dipendenza esterna
- File unico `publish/` con tutto il necessario
- Per generarla: `dotnet publish -c Release -r win-x64 --self-contained true`
- **Nota**: l'installer minimal controlla la presenza del runtime .NET. Per la versione full, commenta o rimuovi la chiamata a `IsDotNet10Installed()` in `installer\MDReader.Installer.Minimal\MainWindow.xaml.cs` (blocco `if (!IsDotNet10Installed())`), oppure usa direttamente `publish\MDReader.exe` senza installer.

## Struttura progetto

```
v3_Distribution/
├── MDReader_V3/                         # App WPF
│   ├── MainWindow.xaml(.cs)             # UI principale
│   ├── Services/
│   │   └── MarkdownService.cs           # Markdig → HTML
│   └── Guida.md                         # Guida utente
├── installer/
│   └── MDReader.Installer.Minimal/      # Installer WPF
└── RIEPILOGO.md                         # Documentazione progetto
```

## Stack

| Componente | Tecnologia |
|---|---|
| Framework | WPF, .NET 10 |
| Markdown | Markdig 0.40.0 |
| WebView | Microsoft.Web.WebView2 1.0.3065.39 |
| Installer | WPF, .NET Framework 4.8.1 |
