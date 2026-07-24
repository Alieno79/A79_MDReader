# MDReader — Guida rapida

## Aprire un file
- **Pulsante Apri** (Ctrl+O): seleziona un file `.md`, `.markdown` o `.txt`
- **Drag & drop**: trascina un file supportato sull'**Indice** (colonna sinistra) se un documento è già aperto, oppure su qualsiasi punto della finestra se nessun documento è aperto
- **Doppio click da Explorer**: se il programma è associato ai file `.md`
- **Multi istanza**: ogni file si apre in una nuova finestra indipendente

## Nuovo file
- **Pulsante Nuovo** (Ctrl+N): crea un file temporaneo con nome univoco (`temporaneo_YYYYMMDD_HHMMSS.md`)
- Scrivi il contenuto, poi **Salva** per scegliere la destinazione finale
- Il file temporaneo viene eliminato automaticamente dopo il salvataggio

## Salvare
- **Salva** (Ctrl+S): se file temporaneo, apre "Salva con nome"
- **Salva come** (Ctrl+Maiusc+S): scegli una nuova destinazione (.md o .txt)

### File .txt
All'apertura di un `.txt`, il contenuto viene convertito internamente in Markdown e salvato su un file temporaneo `.md`. Al primo **Salva** l'applicazione chiede:
- **Sì** → salva come `.md` (apre finestra per scegliere destinazione)
- **No** → salva come `.txt` (sostituisce il file originale)
- **Annulla** → non salva

## Editor
- **Pulsante Editor**: mostra/nasconde il pannello di editing
- **Layout verticale/orizzontale** (↔/↕): editor sopra/sotto o affiancato all'anteprima
- **Formattazione**: seleziona il testo e premi il pulsante corrispondente
- **Toggle tag**: se il testo è già formattato, premi di nuovo per rimuovere il tag
- **Selezione multi-riga**: applica la formattazione a ogni riga della selezione

## Toolbar formattazione (da sinistra)
| Pulsante | Scorciatoia | Effetto |
|---|---|---|
| **B** | Ctrl+B | Grassetto `**testo**` |
| **I** | Ctrl+I | Corsivo `_testo_` |
| **U** | — | Sottolineato `<u>testo</u>` |
| **S** | — | Barrato `~~testo~~` |
| **H1/H2/H3** | — | Heading `# ` / `## ` / `### ` |
| **•** | — | Elenco puntato `- ` |
| **1.** | — | Elenco numerato `1. 2. ...` |
| **>** | — | Citazione `> ` |
| **</>** | — | Codice `` `codice` `` |
| **Link** | — | `[testo](url)` |
| **Immagine** | — | `![testo](url)` |
| **—** | — | Linea orizzontale |
| **Colore** | — | 16 colori, `<span style="color:...">` |

## Ricerca
- **Ctrl+F**: attiva la barra di ricerca
- **F3**: occorrenza successiva
- **Maiusc+F3**: occorrenza precedente
- **Esc**: cancella la ricerca

## Zoom anteprima
- **+** / **−**: zoom avanti/indietro (0.25x – 5x)
- **1:1**: ripristina zoom 100%

## Pulsante Aggiorna anteprima
Nella barra "Anteprima" il pulsante di aggiornamento usa solo testo "Aggiorna" con sfondo giallo canarino. Al passaggio del mouse diventa giallo acceso.

## Indice (TOC)
- Colonna sinistra **sempre visibile** con l'elenco delle intestazioni
- **Click su un'intestazione**: salta a quella sezione nell'anteprima
- **Trascina un file .md o .txt sull'Indice** per aprirlo in una nuova istanza (solo con documento già aperto)

## Toolbar
- La toolbar principale (a sinistra) e quella di formattazione (a sinistra quando l'editor è visibile) ora hanno la stessa altezza e spaziatura orizzontale tra i pulsanti
- Sfondo toolbar in modalità scura: #2d2d2d

## Modalità scura

## Tasti rapidi
| Tasto | Azione |
|---|---|
| Ctrl+N | Nuovo file |
| Ctrl+O | Apri file |
| Ctrl+S | Salva |
| Ctrl+Maiusc+S | Salva con nome |
| Ctrl+B | Grassetto |
| Ctrl+I | Corsivo |
| Ctrl+F | Cerca |
| F3 | Occorrenza successiva |
| Maiusc+F3 | Occorrenza precedente |

## Note
- L'anteprima non si aggiorna automaticamente: premi **Aggiorna** nella barra "Anteprima" per vedere le modifiche
- I tag di formattazione vengono applicati per ogni riga in caso di selezione multi-riga
- I prefissi di lista (`- `, `* `, `> `, `# `) restano sempre fuori dai tag di formattazione
- I file `.txt` vengono convertiti in Markdown all'apertura; la formattazione Markdown è pienamente supportata anche su questi file
