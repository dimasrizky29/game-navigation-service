# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-09-16

### Added
- `INavigationLogger` / `UnityNavigationLogger` / `NullNavigationLogger` — the package no longer depends on any bespoke logging library.
- `RouteDefinition` is now a `ScriptableObject` with an `IsValid()` self-check; the old `RouteDefinition` class is replaced.
- `NavigationConfig` exposes a cached `Lookup`, `TryGet`, `InvalidateCache`, and `OnValidate` route validation.
- Configurable `SceneRegisterTimeoutSeconds` and `SceneTransitionDelaySeconds` (replaces hardcoded 5s / 2s).
- `CanvasGroupFader` — per-panel fade with cancellation, eliminating sub-panel fade races.
- Real EditMode tests for config lookup, route validation, and navigation-service edge cases.
- `LICENSE` file (MIT).

### Changed
- **Namespaces**: all types moved under `YatechStudio.Navigation.*`. This is a breaking change for consumers.
- **`NavigationService` is now single-flight**: navigation requests are serialized through an internal queue, so overlapping calls can no longer race or interleave.
- **Fixed scene-registration race**: `_sceneLoadTcs` is created before the scene load begins, so `RegisterView` can never miss it.
- **Fixed window double-spawn**: concurrent navigation to the same Window/Popup awaits the in-flight load instead of instantiating twice.
- **Fixed history-push logic**: the same route is no longer pushed twice in a row; transient routes are never remembered.
- **Fixed auth redirect recursion**: the login route is never auth-guarded.
- `ReleaseAllWindows` now clears in-flight loads too; `Dispose` is idempotent.
- asmdef names cleaned up (`Unity84b3bb65eed9dd219507.*` → `Yatechstudio.Navigationsystem.*`); GUIDs preserved.
- README rewritten; documentation expanded.

### Removed
- `Editor/ServiceInstaller.cs` — the `InitializeOnLoad` auto-install hack. Dependencies are now declared in `package.json` and resolved by UPM.
- Template boilerplate sample (`SampleExample.cs` / `MyPublicSampleExampleClass`).

## [0.1.0] - 2026-04-14

- First release.
