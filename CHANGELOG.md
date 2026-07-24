# Changelog

## Versione 3.2 (2026-07-24)

### Correzioni grafica UI modalità scura
- Bordi superiore e inferiore per le barre TOC, Editor e Anteprima ora uniformi
- Colore bordo barre modificato (#505050 in scuro) per leggibilità su sfondo toolbar #2d2d2d
- Separatori verticali (GridSplitter) uniformati tra TOC/Editor e Editor/Anteprima
- Allineamento splitter editor corretto (HorizontalAlignment.Stretch)
- Toolbar principale ed editor: spaziatura orizzontale pulsanti aumentata (Margin 6,0)
- Rimosso padding bottom dalle toolbar che comprimeva i pulsanti verticalmente

### Miglioramenti pulsante Aggiorna
- Rimossa icona freccia, solo testo "Aggiorna"
- Sfondo con gradiente giallo canarino (#FFFACD)
- Altezza pulsante aumentata (Padding 10,1, Height 22)
- Trigger mouseover con giallo pieno acceso (#FFFFD700, tinta unica)
- ControlTemplate personalizzato senza override azzurro al mouseover
- Colore testo migliorato (#8B6912)
- Font ridotto a 8pt per non superare i bordi del pulsante

### Miglioramenti minori
- Rimosso TextDecorations.Underline dal pulsante Colore testo
- Colore pulsante Colore testo cambiato da Red a #E74C3C per leggibilità
- Altezza barre header aumentata da 28 a 32px
- Colore sfondo form/toolbar in scuro: #2d2d2d

## Versione 3.1 (2026-07-23)

### Aggiunte
- **Pulsante Stampa:** aggiunto nella toolbar principale per stampare l'anteprima

### Correzioni grafica
- Miglioramenti UI modalità scura
- Miglioramento leggibilità pulsanti
- Sfondo form e toolbar ora coerenti in modalità scura
- Barra "Anteprima" e "Indice" allineate visivamente

### Modifiche stampa
- La stampa non viene più influenzata dalla modalità scura (sfondo e testo sempre corretti)
- La barra "Anteprima" e il pulsante Aggiorna vengono nascosti durante la stampa
- La WebView resta in tema scuro durante l'anteprima di stampa
