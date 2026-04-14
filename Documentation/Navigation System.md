# Navigation System Technical Documentation

The **Navigation System** is a robust, clean-architecture-inspired routing framework for Unity. It decouples UI logic from scene management and provides a unified API for navigating between Scene-based Pages, Addressable Windows, and Popups.

## Key Features

- 🚀 **Unified Navigation**: Navigate to Scenes or Addressables with a single method call.
- 🏛️ **Clean Architecture**: Clear separation between Domain (interfaces), Infrastructure (logic), and Core (UI bases).
- 🛡️ **Auth Guards**: Middleware support to prevent access to routes based on authentication status.
- 📦 **Payload Support**: Pass data between views during navigation.
- 📜 **History Management**: Built-in undo/back functionality with support for sub-panel history.
- 🏗️ **Memory Efficiency**: Automatic cleanup of unused Addressable assets.

---

## Architecture Overview

The system is divided into three main layers:

### 1. Domain (Interfaces)
Located in `Runtime/Domain/Navigation`. Defines the contracts for the service, panels, and containers.
- `INavigationService`: The main API for navigation.
- `IPagePanel` & `ISubPanel`: Contracts for UI panels.
- `IViewPanel`: Base contract for any view (Page, Window, or Popup).

### 2. Infrastructure (Implementation)
Located in `Runtime/Infrastructure/Navigation`. Contains the concrete logic and configuration.
- `NavigationService`: Implementation of the navigation logic, history, and asset management.
- `NavigationConfig`: A ScriptableObject that stores all `RouteDefinition`s.

### 3. Core (Base Classes)
Located in `Runtime/Core/Navigation`. Provides base implementations for UI components.
- `BasePagePanel`: Base for main pages in a scene.
- `BaseSubPanel`: Base for nested panels within a page.
- `ScenePageContainer`: Registers in-scene panels to the service.

---

## Technical Flow

```mermaid
graph TD
    A[Caller] -->|NavigateTo| B[NavigationService]
    B -->|Check Config| C[NavigationConfig]
    C -->|Return RouteDefinition| B
    B -->|Auth Guard| D{Authenticated?}
    D -- No -->|Redirect| E[Login Route]
    D -- Yes --> F{Route Type?}
    F -- ScenePage --> G[HandleSceneNavigation]
    F -- Window/Popup --> H[HandleWindowNavigation]
    G -->|Load Scene| I[SceneManager]
    I -->|RegisterView| J[ScenePageContainer]
    J -->|GetPanel| K[BasePagePanel]
    H -->|Load Addressable| L[Addressables]
    L -->|Instantiate| M[IViewPanel Instance]
    K -->|Show| N[UI Screen]
    M -->|Show| N
```

---

## Core Components

### `NavigationConfig`
This ScriptableObject is the "Source of Truth" for all routes in your application.

| Property | Description |
| :--- | :--- |
| `RouteId` | Unique identifier for the route (e.g., "Shop", "Inventory"). |
| `Flags` | Behaviour flags (e.g., `RequiresAuth`, `IsAdditive`, `ClearHistory`). |
| `Type` | `ScenePage`, `Window`, or `Popup`. |
| `TargetScene` | Name of the scene for `ScenePage` types. |
| `Prefab` | `AssetReference` for `Window` or `Popup` types. |

### `NavigationService`
Access this via Dependency Injection (VContainer recommended).

**Main Methods:**
- `NavigateTo(string routeId, string sceneSuffix = "", object payload = null)`: Standard navigation.
- `GoBack()`: Navigates to the previous state in history.
- `HandleHardwareBack()`: Handles back button logic (Check sub-panels first, then route history).

---

## Guidelines for Use

### Setting up a New Scene Page
1. Create a UI Panel and inherit from `BasePagePanel`.
2. Add a `ScenePageContainer` component to an object in the scene.
3. Map your `RouteId` to the UI Panel instance in the `ScenePageContainer` inspector.
4. Add the route to the `NavigationConfig` ScriptableObject.

### Injecting Payloads
When navigating, you can pass any object as a payload:
```csharp
_navigationService.NavigateTo("UserProfile", payload: userData);
```
The target panel receives this via `SetPayload(object payload)`.

### Authentication Guarding
To protect a route, add the `RequiresAuth` flag in the `NavigationConfig`. The service will automatically redirect to the `LoginRouteId` defined in the config if the user is not authenticated.

---

## Advanced: Sub-Panel History
`BasePagePanel` supports nested navigation. If you have a profile page with "Edit" and "Settings" sub-panels:
- Navigating back will first pop the sub-panel history.
- Once sub-panels are back to the "Main" state, the next back action will pop the route history.

---

> [!TIP]
> Always use `NavigationService.HandleHardwareBack()` on Android/Input back actions to ensure the nested history is correctly processed before leaving the page.