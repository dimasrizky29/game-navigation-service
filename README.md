# 🧭 Navigation System for Unity

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-blue.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

A professional routing and navigation framework designed for modern Unity applications. Built with **Clean Architecture** principles, it provides a seamless way to manage complex UI flows, nested views, and cross-scene navigation.

---

## 🔥 Key Features

- **Decoupled Navigation**: Zero direct dependencies between your pages. Navigate via string IDs.
- **Multi-Type Support**: Handles Scene-based pages, Addressable Windows, and Popups uniformly.
- **Deep Nesting**: Built-in support for sub-panel history within a single page.
- **Security First**: Integrated Auth Guards for protected routes.
- **Smart History**: Intelligent back-stack management that understands nested UI states.
- **Performance Optimized**: Automated Addressable life-cycle management and memory cleanup.

---

## 🛠 Installation

### 1. Via Package Manager
Add the following URL to your `manifest.json` or through the Package Manager UI:
```bash
https://github.com/yatech-studio/com.yatechstudio.navigationsystem.git
```

### 2. Dependencies
- [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [UniTask](https://github.com/Cysharp/UniTask)
- [VContainer](https://vcontainer.hadashikun.jp/) (Recommended for DI)

---

## 🚀 Quick Start

### 1. Define Your Routes
Create a **NavigationConfig** asset from `Create > Service > Configs > Navigation`. Add your routes here:

- **ScenePage**: For main screens (e.g., "Lobby", "Market").
- **Window**: For full-screen addressable overlays.
- **Popup**: For small overlays (e.g., "Confirmation", "Alert").

### 2. Implement a Page
Inherit from `BasePagePanel` to create your UI logical controller:

```csharp
public class LobbyPageView : BasePagePanel
{
    public override async UniTask Show(CancellationToken token = default)
    {
        await base.Show(token);
        // Custom animation or logic here
    }
}
```

### 3. Register in Scene
Drop a `ScenePageContainer` into your scene and link your `BasePagePanel` instances to their respective `RouteId`.

### 4. Navigate!
Inject `INavigationService` and call `NavigateTo`:

```csharp
// Simple navigation
await _navigationService.NavigateTo("Shop");

// Navigation with data
await _navigationService.NavigateTo("Profile", payload: myUserObject);
```

---

## 📖 Documentation

For detailed technical guides, implementation details, and API reference, please see the [Technical Documentation](Documentation/Navigation%20System.md).

---

## 🤝 Contributing
Interested in contributing? Please read our contributing guidelines first.

---

## 📄 License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

Developed with ❤️ by **Yatech Studio**