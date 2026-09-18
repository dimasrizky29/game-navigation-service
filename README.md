# Navigation System

A professional routing and navigation framework for Unity. Decoupled scene-based pages, Addressable windows and popups, nested sub-panel history, auth guards and automatic Addressable lifecycle management, with Clean Architecture layering and VContainer-friendly dependency injection.

## Version 0.2.1

### Overview

This package provides a complete navigation system for Unity games, featuring:

- **Route definitions** as `ScriptableObject` assets with built-in validation (`IsValid()`)
- **NavigationConfig** — central config with route lookup, timeout/delay settings, and cached lookups
- **Scene-based pages** (`ScenePage`) and **Addressable windows/popups** with nested sub-panel history
- **Auth guards** per-route (`RequiresAuth` flag) and automatic login redirect
- **CanvasGroupFader** — per-panel fade with cancellation, eliminating sub-panel fade races
- **Single-flight navigation queue** — overlapping calls are serialized, no racing or interleaving
- **Editor tooling** — Route Manager window, Route Edit form, Config inspector extension, and asset utilities

### Features by Version

#### 0.2.1 (current)
- Sample Navigation System — MainMenu + Shop scenes with route definitions, Addressable references for popups/windows, and VContainer DI bootstrap
- Window > Yatech Studio > Route Manager — visual route table, add/edit/delete, Validate All
- Edit Route form via Window > Yatech Studio > Add Route, with per-route type (ScenePage / Popup / Window)
- NavigationConfigEditor — inspector buttons "Open Route Manager" and "+ Add Route" + validity summary footer
- RouteAssetUtility — asset IO for .asset route assets (Create/Get/Delete with dedup)
- MainMenuPage / ShopPage / ConfirmPopupPanel / SettingsPanel sample scripts with mock bridges

#### 0.2.0
- `INavigationLogger` / `UnityNavigationLogger` / `NullNavigationLogger` — no bespoke logging library dependency
- `RouteDefinition` as `ScriptableObject` with `IsValid()` self-check
- `NavigationConfig` exposes cached `Lookup`, `TryGet`, `InvalidateCache`, and `OnValidate`
- Configurable `SceneRegisterTimeoutSeconds` and `SceneTransitionDelaySeconds`
- `CanvasGroupFader` — per-panel fade with cancellation
- Real EditMode tests for config lookup, route validation, and navigation-service edge cases
- `LICENSE` file (MIT)
- Namespaces moved under `YatechStudio.Navigation.*` (breaking change)

### Getting Started

1. Add the package via Unity Package Manager or git:
   ```bash
   git clone https://github.com/yatech-studio/game-navigation-service.git
   ```

2. Create a `NavigationConfig` asset in your project (`Create > Yatech Studio > Navigation Config`).

3. Add routes via the **Route Manager** window (`Window > Yatech Studio > Route Manager`) or the inspector "Add Route" button.

4. Use the `NavigationService` from your VContainer (or manual) DI container to navigate:
   ```csharp
   service.NavigateTo("MainMenu").Forget();
   ```

5. Sample scenes are included in `Samples~/Example/Scenes/`. Open `MainMenu.unity` to see the demo setup.

### Script Samples

| Script | Description |
|--------|-------------|
| `SampleNavigationInstaller` | Bootstrap `NavigationConfig` in code with mock bridges (Auth, UI, Audio) |
| `MainMenuPage` | Main menu with Shop and Settings buttons |
| `ShopPage` | Shop page with Detail, Buy, and Back buttons |
| `ConfirmPopupPanel` | Popup with Yes/No buttons for purchase confirmation |
| `SettingsPanel` | Settings panel with Close button |

### Editor Window Shortcuts

| Menu | Action |
|------|--------|
| `Window > Yatech Studio > Route Manager` | Visual route table: add, edit, delete routes; Validate All |
| `Window > Yatech Studio > Add Route` | Open form to create a new route (ScenePage / Popup / Window) |
| `Window > Yatech Studio > Navigation Config` | Inspector extension: quick-add routes + validity summary |

### Addressables

All popup and window routes reference assets via **Addressable Asset Groups**. Ensure your project has an Addressables setup and the referenced assets are added to the appropriate group. The sample scripts use `AssetReference` fields that resolve at runtime via `Addressables.LoadAssetAsync`.

### License

MIT. See `LICENSE` file for details.

### Documentation

- Full docs: https://github.com/yatech-studio/game-navigation-service/blob/main/Documentation/Navigation%20System.md
- Changelog: https://github.com/yatech-studio/game-navigation-service/blob/main/CHANGELOG.md