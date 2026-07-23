# MDReader V3 — Riepilogo progetto

## Stack
- **Framework**: WPF (.NET 10, `net10.0-windows`)
- **Pacchetti**: Markdig 0.40.0, Microsoft.Web.WebView2 1.0.3065.39
- **Icona app**: `File_MD.ico`
- **Target**: framework-dependent (richiede .NET 10 Desktop Runtime)

## Funzionalità

### Toolbar principale (riga 0)
| Pulsante | Scorciatoia | Dettagli |
|---|---|---|
| **Apri** | Ctrl+O | Apri file `.md`, `.markdown`, `.txt` |
| **Nuovo** | Ctrl+N | Crea `%TEMP%\MDReader\temporaneo_yyyyMMdd_HHmmss.md`, apre editor |
| **Guida** | — | Apre `Guida.md` in una nuova istanza |
| **Salva** | Ctrl+S | Se file temporaneo, reindirizza a Salva con nome |
| **Salva come** | Ctrl+Maiusc+S | Sceglie destinazione (.md o .txt) |
| **Editor** | — | Toggle split editor/preview (default OFF) |
| **↔/↕** | — | Commuta layout verticale / orizzontale |
| **Cerca** | Ctrl+F | Evidenzia occorrenze, F3 / Maiusc+F3 |
| **Scuro/Chiaro** | — | Dark mode persistente |
| **Zoom − / 100% / + / 1:1** | — | WebView2.ZoomFactor (0.25–5.0, step 0.1) |

### Toolbar formattazione (riga 1, visibile solo con Editor attivo)
| Pulsante | Tag inserito | Note |
|---|---|---|
| **B** (Grassetto) | `**testo**` | Ctrl+B |
| **I** (Corsivo) | `_testo_` | Ctrl+I |
| **U** (Sottolineato) | `<u>testo</u>` | — |
| **S** (Barrato) | `~~testo~~` | — |
| **H1** | `# ` | Prefisso per riga |
| **H2** | `## ` | Prefisso per riga |
| **H3** | `### ` | Prefisso per riga |
| **•** (UL) | `- ` | Prefisso per riga |
| **1.** (OL) | `1. 2. 3. ...` | Numerazione progressiva |
| **>** (BQ) | `> ` | Prefisso per riga |
| **</>** (Code) | `` `codice` `` o `` ```codice``` `` | Breve o multi-riga |
| **Link** | `[testo](url)` | — |
| **Immagine** | `![testo](url)` | — |
| **—** (HR) | `\n---\n` | — |
| **Colore** | `<span style="color:NOME">testo</span>` | Popup 16 colori |

### Editor
- **TextBox** WPF (Consolas 14pt, AcceptsReturn/AcceptsTab)
- **Selezione persistente**: non si perde tra click multipli su pulsanti formattazione
- **Trim selezione**: spazi iniziali/finali rimossi prima di applicare tag
- **Manipolazione testo per indice** (`Remove/Insert`)
- **Liste multi-riga**: `InsertLinePrefix` opera su tutte le righe; OL numerazione progressiva
- **Wrap multi-riga**: applica tag a ogni riga, preserva prefissi markdown (`-`, `*`, `>`, `#`, `\d+.`) fuori dai tag
- **Toggle tag**: se il testo è già wrappato, premi di nuovo per rimuovere il tag
- **Rilevamento annidato**: trova tag outer anche con formattazione intermedia (`**__text__**`)
- **Cursore senza selezione**: inserisce tag e posiziona cursore a fine tag

### Anteprima
- **Markdig** → HTML → WebView2
- **Barra intestazione**: "Anteprima" con pulsante **Aggiorna** (stile uniforme a TOC/Editor)
- **JS embedded**: ricerca evidenzia/scroll, dark mode, scrollToId
- **Zoom**: WebView2.ZoomFactor
- **Refresh manuale**: solo dal pulsante Aggiorna nella barra anteprima

