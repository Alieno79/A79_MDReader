# MDReader V3 — Riepilogo

MDReader è un editor Markdown desktop con anteprima in tempo reale, sviluppato in WPF (.NET 10).

## Funzionalità principali
- Apertura e creazione di file Markdown (.md, .txt)
- Editor di testo accanto all'anteprima renderizzata via WebView2
- Indice (TOC) navigabile con scorrimento alle sezioni
- Toolbar di formattazione con toggle tag (grassetto, corsivo, ecc.)
- Ricerca testo (Ctrl+F) con navigazione occorrenze
- Zoom anteprima (0.25x – 5x)
- Stampa dell'anteprima
- Trascinamento file (drag & drop) per aprire documenti
- Modalità scura con impostazione persistente

## Interfaccia
Finestra suddivisa in tre aree:
1. **Indice** (colonna sinistra, sempre visibile)
2. **Editor** (nascosto di default, attivabile dalla toolbar)
3. **Anteprima** (area principale con rendering WebView2)

## Tecnologie
- WPF (.NET 10)
- WebView2 per il rendering HTML dell'anteprima
- Markdown parsing personalizzato (MarkdownService)
- Icone Segoe MDL2 Assets nella toolbar