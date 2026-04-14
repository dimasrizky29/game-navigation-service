# Navigation System Package Decoupling Plan

This plan outlines the architectural changes required to make the `com.unity.navigationsystem` package completely modular and decoupled from the main project's specific business logic and services. 

By implementing this, the navigation package becomes a "Plug-and-Play" system that can be reused in any internal project.

## User Review Required

> [!WARNING]
> This refactor will break existing code in the main project (`Assets/Scripts/...`) wherever `AppRoute` or `NavigationService` is used. We will need to update the main project to implement the new "Bridge" interfaces and replace the `AppRoute` enum with string constants.

## Proposed Changes

### 1. Route Identification Refactor (Decoupling `AppRoute`)

Currently, the `AppRoute` enum tightly links the package to game-specific screens (`BuyCourt`, `FixCourt`). We will replace the Enum-based routing with a flexible `string`-based routing (or `ScriptableObject` if preferred, but string is standard for web-like routing).

#### [MODIFY] `Runtime/Domain/Navigation/INavigationService.cs`
- Change `AppRoute` to `string` (e.g., `string routeId`) in method parameters like `NavigateTo` and `CurrentRoute`.

#### [DELETE] `Runtime/Domain/Navigation/AppRoute.cs`
- This file will be completely removed from the package.

#### [MODIFY] `Runtime/Infrastructure/Navigation/NavigationConfig.cs`
- Update `RouteDefinition` to use `public string RouteId;` instead of `public AppRoute Route;`.
- Update `ToDictionary()` to return `Dictionary<string, RouteDefinition>`.

#### [MODIFY] `Runtime/Infrastructure/Navigation/NavigationService.cs`
- Update all internal state (`RouteState`, `_activeWindows`, etc.) to use `string routeId` instead of `AppRoute`.

#### [MODIFY] `Runtime/Core/Navigation/ScenePageContainer.cs`
- Update `RoutePanelMap` to use `public string routeId`.

---

### 2. Service Bridge Pattern (Decoupling External Dependencies)

To remove dependencies on `IGlobalUIService`, `IAuthStore`, and `IAudioService` from the package, we will define "Bridge Interfaces" within the package. The main project will provide the implementation.

#### [NEW] `Runtime/Domain/Bridges/INavigationUIBridge.cs`
Define how the navigation system requests UI layers and loading screens.
```csharp
using UnityEngine;

public interface INavigationUIBridge
{
    void ShowLoading(bool show);
    Transform GetLayer(NavType navType);
}
```

#### [NEW] `Runtime/Domain/Bridges/INavigationAuthBridge.cs`
Define how the navigation system checks if a user can access a protected route.
```csharp
using System;

public interface INavigationAuthBridge
{
    bool IsAuthenticated { get; }
    // Action parameter is the authentication state (true if logged in)
    event Action<bool> OnAuthenticationChanged; 
}
```

#### [NEW] `Runtime/Domain/Bridges/INavigationAudioBridge.cs`
Define how the navigation system triggers UI sounds (like the "Back" button click).
```csharp
public interface INavigationAudioBridge
{
    void PlayBackSound();
}
```

#### [MODIFY] `Runtime/Infrastructure/Navigation/NavigationService.cs`
- Inject `INavigationUIBridge` and `INavigationAuthBridge` instead of `IGlobalUIService` and `IAuthStore`.
- Update the initialization and usages (`ShowLoading`, `GetLayer`, `IsLoggedIn`) to use the new bridges.

#### [MODIFY] `Runtime/Core/Navigation/BasePagePanel.cs`
- Inject `INavigationAudioBridge` instead of `IAudioService`.
- Replace `_audioService.PlayOneShot("ButtonClose")` with `_audioBridge.PlayBackSound()`.

---

### 3. Updating the Main Project (Outside the Package)

Once the package is updated, we will create the concrete implementations of these bridges in the main project (`Assets/Scripts/...`) and register them in the Dependency Injection container (`ProjectLifetimeScope`).

*Note: This will be implemented in the main project files, ensuring the package remains clean.*

## Open Questions

1. **Route Identity:** I propose using `string` (e.g., `"InGameMenu"`) for `RouteId` as it's the most flexible and easy to read. Another option is using `ScriptableObject` references (e.g., `RouteAsset`) for strong typing, but it adds a bit of boilerplate. Are you okay with using `string` for the `RouteId`?
2. **Main Project Adaptation:** After updating the package, I will need to fix the compile errors in the main project files that were using `AppRoute` or injecting the old services. Shall I proceed to create the bridge implementors in the `Assets/Scripts` folders as well?

## Verification Plan

### Automated Tests
- Ensure all existing unit tests in `Tests/` compile and pass by mocking the new Bridge interfaces.

### Manual Verification
- Verify that the unity project compiles successfully without any dependency errors originating from the `navigationsystem` package.
- Ensure the `navigationsystem` package no longer contains any reference to `IGlobalUIService`, `IAuthStore`, or `IAudioService`.
