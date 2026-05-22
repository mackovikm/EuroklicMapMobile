# .NET MAUI Android – poznámky a opravy

## MauiProgram.cs – správný vzor inicializace

**SPRÁVNĚ:**
```csharp
builder.UseMauiApp<App>();
```
**ŠPATNĚ** (nevztahuje App třídu na aplikaci):
```csharp
builder.UseMaui();   // ← chyba, App třída není zaregistrována
```

## MainActivity.cs – splash theme

**SPRÁVNĚ:**
```csharp
[Activity(Theme = "@style/Maui.SplashTheme", ...)]
```
**ŠPATNĚ** (`@android:style/Theme.Splash` v Android SDK neexistuje → APT2260):
```csharp
[Activity(Theme = "@android:style/Theme.Splash", ...)]
```

## App.xaml.cs – inicializace okna (MAUI 9+)

**SPRÁVNĚ** (bez varování CS0618):
```csharp
protected override Window CreateWindow(IActivationState? activationState)
    => new Window(new AppShell());
```

## AndroidManifest.xml – AAPT2 problémy

- Komentáře s diakritikou v XML způsobují APT2067 → psát jen ASCII
- `android:label` může kolidovat se string resources → vynechat
- HTTP provoz řešit přes `Resources/xml/network_security_config.xml`, ne `usesCleartextTraffic`

## Emulátor – diakritika v cestě uživatele

QEMU nefunguje s ne-ASCII znaky v cestě k AVD (např. `MarekMackovík`).
Řešení: `ANDROID_AVD_HOME=D:\AndroidAVD` nebo fyzické zařízení.

## Xiaomi – nasazení přes USB

`INSTALL_FAILED_USER_RESTRICTED` = chybí „Instalace přes USB" ve vývojářských možnostech.
Na HyperOS: Nastavení → Soukromí → Speciální oprávnění → Instalace neznámých aplikací.
