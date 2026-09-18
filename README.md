# Kameraüberwachung

Die Anwendung ist eine lokale ASP.NET-Core-Webanwendung. Im Browser kann eine
verfügbare Kamera ausgewählt und deren Livebild angezeigt werden. Die
Bewegungserkennung vergleicht aufeinanderfolgende Videobilder. Bei Bewegung
wird höchstens alle zwei Sekunden ein JPEG-Bild an das Backend übertragen und
im Ordner `SurveillanceBackend/captures` gespeichert.

## Starten

```bash
dotnet run --project SurveillanceBackend
```

Danach die in der Konsole angegebene URL im Browser öffnen. Für den
Kamerazugriff muss die Seite über `https://localhost` oder `localhost`
aufgerufen werden. Beim ersten Start muss der Browser den Kamerazugriff
erlauben. Klicke anschließend auf **Kamera starten**. Falls vorher einmal
„Blockieren“ ausgewählt wurde, muss die Berechtigung über das Kamera- oder
Schlosssymbol links neben der URL wieder auf „Zulassen“ gesetzt werden.
