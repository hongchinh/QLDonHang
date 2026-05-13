Place the three Roboto font files here so the Quotation PDF renderer can
embed them and render Vietnamese diacritics correctly:

  Roboto-Regular.ttf
  Roboto-Bold.ttf
  Roboto-Italic.ttf

Source: https://fonts.google.com/specimen/Roboto  (Apache-2.0)

The .csproj globs `Pdf\Fonts\*.ttf` as EmbeddedResource. After dropping
the three TTFs here and rebuilding Infrastructure, no further changes
are needed -- QuotationPdfRenderer registers them at startup.

Without the files present, PDF rendering falls back to the QuestPDF
default font (Lato) which does NOT cover the full Vietnamese diacritic
set. The endpoint still returns a PDF; only diacritics may be missing
or rendered as boxes.
