# Navigation System Example

Complete navigation demo: MainMenu + Shop scenes, page/popup/window route types, auth-guard flow, mock bridges, and VContainer DI bootstrap.

## Scenes

| Scene | Hierarchy | Routes defined |
|-------|-----------|----------------|
| `MainMenu.unity` | `Canvas > MainMenuPage > [ShopButton, SettingsButton, LoginPanel]` | `MainMenu` (ScenePage, ClearHistory), `Login` (ScenePage) |
| `Shop.unity` | `Canvas > ShopPage > [DetailButton, BuyButton, BackButton]` | `Shop` (ScenePage, RequiresAuth), `Confirm` (Popup, IsTransient), `Settings` (Window, IsAdditive) |

## Setup steps

1. **Import this sample** into your project via Package Manager > Samples > Import.

2. **(One-time, after import)** The scenes currently reference the legacy `SamplePage` script (v0.2.0 GUID).
   Select `Canvas/MainMenuPage` and `Canvas/ShopPage`, remove the `SamplePage` component,
   and add `MainMenuPage` / `ShopPage` from `Scripts/` instead.

3. **Addressables**: create an Addressable group and add the popup/window prefabs you want
   to reference (e.g. `ConfirmPopup`, `SettingsWindow`). Assign those assets to the
   `AssetReference` fields on `SampleNavigationInstaller`.

4. **Attach `SampleNavigationInstaller`** to any root GameObject in `MainMenu.unity`
   (e.g. the `Canvas`). It builds the `NavigationConfig` in code on `Awake`, registers
   the mock bridges, resolves `NavigationService` from a VContainer scope, and navigates
   to `MainMenu` (the default route).

5. **Press Play** in `MainMenu.unity`. The mock auth bridge starts signed-in, so
   `ShopButton` navigates to `Shop` directly. Set `startSignedIn = false` on the
   installer to see the auth-guard redirect to `LoginPanel`.

## Route flow

```
MainMenu ──ShopButton──> Shop ──BuyButton──> Confirm (popup, transient)
         ──SettingsButton──> Settings (window, additive)
Shop    ──BackButton──> MainMenu
Shop    (auth-guard: if not signed-in -> LoginPanel)
```

## Scripts

| Script | Purpose |
|--------|---------|
| `SampleNavigationInstaller` | Builds `NavigationConfig` in code; registers mock `INavigationAuthBridge` / `INavigationUIBridge` / `INavigationAudioBridge`; bootstraps VContainer; navigates to default route on `Awake` |
| `MainMenuPage` | Wires `ShopButton` -> `NavigateTo("Shop")`, `SettingsButton` -> `NavigateTo("Settings")` |
| `ShopPage` | Wires `DetailButton` -> `SwitchSubPanel("Detail")`, `BuyButton` -> `NavigateTo("Confirm")` |
| `ConfirmPopupPanel` | Popup: `Yes` logs purchase, `No`/close hides |
| `SettingsPanel` | Window: `Close` hides |

## Notes

- Mock bridges are inner classes of `SampleNavigationInstaller` — replace them with your real implementations in a production project.
- No `UnityEditor` API is used in any sample script; everything compiles into a player build.
- `SamplePage.cs` (top level of this folder) is the legacy v0.2.0 sample, kept for reference.