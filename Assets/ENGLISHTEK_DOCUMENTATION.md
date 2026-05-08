# TekContainer — Complete Documentation

> Last updated: May 8, 2026
> Unity project. Namespace root: `Tek.Core` (container), `EnglishTek.Grade1.ID###` / `EnglishTek.Grade2.ID###` (interactives).

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Folder Structure](#2-folder-structure)
3. [Architecture Overview](#3-architecture-overview)
4. [Core Scripts Reference](#4-core-scripts-reference)
   - 4.1 [GameSession](#41-gamesession)
   - 4.2 [InteractiveController](#42-interactivecontroller)
   - 4.3 [InteractiveManifest](#43-interactivemanifest)
   - 4.4 [InteractiveCatalogEntry](#44-interactivecatalogentry)
   - 4.5 [InteractiveCatalogMenu](#45-interactivecatalogmenu)
   - 4.6 [CatalogMenuNavigator](#46-catalogmenunavigator)
   - 4.7 [ContainerReturnOverlay](#47-containerreturnoverlay)
   - 4.8 [UIGroup](#48-uigroup)
   - 4.9 [Catalog Helpers](#49-catalog-helpers)
   - 4.10 [ArcCarousel](#410-arccarousel)
   - 4.11 [CarouselHomeBackground](#411-carouselhomebackground)
   - 4.12 [InteractiveCacheClearer (Editor)](#412-interactivecacheclearer-editor)
  - 4.13 [BundleUrlHelper](#413b-bundleurlhelper)
  - 4.14 [XML Loading Pattern](#414-xml-loading-pattern-current-code)
  - 4.15 [GameManager Pattern](#415-gamemanager-pattern-current-code)
   - 4.16 [AspectRatioEnforcer](#416-aspectratioenforcer)
   - 4.17 [InteractivePacker (Container Editor)](#417-interactivepacker-container-editor)
5. [Interactive Games Reference](#5-interactive-games-reference)
   - 5.1 [ID106 — Whack-a-Mole (Grade 1, Grammar)](#51-id106--whack-a-mole-grade-1-grammar)
   - 5.2 [ID213 — A Day at the Beach (Grade 1, Grammar)](#52-id213--a-day-at-the-beach-grade-1-grammar)
   - 5.3 [ID101 — Whack-a-Mushroom (Grade 2, FilipinoTek)](#53-id101--whack-a-mushroom-grade-2-filipinotek)
   - 5.4 [ID232 — Grade 2 Grammar (Robot Factory)](#54-id232--grade-2-grammar-robot-factory)
    - 5.5 [ID102 — FilipinoTek Grade 2 (Legacy Flow)](#55-id102--filipinotek-grade-2-legacy-flow)
    - 5.6 [ID313 — Grade 1 Grammar](#56-id313--grade-1-grammar)
6. [Server Layout & Bundle Naming](#6-server-layout--bundle-naming)
7. [Catalog JSON Schema](#7-catalog-json-schema)
8. [Data Flow — End to End](#8-data-flow--end-to-end)
9. [UI Animation System](#9-ui-animation-system)
10. [LMS Score Submission](#10-lms-score-submission)
11. [Common Issues & Fixes](#11-common-issues--fixes)
12. [Adding a New Interactive — Step by Step](#12-adding-a-new-interactive--step-by-step)

---

## 1. Project Overview

**TekContainer** is a Unity multi-platform shell that:

- Displays of interactive games fetched from a remote HTTP server.
- Downloads, caches, and launches individual games as **AssetBundles** at runtime.
- Returns the player to the container menu when a game session ends.
- Submits scores to a remote LMS API.

Games are completely decoupled from the container — they live in separate AssetBundles on the server and are never compiled into the container build. The container only holds the loading and routing infrastructure.

---

## 2. Folder Structure

```
EnglishTekContainer/
├── Assets/
│   ├── Core/
│   │   ├── Core.asmdef
│   │   ├── Editor/
│   │   │   └── InteractiveCacheClearer.cs      ← Toolbar to clear bundle cache
│   │   ├── Scripts/
│   │   │   ├── GameSession.cs                  ← Global session state
│   │   │   ├── ContainerReturnOverlay.cs       ← Persistent back button
│   │   │   ├── Interactive/
│   │   │   │   ├── InteractiveController.cs    ← Main coordinator
│   │   │   │   ├── InteractiveCatalogMenu.cs   ← Catalog UI
│   │   │   │   ├── InteractiveCatalogEntry.cs  ← Data model
│   │   │   │   ├── InteractiveManifest.cs      ← ScriptableObject for a game
│   │   │   │   └── BundleUrlHelper.cs          ← URL/path helpers
│   │   │   ├── Catalog/
│   │   │   │   ├── CatalogFilter.cs
│   │   │   │   ├── CatalogStringHelper.cs
│   │   │   │   ├── CatalogThumbnailLoader.cs
│   │   │   │   ├── CatalogMenuNavigator.cs     ← Animated nav extension
│   │   │   │   └── CatalogUiFactory.cs
│   │   │   └── UI/
│   │   │       ├── ArcCarousel.cs
│   │   │       ├── UIGroup.cs
│   │   │       ├── AspectRatioEnforcer.cs      ← Letterbox aspect ratio enforcer
│   │   │       └── CarouselHomeBackground.cs
│   │   └── InteractiveScripts/
│   │       ├── ID101_Scripts/   (ID101.asmdef)  ← FilipinoTek.Grade2.ID101
│   │       ├── ID102_Scripts/   (ID102.asmdef)  ← FilipinoTek.Grade2.ID102
│   │       ├── ID106_Scripts/   (ID106.asmdef)
│   │       ├── ID213_Scripts/   (ID213.asmdef)
│   │       ├── ID232_Scripts/   (ID232.asmdef)
│   │       └── ID313_Scripts/   (ID313.asmdef)
│   ├── link.xml                               ← IL2CPP stripping preservation list
│   └── Resources/
│       ├── 102/                               ← Legacy ID102 XML root (no XML/ prefix)
│       └── XML/                               ← All game XML — loaded via Resources.Load
│           ├── 101/  (Itembanks.xml, Dialougebanks.xml, Instructions_Level1.xml …)
│           ├── 106/  (Instruction.xml, Itembank_Practice.xml, Feedback.xml …)
│           ├── 213/  (Instruction.xml, Itembank_Practice.xml, Feedback.xml …)
│           ├── 232/  (Instruction.xml, Itembank_Practice.xml, Feedback.xml …)
│           └── 313/  (Instruction.xml, Itembank_Practice.xml, Feedback.xml …)
└── ProjectSettings/
```

---

## 3. Architecture Overview

```
InteractiveController  (MonoBehaviour — one per grade/scene)
    │
    ├── fetches {serverRoot}/{grade}/catalog.json
    │       └── populates List<InteractiveCatalogEntry>
    │
    ├── fires CatalogUpdated event
    │       └── InteractiveCatalogMenu / CatalogMenuNavigator render buttons
    │
    └── RequestGameLoad(gameId)
            ├── resolve folder URL from catalog entry
            ├── download .assets bundle (local cache first)
            ├── download .scenes bundle (local cache first)
            ├── set GameSession.CurrentAssetBundle / CurrentSceneBundle / CurrentManifest
            ├── spawn ContainerReturnOverlay (DontDestroyOnLoad)
            └── SceneManager.LoadScene(manifest.firstSceneName)
                    └── Game runs independently.
                        ContainerReturnOverlay back button
                        calls GameSession.CleanUp() and returns.
```

---

## 4. Core Scripts Reference

### 4.1 `GameSession`

**File:** `Assets/Core/Scripts/GameSession.cs`
**Type:** `static class` — `Tek.Core`

Global state bag bridging container and running game.

| Member | Type | Description |
|--------|------|-------------|
| `CurrentManifest` | `InteractiveManifest` | Manifest of the running game. |
| `CurrentAssetBundle` | `AssetBundle` | Loaded `.assets` bundle. |
| `CurrentSceneBundle` | `AssetBundle` | Loaded `.scenes` bundle. |
| `ContainerSceneName` | `string` | Container scene name to return to. |
| `CleanUp()` | `void` | Unloads both bundles, nulls all fields, calls `Resources.UnloadUnusedAssets()`. |

**Usage in gameplay scripts:**
```csharp
string xml = GameSession.CurrentManifest.GetXMLText("Itembank_Practice");
// Internally calls Resources.Load<TextAsset>("XML/106/Itembank_Practice")
```

---

### 4.2 `InteractiveController`

**File:** `Assets/Core/Scripts/Interactive/InteractiveController.cs`
**Type:** `MonoBehaviour` — `Tek.Core`

Central coordinator. One instance per grade, lives in the container scene.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `serverRoot` | `http://localhost:8080/Interactive/` | Base URL for all server requests. |
| `grade` | `grade1` | Grade prefix for catalog and bundle names. Use `grade2` for Grade 2. |
| `bundlePrefix` | _(empty)_ | Prefix for bundle file names. Set in Inspector per product (e.g. `englishtek`, `filipinotek`). |
| `catalogFileName` | `catalog.json` | Constant in code (not Inspector-exposed). |
| `defaultCategory` | _(empty)_ | Fallback category for default folder paths. |
| `defaultUnit` | _(empty)_ | Fallback unit for default folder paths. |
| `refreshCatalogOnStart` | `true` | Auto-fetch catalog on `Start()`. |
| `overlayPrefab` | `null` | Optional back button overlay prefab. |
| `overlayButtonCorner` | `TopLeft` | Screen corner for the back button. |
| `overlayButtonPadding` | `(10, 10)` | Padding from the corner. |

#### Public API

| Member | Description |
|--------|-------------|
| `AvailableInteractives` | `IReadOnlyList<InteractiveCatalogEntry>` — current catalog entries. |
| `CatalogUpdated` | `event Action<IReadOnlyList<InteractiveCatalogEntry>>` |
| `CatalogLoadFailed` | `event Action<string>` — error message on failure. |
| `GameLoadOfflineBlocked` | `event Action<string, InteractiveCatalogEntry>` — fired when not cached and offline. |
| `GameLoadStarted` | `event Action<string, InteractiveCatalogEntry>` — loading/downloading status message. |
| `GameLoadFinished` | `event Action` — fired after success/failure handling completes. |
| `RefreshCatalog()` | Re-fetches catalog JSON from server. |
| `RequestGameLoad(string gameId)` | Downloads and launches the game. |
| `IsInteractiveCached(string gameId)` | `true` when both bundle files exist in local cache. |
| `IsCatalogCached()` | `true` when the grade catalog was cached locally. |
| `ResolveCatalogAssetUrl(entry, assetPath)` | Resolves relative image paths to absolute URLs. |

#### URL Conventions

| Pattern | Result |
|---------|--------|
| Catalog URL | `{serverRoot}/{grade}/catalog.json` |
| Folder URL | `{serverRoot}/{grade}/{category}/{unit}/{id}/` |
| Bundle base | `{bundlePrefix}.{grade}.{id}` (all lowercase) |
| Asset bundle | `{folder}{bundleBase}.assets` |
| Scene bundle | `{folder}{bundleBase}.scenes` |

#### Local Cache

Bundles are cached at:
```
Application.persistentDataPath/InteractiveCache/{cacheKey}/
```
Cache is tried first on every load. Clear via **TekContainer → Clear Interactive Cache** in the Unity toolbar.

`InteractiveController` also version-resets all persistent caches (`CatalogCache`, `InteractiveCache`, `ThumbnailCache`) when `Application.version` changes.

---

### 4.3 `InteractiveManifest`

**File:** `Assets/Core/Scripts/Interactive/InteractiveManifest.cs`
**Type:** `ScriptableObject` (`[CreateAssetMenu]`) — `Tek.Core`

Describes a game bundle. Created in the game project and baked into the `.assets` bundle.

| Field | Type | Description |
|-------|------|-------------|
| `bundleName` | `string` | AssetBundle name base (informational, used by editor tools). |
| `gameId` | `int` | Numeric game ID (e.g. `106`). Must match the ID in the GameManager namespace and the `Resources/XML/{id}/` folder. |
| `firstSceneName` | `string` | Scene to load on startup. Set to `"Title"`. |
| `allScenes` | `List<Object>` | All scenes in the bundle (build tracking). |
| `xmlConfigs` | `List<NamedXML>` | Key → TextAsset XML pairs. **Inspector-only.** Used as a fallback source by `GetXMLText` and by the editor import tool. Not loaded from the bundle at runtime. |
| `prefabsToInclude` | `GameObject[]` | Prefabs force-included in `.assets`. |
| `GetXMLText(key)` | `string` | Loads `Resources/XML/{gameId}/{key}` first. Falls back to `xmlConfigs` if the Resources file is missing. Returns `null` if neither is found. |

> **XML is never bundled.** Files live in `Assets/Resources/XML/{gameId}/` in the container and are loaded at runtime via `Resources.Load`. Use [InteractivePacker](#417-interactivepacker-container-editor) to import XML from the game project into the container when the tool is installed.

---

### 4.4 `InteractiveCatalogEntry`

**File:** `Assets/Core/Scripts/Interactive/InteractiveCatalogEntry.cs`
**Type:** `[Serializable]` class — `Tek.Core`

One entry in `catalog.json`.

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique ID (e.g. `"ID106"`). Used for bundle lookup. |
| `title` | No | Display name. Falls back to `id` if empty. |
| `category` | No | Lesson category (e.g. `"grammar"`). |
| `unit` | No | Unit (e.g. `"unit1"`). |
| `image` | No | Thumbnail path relative to entry folder. |
| `home` | No | Full-bleed background path. |
| `folder` | No | Explicit folder override (e.g. `Grade 1/grammar/unit1/ID106`). |
| `grade` | No | Grade override used when composing default folder/bundle paths. |
| `bundleBaseName` | No | Explicit bundle base override (e.g. `englishtek.grade1.id106`). |
| `bundleVersion` | No | Version suffix used in cache key to force re-download on updates. |
| `enabled` | Yes | `false` hides entry from UI without removing it. |

`DisplayName` property returns `title` when available, otherwise `id`.

Root catalog document: `{ "interactives": [ ... ] }`.

---

### 4.5 `InteractiveCatalogMenu`

**Files:** `Assets/Core/Scripts/Interactive/InteractiveCatalogMenu.cs` + `InteractiveCatalogMenu.Buttons.cs` (partial class)
**Type:** `MonoBehaviour` — `Tek.Core`

Renders catalog as three-level navigation: **Category → Unit → Entry**.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `controller` | _(required)_ | Reference to `InteractiveController`. Auto-found if null. |
| `refreshOnStart` | `true` | Re-fetches catalog on `Start()`. |
| `showBuiltInCatalogPanel` | `false` | Generates a panel procedurally at runtime. |
| `autoGenerateLessonButtons` | `true` | Creates category buttons from catalog data. |
| `unitButtonContainer` | _(optional)_ | Parent for unit buttons (prefab mode). |
| `unitButtonPrefab` | _(optional)_ | Prefab for each unit button. |
| `interactiveButtonContainer` | _(optional)_ | Parent for entry buttons. |
| `interactiveButtonPrefab` | _(optional)_ | Prefab for each entry button. |
| `autoSelectFirstUnit` | `true` | Auto-selects first unit when category is picked. |

#### Public Methods

| Method | Description |
|--------|-------------|
| `SelectLesson(category)` | Filters by category. |
| `SelectUnit(unit)` | Filters by unit within current category. |
| `virtual GoBack()` | Override in subclass for back navigation. |

---

### 4.6 `CatalogMenuNavigator`

**File:** `Assets/Core/Scripts/Catalog/CatalogMenuNavigator.cs`
**Type:** Extends `InteractiveCatalogMenu` — `Tek.Core`

Adds animated UIGroup transitions between navigation levels.

#### Additional Inspector Fields

| Field | Assign | Behaviour |
|-------|--------|-----------|
| `categoryGroup` | Container holding category buttons | Hidden after category is picked. |
| `unitGroup` | Container holding unit buttons | Shown after category pick; hidden after unit pick. |
| `entryGroup` | Container holding entry buttons | Shown after unit pick. |
| `lessonBackButtonGroup` | Optional `UIGroup` for back button visibility | Shown only in unit/entry levels. |
| `lessonBackButtonObject` | Optional `GameObject` fallback | Used when back button is not a `UIGroup`. |

#### Additional Methods

| Method | Description |
|--------|-------------|
| `GoBack()` | Entry visible → shows unit. Unit visible → shows category. |
| `GoToCategories()` | Instantly returns to category group. Wire to a "home" button. |

---

### 4.7 `ContainerReturnOverlay`

**File:** `Assets/Core/Scripts/ContainerReturnOverlay.cs`
**Type:** `MonoBehaviour` (DontDestroyOnLoad singleton) — `Tek.Core`

Persistent back button overlay shown inside the running interactive.

- Created by `InteractiveController.EnsureExists()` just before loading the game scene.
- Shows itself only when the active scene is named `"Title"` (game's first scene).
- Pressing back calls `GameSession.CleanUp()` then loads `GameSession.ContainerSceneName`.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `backButton` | `null` | Assign prefab Button; procedural if null. |
| `backButtonLabel` | `"< Menu"` | Text on the button. |
| `buttonCorner` | `TopLeft` | Screen corner. |
| `buttonSize` | `(120, 44)` | Size of procedural button. |
| `buttonPadding` | `(10, 10)` | Distance from corner. |

**`OverlayButtonCorner` enum:** `TopLeft | TopRight | BottomLeft | BottomRight`

---

### 4.8 `UIGroup`

**File:** `Assets/Core/Scripts/UI/UIGroup.cs`
**Type:** `MonoBehaviour` — `Tek.Core`

Attach to any UI container to give it animated show/hide. Manages a `CanvasGroup` automatically.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `animationIn` | `Fade` | Animation for `Show()`. `None` = instant. |
| `animationOut` | `Fade` | Animation for `Hide()`. `None` = stays visible. |
| `duration` | `0.25` | Seconds. |
| `slideDistance` | `60` | Pixels for slide animations. |
| `startHidden` | `false` | Deactivate on `Awake`. |

#### Public API

| Method | Description |
|--------|-------------|
| `Show(Action onComplete = null)` | Animate into view. |
| `Hide(Action onComplete = null)` | Animate out, then deactivate. |
| `HideWith(UIGroupAnimation overrideAnimation, Action onComplete = null)` | One-shot hide animation override. |
| `ShowImmediate()` | Snap visible, no animation. |
| `HideImmediate()` | Snap hidden, deactivate, no animation. |
| `IsVisible` | `bool` — current visibility. |
| `OnShown` | `event Action` — fired when show completes. |

**Chaining example:**
```csharp
categoryGroup.Hide(() => unitGroup.Show());
```

#### Animation Types

| Value | Show | Hide |
|-------|------|------|
| `None` | Snap in | Does nothing (stays visible) |
| `Fade` | Alpha 0→1 | Alpha 1→0, then deactivate |
| `SlideFromLeft/Right/Top/Bottom` | Slide + fade in | Slide + fade out |
| `ScalePop` | Scale 0→1 (spring) | Scale 1→0 |
| `FadeScalePop` | Scale 0.7→1 + fade | Scale 1→0.7 + fade |
| `FadeSlideUp/Down` | Rise/drop + fade | Reverse |
| `SlideUp` | Rise + fade in | Slides up, **stays visible** |

Easing: `Show` = `EaseOutQuad`, `Hide` = `EaseInQuad`, `ScalePop` = `EaseOutBack`.

---

### 4.9 Catalog Helpers

All `internal static`, `Tek.Core`.

**`CatalogFilter`** — `Assets/Core/Scripts/Catalog/CatalogFilter.cs`

| Method | Description |
|--------|-------------|
| `HasCategory(interactives, category)` | `true` if any entry matches category. |
| `BuildUniqueCategories(interactives)` | Deduplicated list of all categories. |
| `BuildUnitsForCategory(interactives, category)` | Deduplicated units for category. |
| `EffectiveLabel(raw)` | Trims and lowercases a raw category or unit string (replaces old `EffectiveCategory`/`EffectiveUnit`). |

**`CatalogStringHelper`** — `Assets/Core/Scripts/Catalog/CatalogStringHelper.cs`

| Method | Description |
|--------|-------------|
| `NormalizeCategory(value)` | Trim + lowercase. |
| `NormalizeUnit(value)` | Trim + lowercase. |
| `FormatCategoryLabel(category)` | Title-case for display. |
| `FormatUnitLabel(unit)` | Title-case for display. |

**`CatalogThumbnailLoader`** — `Assets/Core/Scripts/Catalog/CatalogThumbnailLoader.cs`

Async HTTP image downloader. Auto-attached to `InteractiveCatalogMenu` GameObject.

| Method | Description |
|--------|-------------|
| `TryLoadThumbnail(entry, target, controller)` | Downloads and applies thumbnail to `RawImage`. |
| `StopAll()` | Stops all in-flight coroutines. |
| `ClearTextures()` | Destroys all downloaded `Texture2D` objects. |

**`CatalogUiFactory`** — `Assets/Core/Scripts/Catalog/CatalogUiFactory.cs`

Procedural UI builders for when no prefabs are assigned.

| Method | Description |
|--------|-------------|
| `CreateTextElement(name, parent, fontSize)` | Creates `Text` with Arial. |
| `CreateThumbnailElement(parent)` | Creates left-anchored `RawImage`. |
| `CreateCategoryRow(parent)` | Creates `HorizontalLayoutGroup` row. |
| `CreateEntriesContainer(parent)` | Creates `VerticalLayoutGroup` container. |

---

### 4.10 `ArcCarousel`

**File:** `Assets/Core/Scripts/UI/ArcCarousel.cs`
**Type:** `MonoBehaviour` (`[ExecuteAlways]`) — `Tek.Core`

Swipeable horizontal arc carousel. Items are children of this GameObject.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `itemWidth` | `120` | Width of each item. |
| `itemHeight` | `60` | Height of each item. |
| `spacing` | `16` | Gap between items. |
| `arcHeight` | `50` | Pixels the center item rises above edge items. |
| `visibleRadius` | `3` | Items within this range from center are on the arc. |
| `edgeScale` | `0.75` | Scale applied to edge items (center = 1.0). |
| `snapSpeed` | `10` | Lerp speed after drag ends. |

#### Public API

| Member | Description |
|--------|-------------|
| `OnCenterIndexChanged` | `event Action<int>` — fires when centered item changes. |
| `GoToIndex(int)` | Animates to an item index. |
| `Rebuild()` | Forces re-read of children and repositions. |

---

### 4.11 `CarouselHomeBackground`

**File:** `Assets/Core/Scripts/UI/CarouselHomeBackground.cs`
**Type:** `MonoBehaviour` — `Tek.Core`

Watches an `ArcCarousel` and swaps a `RawImage` background to the centered entry's `home` image.

#### Inspector Fields

| Field | Description |
|-------|-------------|
| `carousel` | The `ArcCarousel` to watch. |
| `backgroundImage` | Full-bleed `RawImage` to update. |
| `controller` | `InteractiveController` for resolving asset URLs. |

**`SetEntries(IReadOnlyList<InteractiveCatalogEntry>)`** — called by `InteractiveCatalogMenu` after spawning entry buttons. Order must match carousel children.

---

### 4.12 `InteractiveCacheClearer` (Editor)

**File:** `Assets/Core/Editor/InteractiveCacheClearer.cs`
**Type:** `static class` (Editor only) — `Tek.Core.Editor`

Unity toolbar menu at **TekContainer → Clear Interactive Cache**.

| Menu Item | Action |
|-----------|--------|
| **All** | Deletes `InteractiveCache`, `CatalogCache`, and `ThumbnailCache` folders (with confirmation). |
| **Show Cache Folder** | Opens `Application.persistentDataPath` in Explorer. |

Cache folders under `Application.persistentDataPath`:

| Folder | Contents |
|--------|----------|
| `InteractiveCache/` | Downloaded `.assets` and `.scenes` bundles per game ID. |
| `CatalogCache/` | Cached `catalog.json` per grade. |
| `ThumbnailCache/` | Downloaded thumbnail and home background images. |

> Use **All** whenever you push new bundles, catalog changes, or updated images to the server.

---

### 4.13b `BundleUrlHelper`

**File:** `Assets/Core/Scripts/Interactive/BundleUrlHelper.cs`
**Type:** `internal static class` — `Tek.Core`

Static URL and path helpers extracted from `InteractiveController`. Not called directly by game scripts.

| Method | Description |
|--------|-------------|
| `NormalizePathPart(s)` | Trims and normalizes slashes in a path segment. |
| `NormalizeLookupId(id)` | Uppercases and ensures `ID` prefix. |
| `NormalizeCacheKey(id)` | Normalizes cache key and replaces slashes/spaces with `_`. |
| `EncodePathSegments(s)` | URL-encodes spaces and special chars in a path segment. |
| `BuildDefaultFolderPath(grade, category, unit, id)` | Returns `"{grade}/{category}/{unit}/{id}"` omitting empty parts. |
| `BuildDefaultBundleBaseName(prefix, grade, id)` | Returns `"{prefix}.{grade}.{id}"` (all lowercase). |
| `BuildCacheKey(gameId, bundleBase, version)` | Returns normalized cache-directory key with optional version suffix. |

---

### 4.14 `XML Loading Pattern (Current Code)`

There is currently **no** `XmlLoader.cs` utility in this repository. Interactives load XML directly with `Resources.Load` and then parse via `XmlDocument`.

Common pattern used by ID106 / ID213 / ID232 / ID313:
```csharp
string path = "XML/" + GameManager.GameID + "/Itembank_" + difficulty;
TextAsset asset = Resources.Load<TextAsset>(path);
if (asset == null) { Debug.LogError("Missing XML: " + path); return; }

XmlDocument doc = new XmlDocument();
doc.LoadXml(asset.text);
```

Legacy FilipinoTek pattern used by ID101:
```csharp
Resources.Load<TextAsset>("XML/101/Itembanks");
Resources.Load<TextAsset>("XML/101/Dialougebanks"); // Intentional historical spelling
```

Legacy FilipinoTek pattern used by ID102:
```csharp
Resources.Load<TextAsset>("102/Itembanks");
Resources.Load<TextAsset>("102/Dialougebanks");
```

---

### 4.15 `GameManager Pattern (Current Code)`

There is currently **no** shared `IXmlLoadable` interface in this repository. Existing interactives use a static `GameManager` class per game, typically with:

- `Initialize()` to reset score/session state
- `GenerateItem()` or `LoadItembanks()` to load data for selected difficulty
- `NextItem()` to progress gameplay
- `CheckAnswer()` for validation
- `Feedback()` or equivalent to map score to response text

For new games, use ID106 or ID213 as the baseline unless your design specifically matches the ID101/ID102 style.

---

### 4.16 `AspectRatioEnforcer`

**File:** `Assets/Core/Scripts/UI/AspectRatioEnforcer.cs`
**Type:** `MonoBehaviour` (`[RequireComponent(Camera)]`, `DontDestroyOnLoad`) — `Tek.Core`

Attach to the main camera to letterbox/pillarbox the viewport to a fixed aspect ratio. A second camera fills the screen with black behind it.

| Field | Default | Description |
|-------|---------|-------------|
| `targetWidth` | `800` | Target resolution width. |
| `targetHeight` | `600` | Target resolution height. |

On `Awake`: calls `DontDestroyOnLoad`, creates a `BarCamera` child, and computes the viewport rect. Subscribes to `SceneManager.sceneLoaded` to re-apply the rect to cameras in each newly loaded scene using scene-scoped discovery (`scene.GetRootGameObjects()`) — it never touches cameras from other scenes.

---

### 4.17 `InteractivePacker` (Container Editor)

**File status:** `Assets/Core/InteractivePackerV3.zip` (packaged, not extracted in current workspace)
**Menu:** `Tools → Interactive Game Packer`
**Namespace:** `Tek.Core`

Editor window for preparing a game's AssetBundle and syncing XML/namespaces.

#### Workflow

| Button | Action |
|--------|--------|
| **Import XML to Container (Resources)** | Copies every `xmlConfigs` entry from the manifest into container `Resources`. XML is NOT tagged in the bundle. |
| **1. Tag Assets for Bundle** | Sets AssetBundle names: manifest + prefabs → `{name}.assets`; scenes → `{name}.scenes`. |
| **2. Build Asset Bundle** | Calls `BuildPipeline.BuildAssetBundles` for Android into `ServerData/`. |
| **3. Apply Namespace to C# Files** | Renames all `namespace` declarations in the selected script folder to match the bundle name. |

#### Namespace Sync Notes

- Bundle name `englishtek.grade1.id106` → namespace `EnglishTek.Grade1.ID106`
- Use **Select Folder** to point to your individual game project's script directory before clicking button 3.
- Files with `[assembly:]` attributes are skipped automatically.
- `using` directives are preserved outside the namespace block.

> In this workspace, the packer is distributed as a zip. If it is not imported in Unity yet, follow the manual bundle/XML workflow in Section 12.

---

## 5. Interactive Games Reference

All interactives follow this scene flow:
```
Title → Instructions → Difficulty → Game → Feedback
```

Scripts load XML via `Resources.Load<TextAsset>(...)` in each game's `GameManager`.
XML files live in `Assets/Resources/XML/{id}/` in the container — they are **not bundled** with the game.
ID102 is a legacy exception that loads from `Assets/Resources/102/` (without `XML/` prefix).

---

### 5.1 ID106 — Whack-a-Mole (Grade 1, Grammar)

**Namespace:** `EnglishTek.Grade1.ID106` | **Assembly:** `ID106` | **Bundle:** `englishtek.grade1.id106`

#### XML Files (`Resources/XML/106/`)

| File | Loaded by |
|------|-----------|
| `Instruction.xml` | `Resources.Load("XML/106/Instruction")` |
| `Itembank_Practice.xml` | `Resources.Load("XML/106/Itembank_Practice")` |
| `Itembank_Workout.xml` | `Resources.Load("XML/106/Itembank_Workout")` |
| `Itembank_Quiz.xml` | `Resources.Load("XML/106/Itembank_Quiz")` |
| `Feedback.xml` | `Resources.Load("XML/106/Feedback")` |

#### XML Structure

```xml
<!-- Item bank -->
<Activity>
  <Item>
    <Question>text</Question>
    <Correct>answer</Correct>
    <Wrong1>wrong</Wrong1>
    <Wrong2>wrong</Wrong2>
  </Item>
</Activity>

<!-- Feedback -->
<Feedback>
  <Perfect>message</Perfect>   <!-- score = 100% -->
  <Average>message</Average>   <!-- score > 70% -->
  <Fail>message</Fail>         <!-- score ≤ 70% -->
</Feedback>
```

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Static data hub. `Initialize()`, `GenerateItem()`, `NextItem()`, `CheckAnswer()`, `Feedback()`. |
| `Title` | Calls `Initialize()`, disables play button 2s, `Play()` → loads Instructions. |
| `Instructions` | Displays `GameManager.Instructions`. `StartGame()` → loads Difficulty. |
| `Difficulty` | `StartGame(diff)` → sets Difficulty, calls `GenerateItem()`, loads Game. |
| `Game` | Spawns mushroom buttons with shuffled choices. Arrow keys / click. `CheckAnswer()` on hit. |
| `Mushroom` | `Click()` → calls `game.CheckAnswer(text)`. |
| `Hammer` | Follows cursor, clamped to play area, plays hit animation on click. |
| `Feedback` | Displays `GameManager.Feedback()`, submits score via `SubmitScore`. `Play()` → reloads Title. |
| `SubmitScore` | Posts score to LMS API. |

---

### 5.2 ID213 — A Day at the Beach (Grade 1, Grammar)

**Namespace:** `EnglishTek.Grade1.ID213` | **Assembly:** `ID213` | **Bundle:** `englishtek.grade1.id213`

#### XML Files (`Resources/XML/213/`)

| File | Loaded by |
|------|-----------|
| `Instruction.xml` | `Resources.Load("XML/213/Instruction")` |
| `Itembank_Practice.xml` | `Resources.Load("XML/213/Itembank_Practice")` |
| `Itembank_Workout.xml` | `Resources.Load("XML/213/Itembank_Workout")` |
| `Itembank_Quiz.xml` | `Resources.Load("XML/213/Itembank_Quiz")` |
| `Feedback.xml` | `Resources.Load("XML/213/Feedback")` |

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Static data hub. XML loaded directly via `Resources.Load("XML/213/...")`. |
| `Title` | Auto-wires Play button listener at runtime (`Awake`) in case Inspector OnClick is missing in container scene. |
| `Instructions` | Displays instructions. `StartGame()` → loads Difficulty. |
| `Difficulty` | Sets difficulty, calls `GenerateItem()`, loads Game. |
| `Game` | Countdown timer, left/right input, life system, scientist/robot animations. |
| `Feedback` | Displays feedback. `Play()` → reloads Title. |

#### Known Gotchas

| Issue | Cause | Fix |
|-------|-------|-----|
| Title script not attached in container | Stale bundle built before asmdef split (Assembly-CSharp vs ID### mismatch) | Clear cache via **TekContainer → Clear Interactive Cache → All**, then reload |
| XML not loading | `Resources.Load` path does not match the file location under `Assets/Resources/` | Verify file exists at `Resources/XML/213/{filename}` with exact case |

---

### 5.3 ID101 — Whack-a-Mushroom (Grade 2, FilipinoTek)

**Namespace:** `FilipinoTek.Grade2.ID101` | **Assembly:** `ID101` | **Bundle:** `filipinotek.grade2.id101`

> **Note:** ID101 uses a different namespace root (`FilipinoTek`) and bundle prefix (`filipinotek`) compared to all other interactives. Set `bundlePrefix = "filipinotek"` on `InteractiveController` when loading this game.

#### XML Files (`Resources/XML/101/`)

| File | Loaded by |
|------|-----------|
| `Itembanks.xml` | `Resources.Load("XML/101/Itembanks")` |
| `Dialougebanks.xml` | `Resources.Load("XML/101/Dialougebanks")` |
| `Instructions_Level1.xml` | `Resources.Load("XML/101/Instructions_Level1")` |
| `Instructions_Level2.xml` | `Resources.Load("XML/101/Instructions_Level2")` |
| `Instructions_Level3.xml` | `Resources.Load("XML/101/Instructions_Level3")` |

> ID101 uses a single itembank file for all difficulties (difficulty is encoded in the XML node path) and separate instruction files per level — unlike ID106+ which have separate itembank files per difficulty.

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Loads XML via `Resources.Load("XML/" + GetId() + "/...")`. `GetId()` returns `"101"`. |
| `Title` | Entry screen. |
| `Instuction` | Displays per-level instruction. |
| `Settings` | Difficulty/level selection. |
| `Game` | Main game loop. |
| `Trophy` | Feedback / results screen. |
| `SubmitScore` | Posts score to LMS API. |

---

### 5.4 ID232 — Grade 2 Grammar (Robot Factory)

**Namespace:** `EnglishTek.Grade2.ID232` | **Assembly:** `ID232` | **Bundle:** `englishtek.grade2.id232`

#### XML Files (`Resources/XML/232/`)

| File | Loaded by |
|------|-----------|
| `Instruction.xml` | `Resources.Load("XML/232/Instruction")` |
| `Itembank_Practice.xml` | `Resources.Load("XML/232/Itembank_Practice")` |
| `Itembank_Workout.xml` | `Resources.Load("XML/232/Itembank_Workout")` |
| `Itembank_Quiz.xml` | `Resources.Load("XML/232/Itembank_Quiz")` |
| `Feedback.xml` | `Resources.Load("XML/232/Feedback")` |

#### Scripts

| Script | Description |
|--------|--------------|
| `GameManager` | Static data hub. XML loaded via `Resources.Load`. |
| `Title` | Auto-wires Play button listener on `Awake`. |
| `Instructions` | Displays instructions. |
| `Difficulty` | Sets difficulty, loads Game. |
| `Game` | Main game loop. |
| `Feedback` | Displays feedback. `Play()` → reloads Title. |
| `SubmitScore` | Posts score to LMS API. |

---

### 5.5 ID102 — FilipinoTek Grade 2 (Legacy Flow)

**Namespace:** `FilipinoTek.Grade2.ID102` | **Assembly:** `ID102`

#### XML Files (`Resources/102/`)

| File | Loaded by | Notes |
|------|-----------|-------|
| `Itembanks.xml` | `Resources.Load("102/Itembanks")` | Legacy path format (no `XML/` prefix). |
| `Dialougebanks.xml` | `Resources.Load("102/Dialougebanks")` | Historical spelling retained in code/data. |
| `Instructions_Level1.xml` | `Resources.Load("102/Instructions_Level1")` | Per-level instructions. |
| `Instructions_Level2.xml` | `Resources.Load("102/Instructions_Level2")` | |
| `Instructions_Level3.xml` | `Resources.Load("102/Instructions_Level3")` | |

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Uses level-based flow (`Level 1/2/3`) and loads `Itembanks` + `Dialougebanks` via legacy paths. |
| `Title` | Entry screen. |
| `Instuction` | Instruction scene (legacy filename spelling). |
| `Settings` | Level selection scene. |
| `Game` | Main gameplay loop. |
| `Trophy` | Results/feedback scene. |
| `SubmitScore` | Score submission coroutine. |

---

### 5.6 ID313 — Grade 1 Grammar

**Namespace:** `EnglishTek.Grade1.ID313` | **Assembly:** `ID313`

#### XML Files (`Resources/XML/313/`)

| File | Loaded by |
|------|-----------|
| `Instruction.xml` | `Resources.Load("XML/313/Instruction")` |
| `Itembank_Practice.xml` | `Resources.Load("XML/313/Itembank_Practice")` |
| `Itembank_Workout.xml` | `Resources.Load("XML/313/Itembank_Workout")` |
| `Itembank_Quiz.xml` | `Resources.Load("XML/313/Itembank_Quiz")` |
| `Feedback.xml` | `Resources.Load("XML/313/Feedback")` |

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Same static model as ID106/ID213/ID232 (`Initialize`, `GenerateItem`, `NextItem`, `CheckAnswer`, `Feedback`). |
| `Title` | Initializes manager and opens instruction flow. |
| `Instructions` | Displays `GameManager.Instructions`. |
| `Difficulty` | Sets Practice/Workout/Quiz. |
| `Game` | Main gameplay scene. |
| `Feedback` | Displays score feedback and loops back to `Title`. |
| `SubmitScore` | LMS submission logic. |

> Note: `ID313` exists in scripts/resources in this repository but is not currently preserved in `Assets/link.xml` by default. Add `<assembly fullname="ID313" preserve="all"/>` before Android release builds that include this interactive.

---

## 6. Server Layout & Bundle Naming

```
ServerData/Interactive/
├── Grade 1/
│   ├── catalog.json
│   ├── grammar/
│   │   └── unit1/
│   │       ├── ID106/
│   │       │   ├── englishtek.grade1.id106.assets
│   │       │   ├── englishtek.grade1.id106.scenes
│   │       │   ├── thumb.png
│   │       │   └── home.png
│   │       └── ID213/
│   │           ├── englishtek.grade1.id213.assets
│   │           ├── englishtek.grade1.id213.scenes
│   │           ├── thumb.png
│   │           └── home.png
│   ├── listening/
│   ├── Reading/
│   └── Virtual Dialogue/
└── Grade 2/
    ├── catalog.json
    └── grammar/
        └── unit1/
            └── ID232/
                ├── englishtek.grade2.id232.assets
                ├── englishtek.grade2.id232.scenes
                ├── thumb.png
                └── home.png
```

#### Bundle Naming Rule

```
{bundlePrefix}.{grade}.{id-lowercase}.assets
{bundlePrefix}.{grade}.{id-lowercase}.scenes
```

`bundlePrefix` defaults to `englishtek` but can be changed in the `InteractiveController` Inspector.

Examples:
- `englishtek.grade1.id106.assets`
- `filipinotek.grade2.id101.scenes`

---

## 7. Catalog JSON Schema

```json
{
  "interactives": [
    {
      "id": "ID106",
      "title": "Whack-a-Mole",
      "category": "grammar",
      "unit": "unit1",
      "image": "thumb.png",
      "home": "home.png",
      "enabled": true
    }
  ]
}
```

---

## 8. Data Flow — End to End

```
1.  Container scene loads
2.  InteractiveController.Start() → HTTP GET {grade}/catalog.json
3.  Parse JSON → List<InteractiveCatalogEntry>
4.  CatalogUpdated event → InteractiveCatalogMenu renders buttons
5.  User taps an interactive entry
6.  RequestGameLoad("ID106")
7.  Resolve: folder = Grade 1/grammar/unit1/ID106/
             bundleBase = englishtek.grade1.id106
8.  Check local cache:
      Hit  → load from InteractiveCache/
      Miss → HTTP GET .assets + .scenes → save to cache
9.  GameSession.CurrentAssetBundle = loadedAssets
    GameSession.CurrentSceneBundle = loadedScenes
    GameSession.ContainerSceneName = "ContainerScene"
10. Reconstruct manifest from scene bundle → find first scene name ("Title")
    Parse gameId from catalog entry (e.g. "ID106" → 106)
    GameSession.CurrentManifest = new InteractiveManifest { firstSceneName, gameId }
    XML is NOT loaded from the bundle — it is always read from Resources at runtime
11. ContainerReturnOverlay.EnsureExists() → DontDestroyOnLoad
12. SceneManager.LoadScene("Title")
13. Game runs:
      GameManager uses Resources.Load("XML/{id}/...")
      → Instructions → Difficulty → Game → Feedback
14. Feedback → SubmitScore → LMS API
15. User presses Back on overlay
      → GameSession.CleanUp() → unloads bundles
      → SceneManager.LoadScene("ContainerScene")
16. Back to step 2 (catalog already loaded)
```

---

## 9. UI Animation System

### Setup: UIGroup

1. Add `UIGroup` component to any UI container GameObject.
2. Set **Animation Type** and optionally **Start Hidden**.
3. Call `Show()` / `Hide()` from code or assign to buttons.

### Setup: CatalogMenuNavigator (animated catalog nav)

Use `CatalogMenuNavigator` **instead of** `InteractiveCatalogMenu`.

```
Canvas
├── CategoryGroup    ← add UIGroup (Fade, startHidden: false)
├── UnitGroup        ← add UIGroup (Fade, startHidden: true)
└── EntryGroup       ← add UIGroup (Fade, startHidden: true)
```

In `CatalogMenuNavigator` inspector:
- **Category Group** → drag CategoryGroup
- **Unit Group** → drag UnitGroup
- **Entry Group** → drag EntryGroup

Wire a back button → `CatalogMenuNavigator.GoBack()`.

### Animation Type Quick Reference

| Value | Show | Hide |
|-------|------|------|
| `None` | Snap visible | Stays on screen |
| `Fade` | Fade in | Fade out → deactivate |
| `SlideFromLeft/Right/Top/Bottom` | Slide + fade in | Slide + fade out |
| `ScalePop` | Scale spring in | Scale out |
| `FadeScalePop` | Scale 0.7→1 + fade | Scale + fade out |
| `SlideUp` | Rise + fade in | Slides up, stays visible |

---

<!-- ## 10. LMS Score Submission (TODO)

**File:** `Assets/Core/InteractiveScripts/ID###_Scripts/SubmitScore.cs`

```
POST https://tekteachlms-api.com/api/Student/{studentId}/class/{classId}/interactive/{gameId}/score
```

```json
{
  "dateTimeSubmitted": "MM/dd/yyyy HH:mm:ss tt",
  "isCompleted": false,
  "score": 8,
  "gameLevelId": 1
}
```

`gameLevelId`: 1 = Practice, 2 = Quiz/Workout.

| Platform | Behaviour |
|----------|-----------|
| Editor | Dummy student/class/game IDs. Auth key = `"Test"`. |
| WebGL | Reads `sid`, `gid`, `cid` from URL query params. Auth from URL fragment `#`. |
| Android | Not implemented. |

**Always call via coroutine:**
```csharp
StartCoroutine(submitScore.PostScores(diff, score));
``` -->

---

## 11. Common Issues & Fixes

| Symptom | Cause | Fix |
|---------|-------|-----|
| "Associated script cannot be loaded" in Inspector | Bundle built from old Assembly-CSharp; container uses ID### asmdef | Clear cache for that ID, rebuild bundle from the same project that was used to create the asmdef |
| Play button works but scene does not change | OnClick not wired in bundled scene | Title.Awake() auto-wires if no persistent listeners exist |
| XML returns null at runtime | Wrong path or file not imported to Resources | Verify `Assets/Resources/XML/{id}/{filename}.xml` exists; re-run **Import XML to Container** |
| Stale bundle loaded after update | Local cache still has old file | **TekContainer → Clear Interactive Cache → All** or bump `bundleVersion` in catalog |
| Script attached but Play does nothing | Null reference throws before `LoadScene` | Check Console for NullReferenceException; ensure all Inspector refs are assigned |
| Android build crashes on interactive load | IL2CPP stripped interactive assemblies | Ensure `link.xml` preserves all ID### assemblies |
| Bundle loads but scenes are empty | Bundle built for wrong platform (e.g. WebGL bundle on Android) | Rebuild bundle for target platform, clear cache |
| ID101 bundle not found (404) | bundlePrefix still set to `englishtek` | Set `bundlePrefix = "filipinotek"` on `InteractiveController` for Grade 2 scene |

---

## 12. Adding a New Interactive — Step by Step

### Step 1: Define ID, Product, Grade

**Q: What should I lock first?**
A: Lock `ID###`, product line (`englishtek` or `filipinotek`), and grade (`grade1`/`grade2`).

**Q: Why is this first?**
A: The same values drive namespace, asmdef name, bundle names, server path, and catalog entry.

### Step 2: Create Script Folder, asmdef, Namespace

1. Create `Assets/Core/InteractiveScripts/ID###_Scripts/`.
2. Add `ID###.asmdef`.
3. Use namespace format matching current project conventions:
   - `EnglishTek.Grade1.ID###`
   - `EnglishTek.Grade2.ID###`
   - `FilipinoTek.Grade2.ID###`

**Q: Why do this before coding?**
A: Bundle scene scripts resolve by assembly + type name. Wrong asmdef/namespace leads to missing-script errors.

### Step 3: Update `link.xml`

Add:
```xml
<assembly fullname="ID###" preserve="all"/>
```

**Q: Why is this required?**
A: IL2CPP stripping can remove classes that are only referenced in AssetBundle scenes.

### Step 4: Implement Game Scripts Using Current Pattern

Create at least: `GameManager`, `Title`, `Instructions` (or `Instuction` for legacy flow), `Difficulty`/`Settings`, `Game`, `Feedback`/`Trophy`, `SubmitScore`.

Use direct `Resources.Load` in `GameManager`:
```csharp
string path = "XML/###/Itembank_" + difficulty;
TextAsset asset = Resources.Load<TextAsset>(path);
if (asset == null) { Debug.LogError("Missing XML: " + path); return; }

XmlDocument doc = new XmlDocument();
doc.LoadXml(asset.text);
```

**Q: Why not use XmlLoader/IXmlLoadable?**
A: Those helpers are not present in the current repository. Existing interactives all use direct loading.

### Step 5: Build the Scene Flow

Required flow:
`Title -> Instructions -> Difficulty/Settings -> Game -> Feedback/Trophy`

**Q: Why must first scene be Title?**
A: `InteractiveController` derives and loads the first scene from the scenes bundle and expects this game-entry scene behavior.

### Step 6: Prepare XML Files

Standard EnglishTek layout:

| File | Load path |
|------|-----------|
| `Instruction.xml` | `XML/###/Instruction` |
| `Feedback.xml` | `XML/###/Feedback` |
| `Itembank_Practice.xml` | `XML/###/Itembank_Practice` |
| `Itembank_Workout.xml` | `XML/###/Itembank_Workout` |
| `Itembank_Quiz.xml` | `XML/###/Itembank_Quiz` |

Legacy FilipinoTek exceptions:
- ID101 uses `XML/101/*` with `Dialougebanks` naming
- ID102 uses `102/*` (no `XML/` prefix)

**Q: Why is exact casing important?**
A: Case-sensitive paths break at runtime on Android/Linux.

### Step 7: Create/Configure `InteractiveManifest`

Set:
- `gameId` numeric value
- `firstSceneName = "Title"`
- `allScenes` list
- `prefabsToInclude` if needed
- `xmlConfigs` entries if using the packer import button

**Q: Why manifest if XML is not bundled?**
A: It still tracks scenes/prefabs and supports XML import workflows where the editor tool is available.

### Step 8: Build Bundles

Bundle naming rule:
```text
{bundlePrefix}.{grade}.{id-lowercase}.assets
{bundlePrefix}.{grade}.{id-lowercase}.scenes
```

Assign:
- scenes -> `.scenes`
- manifest + prefabs -> `.assets`
- never XML files

Build per target platform (Windows/WebGL/Android).

**Q: Why per-platform builds?**
A: AssetBundles are platform-specific binaries.

### Step 9: Import/Copy XML into Container

If `InteractivePacker` is available in Unity, use import.
If not, manually copy XML files to the container `Assets/Resources/...` location.

**Q: Why copy XML into container?**
A: Runtime loading uses `Resources.Load` from container assets, not from bundle payload.

### Step 10: Deploy to Server

Upload to folder (example):
`ServerData/Interactive/Grade 1/grammar/unit1/ID###/`

Upload:
- `{bundleBase}.assets`
- `{bundleBase}.scenes`
- `thumb.png`
- `home.png`

**Q: Why include thumb/home?**
A: Catalog UI and carousel background use them for entry visuals.

### Step 11: Add/Update `catalog.json`

Example entry:
```json
{
  "id": "ID###",
  "title": "My Game Title",
  "category": "grammar",
  "unit": "unit1",
  "image": "thumb.png",
  "home": "home.png",
  "enabled": true,
  "bundleVersion": "1"
}
```

**Q: Why include `bundleVersion`?**
A: `InteractiveController` uses it in the cache key so users fetch updated bundles after releases.

### Step 12: Validate End-to-End

Check:
- Catalog entry appears
- Tap entry loads Title
- XML content shows in instructions/game
- Gameplay reaches feedback/trophy
- Score submission works
- Back overlay returns to container
- No Console errors

**Q: Why clear cache during testing?**
A: Cached bundles/catalog/images can mask server-side changes.

Use: `TekContainer -> Clear Interactive Cache -> All`.

### Troubleshooting Quick Q&A

**Q: Scene loads white/empty. Why?**
A: Usually wrong-platform bundle or stale cache.

**Q: Script missing in loaded scene. Why?**
A: Assembly mismatch or missing `link.xml` preserve entry.

**Q: XML is null. Why?**
A: Wrong `Resources.Load` path, filename case mismatch, or file not copied into container.

**Q: Bundles 404 on server. Why?**
A: Folder/category/unit/ID path or bundle base name does not match catalog/controller conventions.