### TOC (Indice)
- Colonna sinistra fissa (260px), **sempre visibile** (nessun toggle)
- Elenco intestazioni estratte dal Markdown
- Click → scroll all'ancora nell'anteprima

### Gestione file .txt
- Apertura/drag di `.txt` → contenuto convertito in Markdown (ogni riga diventa `<br>` con due spazi finali)
- Salvato su temp `tempFromTXT_*.md`, l'app lavora internamente in puro Markdown
- Al salvataggio l'utente sceglie:
  - **.md**: apre Salva con nome per scegliere destinazione
  - **.txt**: riscrive il file originale con il contenuto corrente
- Filtri accettati in drag/drop e apertura: `.md`, `.markdown`, `.mdown`, `.mdwn`, `.txt`

### Trascinamento file (Drag & Drop)
- `AllowDrop="True"` sulla Window
- `AllowExternalDrop="False"` sul WebView2 (blocca navigazione nativa a file://)
- **Nessun documento aperto**: overlay verde su tutta l'area, drop → carica nell'istanza corrente
- **Documento aperto**: drag funziona solo sull'area **TOC** (overlay verde con `+`)
- **Drop con documento aperto**: sempre nuova istanza (`Process.Start`)
- **File non supportati** (es. immagini, PDF): `DragDropEffects.None`, nessun overlay

### Multi-istanza
- Ogni file si apre in una finestra indipendente
- File temporanei con timestamp univoco per evitare conflitti

## Layout
- Griglia 4 righe: toolbar principale | toolbar formattazione | area contenuto | status bar
- Area contenuto: TOC (sempre visibile) | gridSplitter | contentArea (editor + gridSplitter + preview)
- Layout editor verticale/orizzontale commutabile
- Barre intestazione uniformi: TOC "Indice", Editor "Editor", Preview "Anteprima" (28px, `#f0f0f0`, semibold 13px)

## Installer (Minimal)
- **Target**: .NET Framework 4.8.1 (Windows Forms + WPF)
- **Framework-dependent**: richiede .NET 10 Desktop Runtime (rilevamento automatico)
- Cerca i file dell'app in una cartella `publish/` (risalendo l'albero directory)
- Rileva installazione precedente tramite `MDReader-install-path.txt`:
  - Se presente e valido: default path = percorso installato, pulsante = **Aggiorna**
  - Altrimenti: default path = `%LOCALAPPDATA%\Programs\MDReader`, pulsante = **Installa**
- Installa: copia file, registra associazioni `.md`/`.markdown`, crea Start Menu shortcut
- Disinstalla: rimuove associazioni, shortcut, cartella
- Log in `install-log.txt`

## V3 vs V2
- V3 aggiunge editing completo (toolbar formattazione, split editor/preview, TOC colonna)
- V2 è read-only
- V3 include Guida.md e pulsante Guida

## Possibili modifiche future
- **Self-contained**: pubblicare con `--self-contained true` per rimuovere dipendenza da .NET Runtime
- **Autocompletamento markdown**: suggerimenti durante la digitazione
- **Tabella formattazione**: pulsante per inserire tabelle
- **Controllo ortografico**: integrazione con dizionario
- **Multi-cursore**: editing simultaneo su più punti
- **Snippet**: blocchi di testo predefiniti
- **Temi personalizzati**: supporto per temi CSS utente

## Compilazione

### App (framework-dependent)
```powershell
dotnet publish -c Release MDReader_V3.csproj
```

### App (self-contained)
```powershell
dotnet publish -c Release -r win-x64 --self-contained true MDReader_V3.csproj
```

### Installer minimal
```powershell
dotnet build -c Release installer\MDReader.Installer.Minimal\MDReader.Installer.Minimal.csproj
```

### Preparazione distribuzione
1. Pubblica l'app
2. Copia `Guida.md` nella cartella `publish/`
3. Copia `MDReader.Installer.exe` + `setup.ico` + `publish/` in una cartella di distribuzione
