# EnglishTek Container — Codebase Documentation

> Generated: April 16, 2026  
> Unity project. Namespace root: `EnglishTek.Core` (container) and `EnglishTek.Grade1.ID###` (interactives).

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Folder Structure](#2-folder-structure)
3. [Architecture Overview](#3-architecture-overview)
4. [Core System — Scripts](#4-core-system--scripts)
   - 4.1 [GameSession](#41-gamesession)
   - 4.2 [InteractiveController](#42-interactivecontroller)
   - 4.3 [InteractiveManifest](#43-interactivemanifest)
   - 4.4 [InteractiveCatalogEntry & InteractiveCatalogDocument](#44-interactivecatalogentry--interactivecatalogdocument)
   - 4.5 [InteractiveCatalogMenu](#45-interactivecatalogmenu)
   - 4.6 [CatalogMenuNavigator](#46-catalogmenunavigator)
   - 4.7 [ContainerReturnOverlay](#47-containerreturnoverlay)
   - 4.8 [UIGroup](#48-uigroup)
   - 4.9 [UIGroupAnimation (enum)](#49-uigroupanimation-enum)
5. [Catalog Subsystem](#5-catalog-subsystem)
   - 5.1 [CatalogFilter](#51-catalogfilter)
   - 5.2 [CatalogStringHelper](#52-catalogstringhelper)
   - 5.3 [CatalogThumbnailLoader](#53-catalogthumbnailloader)
   - 5.4 [CatalogUiFactory](#54-cataloguifactory)
6. [UI Components](#6-ui-components)
   - 6.1 [ArcCarousel](#61-arccarousel)
   - 6.2 [CarouselHomeBackground](#62-carouselhomebackground)
7. [Interactive Games](#7-interactive-games)
   - 7.1 [ID106 — Whack-a-Mole (Grade 1, Grammar)](#71-id106--whack-a-mole-grade-1-grammar)
   - 7.2 [ID213 — A Day at the Beach (Grade 1, Grammar)](#72-id213--a-day-at-the-beach-grade-1-grammar)
8. [Server Data Layout](#8-server-data-layout)
9. [Catalog JSON Schema](#9-catalog-json-schema)
10. [Asset Bundle Naming Convention](#10-asset-bundle-naming-convention)
11. [Data Flow — End to End](#11-data-flow--end-to-end)
12. [Adding a New Interactive](#12-adding-a-new-interactive)
13. [Adding a New Grade](#13-adding-a-new-grade)
14. [LMS Score Submission (SubmitScore)](#14-lms-score-submission-submitscore)

---

## 1. Project Overview

**EnglishTek Container** is a Unity WebGL / multi-platform shell that:

- Displays a menu of educational interactive games fetched from a remote HTTP server.
- Downloads, caches, and launches individual games as **AssetBundles** at runtime.
- Returns the player to the container menu when a game session ends.
- Submits scores to a remote LMS API (`tekteachlms-api.com`).

Games are completely decoupled from the container — they live in separate AssetBundles on the server and are never compiled into the container build. The container only holds the loading / routing infrastructure.

---

## 2. Folder Structure

```
EnglishTekContainer/
├── Assets/
│   ├── Core/
│   │   ├── Scripts/
│   │   │   ├── GameSession.cs              ← Global session state (static)
│   │   │   ├── UIGroup.cs                  ← Animated show/hide for UI panels
│   │   │   ├── ContainerReturnOverlay.cs   ← Persistent "Back to Menu" button
│   │   │   ├── Interactive/
│   │   │   │   ├── InteractiveController.cs        ← Main coordinator
│   │   │   │   ├── InteractiveCatalogMenu.cs       ← Built-in catalog UI
│   │   │   │   ├── CatalogMenuNavigator.cs         ← Animated nav extension
│   │   │   │   ├── InteractiveCatalogEntry.cs      ← Data model
│   │   │   │   └── InteractiveManifest.cs          ← ScriptableObject for a game
│   │   │   ├── Catalog/
│   │   │   │   ├── CatalogFilter.cs                ← Filtering helpers
│   │   │   │   ├── CatalogStringHelper.cs          ← Normalization helpers
│   │   │   │   ├── CatalogThumbnailLoader.cs       ← Async image downloader
│   │   │   │   └── CatalogUiFactory.cs             ← Procedural UI builders
│   │   │   └── UI/
│   │   │       ├── ArcCarousel.cs                  ← Swipeable arc carousel
│   │   │       └── CarouselHomeBackground.cs       ← Per-entry background swap
│   │   └── InteractiveScripts/
│   │       ├── ID106_Scripts/              ← Whack-a-Mole game scripts
│   │       └── ID213_Scripts/              ← A Day at the Beach game scripts
│   └── TextMesh Pro/
├── ServerData/
│   └── Interactive/
│       ├── Grade 1/
│       │   ├── catalog.json
│       │   ├── grammar/unit1/ID106/  ← bundles + images
│       │   └── listening/...
│       └── Grade 2/
│           ├── catalog.json
│           └── grammar/unit1/...
└── ProjectSettings/
```

---

## 3. Architecture Overview

```
InteractiveController  (MonoBehaviour, one per scene)
    │
    ├── fetches catalog.json  ──►  grade/<grade>/catalog.json
    │       │
    │       └── populates List<InteractiveCatalogEntry>
    │
    ├── exposes CatalogUpdated event
    │       │
    │       └── InteractiveCatalogMenu / CatalogMenuNavigator  listen
    │               │
    │               └── renders category / unit / entry buttons
    │
    └── RequestGameLoad(gameId)
            │
            ├── resolves folder URL from catalog entry or default path
            ├── downloads .assets bundle  (with local cache)
            ├── downloads .scenes bundle  (with local cache)
            ├── sets GameSession.CurrentAssetBundle / CurrentSceneBundle
            ├── creates InteractiveManifest (scene-bundle fallback)
            ├── spawns ContainerReturnOverlay (DontDestroyOnLoad)
            └── SceneManager.LoadScene(manifest.firstSceneName)
                    │
                    └── Game runs. ContainerReturnOverlay watches for
                        scene named "Title" and shows the back button.
                        Pressing it calls GameSession.CleanUp() and
                        reloads the original container scene.
```

---

## 4. Core System — Scripts

### 4.1 `GameSession`

**File:** `Assets/Core/Scripts/GameSession.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `static class`

Global singleton-like state bag that bridges the container and the loaded game.

| Member | Type | Description |
|--------|------|-------------|
| `CurrentManifest` | `InteractiveManifest` | The manifest of the currently running game. |
| `CurrentAssetBundle` | `AssetBundle` | The `.assets` bundle (holds prefabs, textures, XML configs). |
| `CurrentSceneBundle` | `AssetBundle` | The `.scenes` bundle (holds game scenes). |
| `ContainerSceneName` | `string` | Name of the container scene to return to when the game exits. |
| `CleanUp()` | `static void` | Unloads both bundles, nulls all fields, calls `Resources.UnloadUnusedAssets()`. |

**Usage pattern (from game scripts):**
```csharp
// Reading XML item bank from the bundle
string xmlContent = GameSession.CurrentManifest.GetXMLText("ItemBankPractice_ET1ID106");
```

---

### 4.2 `InteractiveController`

**File:** `Assets/Core/Scripts/Interactive/InteractiveController.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour`

The central coordinator. One instance lives on a GameObject in the container scene.

#### Inspector Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `serverRoot` | `string` | `http://localhost:8080/Interactive/` | Base URL for all server requests. |
| `grade` | `string` | `grade1` | Which grade's catalog and bundles to load. Change to `grade2` for Grade 2. |
| `catalogFileName` | `string` | `catalog.json` | File name appended after the grade path to form the catalog URL. |
| `defaultCategory` | `string` | _(empty)_ | Fallback category when building default folder paths. |
| `defaultUnit` | `string` | _(empty)_ | Fallback unit when building default folder paths. |
| `refreshCatalogOnStart` | `bool` | `true` | Whether to auto-fetch the catalog in `Start()`. |
| `overlayPrefab` | `ContainerReturnOverlay` | `null` | Optional prefab for the back button overlay. Falls back to procedural if null. |
| `overlayButtonCorner` | `OverlayButtonCorner` | `TopLeft` | Screen corner for the back button. |
| `overlayButtonPadding` | `Vector2` | `(10, 10)` | Padding from the chosen corner. |

#### Public API

| Member | Description |
|--------|-------------|
| `AvailableInteractives` | `IReadOnlyList<InteractiveCatalogEntry>` — all enabled entries from the last catalog fetch. |
| `CatalogUpdated` | `event Action<IReadOnlyList<InteractiveCatalogEntry>>` — fired when catalog loads successfully. |
| `CatalogLoadFailed` | `event Action<string>` — fired with an error message on failure. |
| `RefreshCatalog()` | Re-fetches the catalog JSON from the server. |
| `RequestGameLoad(string gameId)` | Downloads and launches the game with the given ID. |
| `ResolveCatalogAssetUrl(entry, assetPath)` | Resolves a relative image/asset path to an absolute URL for a given catalog entry. |

#### Internal Flow — `RequestGameLoad`

1. Looks up `gameId` in `availableInteractives` (case-insensitive, normalised to `ID###`).
2. Builds a `DownloadTarget` struct with resolved grade, folder, and bundle base name.
3. Constructs asset URL: `{serverRoot}/{folder}/{bundleBase}.assets`
4. Constructs scene URL: `{serverRoot}/{folder}/{bundleBase}.scenes`
5. Calls `LoadBundleWithLocalCacheRoutine` for each — tries local disk cache first, falls back to HTTP download, saves to disk.
6. If asset bundle loads OK, attempts to find an `InteractiveManifest` asset inside it. Due to potential assembly mismatches with externally built bundles, falls back to reading the first scene name directly from the scene bundle.
7. Sets `GameSession.*`, ensures `ContainerReturnOverlay` exists, calls `SceneManager.LoadScene`.

#### URL / Path Conventions

| Helper | Description |
|--------|-------------|
| `BuildCatalogUrl()` | `{root}{grade}/{catalogFileName}` |
| `BuildFolderUrl(folder)` | `{root}{folder}/` |
| `BuildDefaultFolderPath(grade, cat, unit, id)` | `grade/category/unit/id` (empty parts omitted) |
| `BuildDefaultBundleBaseName(grade, id)` | `englishtek.{grade}.{id}` (all lowercase) |
| `NormalizePathPart(value)` | Trims, collapses backslashes, strips surrounding slashes. |
| `NormalizeLookupId(value)` | Uppercases, prepends `ID` if missing. |
| `EncodePathSegments(path)` | URL-encodes each `/`-separated segment (`Uri.EscapeDataString`). |

---

### 4.3 `InteractiveManifest`

**File:** `Assets/Core/Scripts/Interactive/InteractiveManifest.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `ScriptableObject` (`[CreateAssetMenu]`)

Describes the contents of one interactive game bundle. Created inside each game project and baked into the `.assets` bundle.

| Field | Type | Description |
|-------|------|-------------|
| `bundleName` | `string` | The AssetBundle name this manifest belongs to. |
| `firstSceneName` | `string` | Scene to load when the game starts (e.g. `"Title"`). |
| `allScenes` | `List<Object>` | All scene assets included in the bundle (for build tracking). |
| `xmlConfigs` | `List<NamedXML>` | Key → TextAsset XML pairs for item banks, instructions, feedback. |
| `prefabsToInclude` | `GameObject[]` | Prefabs that must be included in the asset bundle. |
| `GetXMLText(key)` | `string` | Returns the `InnerText` of the `TextAsset` matching the given key, or `null`. |

**`NamedXML`** (nested serializable class):

| Field | Description |
|-------|-------------|
| `key` | String key used to look up the XML at runtime (e.g. `"ItemBankPractice_ET1ID106"`). |
| `xmlFile` | The `TextAsset` containing the raw XML content. |

---

### 4.4 `InteractiveCatalogEntry` & `InteractiveCatalogDocument`

**File:** `Assets/Core/Scripts/Interactive/InteractiveCatalogEntry.cs`  
**Namespace:** `EnglishTek.Core`

`InteractiveCatalogEntry` — a single entry in `catalog.json`:

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` | Unique identifier, e.g. `"ID106"`. Used for bundle lookup. |
| `title` | `string` | Human-readable display name. |
| `image` | `string` | Relative or absolute path to the thumbnail image. |
| `home` | `string` | Relative or absolute path to the full-bleed home background image. |
| `category` | `string` | Lesson category (e.g. `"grammar"`, `"listening"`). |
| `unit` | `string` | Sub-category unit (e.g. `"unit1"`). |
| `folder` | `string` | Override server folder path. If empty, default path is computed. |
| `grade` | `string` | Override grade for this entry. If empty, uses controller's `grade`. |
| `bundleBaseName` | `string` | Override bundle file name base. If empty, computed from grade + id. |
| `enabled` | `bool` | If `false`, this entry is skipped when building the UI. |
| `DisplayName` | `string` | Returns `title` if set, otherwise `id`. |

`InteractiveCatalogDocument` — the root JSON wrapper:

```json
{
  "interactives": [ /* array of InteractiveCatalogEntry */ ]
}
```

---

### 4.5 `InteractiveCatalogMenu`

**File:** `Assets/Core/Scripts/Interactive/InteractiveCatalogMenu.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour`

Renders the catalog as a three-level navigation: **Category → Unit → Entry**.

#### Inspector Fields

| Field | Description |
|-------|-------------|
| `controller` | Reference to `InteractiveController`. Auto-found if null. |
| `targetCanvas` | Canvas to attach procedural UI to. Auto-found if null. |
| `refreshOnStart` | Re-fetches catalog on `Start()` if true. |
| `showBuiltInCatalogPanel` | If true, generates a dark overlay panel with built-in UI elements procedurally. |
| `autoGenerateLessonButtons` | If true, creates category buttons automatically from catalog data. |
| `unitButtonContainer` | Parent transform for unit buttons (uses prefab path). |
| `unitButtonPrefab` | Prefab instantiated for each unit button. |
| `interactiveButtonContainer` | Parent transform for entry buttons (uses prefab path). |
| `interactiveButtonPrefab` | Prefab instantiated for each interactive entry button. |
| `autoSelectFirstUnit` | Automatically selects the first unit when a category is chosen. |
| `panelSize` | Size of the procedural catalog panel. |
| `anchoredPosition` | Anchored position of the procedural panel. |
| `entryHomeBackground` | Reference to a `CarouselHomeBackground` for full-bleed entry backgrounds. |

#### Public API

| Method | Description |
|--------|-------------|
| `SelectLesson(string category)` | Filters entries to the given category. Normalizes the string. |
| `SelectUnit(string unit)` | Filters entries to the given unit within the current category. |
| `SelectGrammarLesson()` | Convenience — calls `SelectLesson("grammar")`. |
| `SelectReadingLesson()` | Convenience — calls `SelectLesson("reading")`. |
| `SelectListeningLesson()` | Convenience — calls `SelectLesson("listening")`. |
| `SelectVirtualDialogueLesson()` | Convenience — calls `SelectLesson("virtual dialogue")`. |
| `virtual GoBack()` | Override in subclass. Base is no-op. |

#### Internal Flow

1. **`Awake`** — wires up `InteractiveController` reference, finds `Canvas`, attaches `CatalogThumbnailLoader`.
2. **`OnEnable`** — subscribes to `CatalogUpdated` and `CatalogLoadFailed` events.
3. **`Start`** — if catalog already populated, calls `HandleCatalogUpdated`; else calls `RefreshCatalog()`.
4. **`HandleCatalogUpdated`** — clears lists, rebuilds category buttons, applies default category filter.
5. **`ApplyCategoryFilter(category)`** — sets `selectedCategory`, builds unit buttons, optionally auto-selects first unit, calls virtual `OnCategoryApplied()`, calls `RenderFilteredEntries()`.
6. **`SelectUnit(unit)`** — sets `selectedUnit`, calls virtual `OnUnitSelected()`, calls `RenderFilteredEntries()`.
7. **`RenderFilteredEntries()`** — creates entry buttons (prefab or procedural) for entries matching current category + unit filter.

---

### 4.6 `CatalogMenuNavigator`

**File:** `Assets/Core/Scripts/Interactive/CatalogMenuNavigator.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour` (extends `InteractiveCatalogMenu`)

Adds **animated transitions** between the three navigation levels using `UIGroup`.

#### Additional Inspector Fields

| Field | Description |
|-------|-------------|
| `categoryGroup` | `UIGroup` shown at the start. Hidden when a category is picked. |
| `unitGroup` | `UIGroup` shown after category selection. |
| `entryGroup` | `UIGroup` shown after unit selection. |

#### Overrides

| Method | Behaviour |
|--------|-----------|
| `OnCategoryApplied()` | Hides `categoryGroup`, then shows `unitGroup`. |
| `OnUnitSelected()` | Disables unit buttons, hides `unitGroup`, then shows `entryGroup`. |
| `GoBack()` | entry visible → hides entry, shows unit. unit visible → hides unit, shows category. |

#### Additional Public Methods

| Method | Description |
|--------|-------------|
| `GoToCategories()` | Instantly hides entry group, fades out unit group, shows category group. Wire to a "back to start" button. |

---

### 4.7 `ContainerReturnOverlay`

**File:** `Assets/Core/Scripts/ContainerReturnOverlay.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour`

A **persistent (DontDestroyOnLoad) singleton** that renders a back button overlay inside the active game scene.

#### How It Works

- `InteractiveController` calls `ContainerReturnOverlay.EnsureExists(prefab, corner, padding)` just before loading the game scene.
- The overlay monitors `SceneManager.GetActiveScene().name` every frame. It **shows itself only when the active scene is named `"Title"`** (the game's first scene). It hides otherwise.
- Pressing the back button calls `ReturnToContainer()`:
  1. Calls `GameSession.CleanUp()` (unloads both AssetBundles, frees memory).
  2. Loads `GameSession.ContainerSceneName` to return to the menu.

#### Inspector Fields (Procedural Mode)

| Field | Default | Description |
|-------|---------|-------------|
| `backButton` | `null` | Assign a prefab Button to use instead of the procedural one. |
| `backButtonLabel` | `"< Menu"` | Text on the procedural button. |
| `buttonSize` | `(120, 44)` | Size of the procedural button. |
| `buttonPadding` | `(10, 10)` | Distance from the screen corner. |
| `buttonCorner` | `TopLeft` | Which corner to place the button in. |
| `buttonColor` | Dark semi-transparent | Background color. |
| `labelColor` | White | Text color. |
| `labelFontSize` | `18` | Font size. |

#### `OverlayButtonCorner` Enum

```
TopLeft | TopRight | BottomLeft | BottomRight
```

---

### 4.8 `UIGroup`

**File:** `Assets/Core/Scripts/UIGroup.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour`

Attach to any UI container `GameObject` to give it animated show/hide support. Manages a `CanvasGroup` automatically.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `animationIn` | `Fade` | Animation type used by `Show()`. `None` = instant snap. |
| `animationOut` | `Fade` | Animation type used by `Hide()`. `None` = group stays visible. |
| `duration` | `0.25` | Duration of all animations in seconds. |
| `slideDistance` | `60` | Pixels offset for slide animations. |
| `startHidden` | `false` | Deactivate and alpha-zero on `Awake`. |

#### Public API

| Method | Description |
|--------|-------------|
| `Show(Action onComplete = null)` | Animates the group into view. |
| `Hide(Action onComplete = null)` | Animates the group out of view, then deactivates it. |
| `HideWith(UIGroupAnimation, Action)` | Hide with a one-time animation override. |
| `ShowImmediate()` | Snap visible with no animation. |
| `HideImmediate()` | Snap hidden and deactivate with no animation. |
| `IsVisible` | `bool` property — current visibility state. |

---

### 4.9 `UIGroupAnimation` (enum)

| Value | Show Behaviour | Hide Behaviour |
|-------|---------------|----------------|
| `None` | Instant snap visible | Group stays visible (no hide) |
| `Fade` | Alpha 0 → 1 | Alpha 1 → 0 |
| `SlideFromLeft` | Slides in from left + fade | Slides out to left + fade |
| `SlideFromRight` | Slides in from right + fade | Slides out to right + fade |
| `SlideFromTop` | Slides in from top + fade | Slides out to top + fade |
| `SlideFromBottom` | Slides in from below + fade | Slides out to below + fade |
| `ScalePop` | Scale 0 → 1 (EaseOutBack) | Scale 1 → 0 |
| `FadeSlideUp` | Rise from below + fade in | Fall down + fade out |
| `FadeSlideDown` | Drop from above + fade in | Rise up + fade out |
| `FadeScalePop` | Scale 0.7→1 + fade (EaseOutBack) | Scale 1→0.7 + fade |
| `SlideUp` | (show: same as FadeSlideUp) | Slides upward; **stays visible** at new position |

Easing: show uses `EaseOutQuad`, hide uses `EaseInQuad`. `ScalePop` / `FadeScalePop` use `EaseOutBack` for the spring overshoot.

---

## 5. Catalog Subsystem

### 5.1 `CatalogFilter`

**File:** `Assets/Core/Scripts/Catalog/CatalogFilter.cs`  
**Access:** `internal static` (not exposed outside `EnglishTek.Core`)

| Method | Description |
|--------|-------------|
| `HasCategory(interactives, category)` | Returns `true` if any entry matches the given normalized category. |
| `BuildUniqueCategories(interactives)` | Returns ordered list of unique normalized categories. |
| `BuildUnitsForCategory(interactives, category)` | Returns ordered list of unique normalized units for a category. |

Empty/null categories and units default to `"general"`.

---

### 5.2 `CatalogStringHelper`

**File:** `Assets/Core/Scripts/Catalog/CatalogStringHelper.cs`  
**Access:** `internal static`

| Method | Description |
|--------|-------------|
| `NormalizeCategory(value)` | `Trim().ToLowerInvariant()` |
| `NormalizeUnit(value)` | `Trim().ToLowerInvariant()` |
| `FormatCategoryLabel(category)` | Title-cases normalized category for display. |
| `FormatUnitLabel(unit)` | Title-cases normalized unit for display. |

---

### 5.3 `CatalogThumbnailLoader`

**File:** `Assets/Core/Scripts/Catalog/CatalogThumbnailLoader.cs`  
**Type:** `MonoBehaviour` (`[DisallowMultipleComponent]`, `internal`)

Downloads thumbnail images from the server and applies them to `RawImage` components asynchronously. Attached automatically by `InteractiveCatalogMenu`.

| Method | Description |
|--------|-------------|
| `TryLoadThumbnail(entry, target, controller)` | Starts a coroutine to download `entry.image` and set it on `target`. |
| `StopAll()` | Stops all in-flight download coroutines. Called in `OnDisable`. |
| `ClearTextures()` | Destroys all downloaded `Texture2D` instances to free GPU memory. |

---

### 5.4 `CatalogUiFactory`

**File:** `Assets/Core/Scripts/Catalog/CatalogUiFactory.cs`  
**Access:** `internal static`

Procedural UI builders used by `InteractiveCatalogMenu` when `showBuiltInCatalogPanel = true`.

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateTextElement(name, parent, fontSize)` | `Text` | Creates a Unity UI `Text` with Arial font. |
| `CreateThumbnailElement(parent)` | `RawImage` | Creates a left-anchored thumbnail slot. |
| `CreateCategoryRow(parent)` | `RectTransform` | Creates a horizontal layout group for category tabs. |
| `CreateEntriesContainer(parent)` | `RectTransform` | Creates a vertical layout group for entry buttons. |

---

## 6. UI Components

### 6.1 `ArcCarousel`

**File:** `Assets/Core/Scripts/UI/ArcCarousel.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour` (`[ExecuteAlways]`, `[RequireComponent(typeof(Image))]`)

A **revolver-style horizontal arc carousel**. Items are children of the `ArcCarousel` GameObject. Swiping rotates them along an arc and snaps to the nearest item.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `itemWidth` | `120` | Width of each carousel item. |
| `itemHeight` | `60` | Height of each carousel item. |
| `spacing` | `16` | Gap between items. |
| `arcHeight` | `50` | Pixels the center item rises above edge items. |
| `visibleRadius` | `3` | Items within this range from center are on the arc; beyond are flat. |
| `edgeScale` | `0.75` | Scale applied to edge items (center = 1.0). |
| `snapSpeed` | `10` | Lerp speed after drag ends. |
| `dragStep` | `0` | Pixels per item step. Auto-computes from `itemWidth + spacing` if 0. |

#### Public API

| Member | Description |
|--------|-------------|
| `OnCenterIndexChanged` | `event Action<int>` — fires when the centered item index changes. |
| `GoToIndex(int)` | Animates to a specific item index. |
| `Rebuild()` | Forces an immediate re-read of children and repositions. |

#### Layout

- All child `RectTransform`s that are active are treated as items.
- X position: `centerX + (i - currentOffset) * (itemWidth + spacing)`
- Y position: `itemHeight/2 + arcHeight * cos(clamp(rel/visibleRadius) * π/2)`
- Scale: lerp between `edgeScale` and `1` based on distance from center.

---

### 6.2 `CarouselHomeBackground`

**File:** `Assets/Core/Scripts/UI/CarouselHomeBackground.cs`  
**Namespace:** `EnglishTek.Core`  
**Type:** `MonoBehaviour` (`[DisallowMultipleComponent]`)

Watches an `ArcCarousel` and swaps a background `RawImage` to the centered entry's `home` image URL.

#### Inspector Fields

| Field | Description |
|-------|-------------|
| `carousel` | The `ArcCarousel` holding the entry buttons. |
| `backgroundImage` | The full-bleed `RawImage` to update. |
| `controller` | `InteractiveController` used to resolve asset URLs. |

#### Public API

| Method | Description |
|--------|-------------|
| `SetEntries(IReadOnlyList<InteractiveCatalogEntry>)` | Called by `InteractiveCatalogMenu` after rendering entry buttons. Order must match carousel children. Immediately loads the first entry's home image. |
| `HideBackground()` | Stops any in-flight load, hides the background (alpha 0). |

When `ArcCarousel.OnCenterIndexChanged` fires, downloads the `home` image of the newly centered entry and fades the `RawImage` in.

---

## 7. Interactive Games

Each interactive game is a self-contained Unity project, built as **two AssetBundles** (`.assets` + `.scenes`) and deployed to the server. The container streams and runs them at runtime.

### Scene Flow (both games)

```
Title → Instructions → Difficulty → Game → Feedback
```

All scene transitions use a 0.5-second coroutine delay before calling `SceneManager.LoadScene`.

---

### 7.1 ID106 — Whack-a-Mole (Grade 1, Grammar)

**Namespace:** `EnglishTek.Grade1.ID106`  
**Assembly:** `ID106.asmdef`  
**Server path:** `Grade 1/grammar/unit1/ID106/`  
**Bundle base:** `englishtek.grade1.id106`

#### Scripts

##### `GameManager` (static class)

The data and logic hub for ID106. All fields are static — the game is single-session.

**State variables:**

| Variable | Type | Description |
|----------|------|-------------|
| `GameID` | `int` | Auto-derived from namespace (`106`). |
| `Instructions` | `string` | Loaded from XML key `"Instruction_ET1ID106"`. |
| `Difficulty` | `string` | `"Practice"`, `"Workout"`, or `"Quiz"`. |
| `Score` | `int` | Current score. |
| `Item` | `int` | Number of items answered so far. |
| `TotalItem` | `int` | Fixed at 10. |
| `Question` | `string` | Current question text. |
| `Correct` | `string` | Current correct answer. |
| `Choices` | `string` | Comma-separated shuffled choices (correct + 2 wrongs). |

**XML Manifest Keys:**

| Key | Purpose |
|-----|---------|
| `Instruction_ET1ID106` | Instructions text |
| `ItemBankPractice_ET1ID106` | Practice item bank |
| `ItembankWorkout_ET1ID106` | Workout item bank |
| `ItembankQuiz_ET1ID106` | Quiz item bank |
| `Feedback_ET1ID106` | Feedback strings |

**XML Item Bank structure:**
```xml
<Activity>
  <Item>
    <Question>text</Question>
    <Correct>answer</Correct>
    <Wrong1>wrong answer</Wrong1>
    <Wrong2>wrong answer</Wrong2>
  </Item>
</Activity>
```

**Feedback XML structure:**
```xml
<Feedback>
  <Perfect>message</Perfect>
  <Average>message</Average>
  <Fail>message</Fail>
</Feedback>
```
- Perfect: score = 100%
- Average: score > 70%
- Fail: score ≤ 70%

**Key methods:**

| Method | Description |
|--------|-------------|
| `Initialize()` | Sets `GameID`, `Instructions`, resets `Score = 0`, `Item = 0`, `TotalItem = 10`. |
| `GenerateItem()` | Reads XML for selected difficulty, populates internal question/answer lists. |
| `NextItem()` | Picks a random item from the lists, sets `Question`, `Correct`, `Choices` (shuffled). |
| `CheckAnswer(string)` | Returns `true` if answer matches `Correct`. |
| `Feedback()` | Returns feedback string based on score percentage. |

---

##### `Title` (MonoBehaviour)

- Calls `GameManager.Initialize()` on `Start`.
- Disables the Play button for 2 seconds (intro animation time).
- `Play()` → loads `"Instructions"` scene.

##### `Instructions` (MonoBehaviour)

- Displays `GameManager.Instructions` in a UI `Text`.
- `StartGame()` → loads `"Difficulty"` scene.

##### `Difficulty` (MonoBehaviour)

- `StartGame(string difficulty)` → sets `GameManager.Difficulty`, calls `GenerateItem()`, loads `"Game"` scene.

##### `Game` (MonoBehaviour)

- Spawns 3 random **flower** prefabs in the play area (decorative).
- Spawns 3 **mushroom** prefabs each with one of the shuffled `Choices` as their label.
- `CheckAnswer(string answer)`:
  - Disables all mushroom buttons.
  - If correct: increments score, triggers correct animations, waits, advances to next item or Feedback.
  - If wrong: triggers wrong animations, waits, advances.
- When `Item >= TotalItem`, loads `"Feedback"` scene.

**Inspector refs:** `question`, `score`, `item`, `instructions` (Text); `correct`, `wrong` (GameObjects with AudioSource); `itemContainer` (Transform); `flowers[]`, `mushrooms[]` (prefab arrays).

##### `Mushroom` (MonoBehaviour)

- `Click()` → calls `game.CheckAnswer(GetComponentInChildren<Text>().text)`.
- Gets the `Game` reference via `FindObjectOfType<Game>()`.

##### `Hammer` (MonoBehaviour)

- Follows the mouse cursor (clamped to play area).
- Plays hammer hit animation on mouse button down.
- Position is clamped to `(-350,350)` x and `(-195,180)` y.

##### `Feedback` (MonoBehaviour)

- Displays `GameManager.Feedback()`.
- Submits score via `SubmitScore.PostScores(diff, score)`.
- `Play()` → reloads `"Title"` scene and re-initializes.

---

### 7.2 ID213 — A Day at the Beach (Grade 1, Grammar)

**Namespace:** `EnglishTek.Grade1.ID213`  
**Assembly:** `ID213.asmdef`  
**Server path:** `Grade 1/grammar/unit1/ID213/` *(see catalog)*  
**Bundle base:** `englishtek.grade1.id213`

Similar structure to ID106 but uses a **character navigation** mechanic instead of Whack-a-Mole.

#### Key Differences from ID106

- Questions have two choices only: **Correct** and **Wrong** (no Wrong2).
- Answers are displayed on a **Starfish** and a **Shell** in the scene.
- In **Practice** mode: Starfish = always "Yes", Shell = always "No".
- In **Quiz/Workout** mode: choices are the actual Correct/Wrong strings, randomly assigned to starfish/shell.
- A **Character** (animated sprite) walks around the play area — the player guides it with arrow keys to reach the correct answer object.

**XML Manifest Keys:**

| Key | Purpose |
|-----|---------|
| `Instruction_ET1ID213` | Instructions text |
| `ItemBankPractice_ET1ID213` | Practice item bank |
| `ItembankWorkout_ET1ID213` | Workout item bank |
| `ItembankQuiz_ET1ID213` | Quiz item bank |
| `Feedback_ET1ID213` | Feedback strings |

**XML Item Bank structure:**
```xml
<Activity>
  <Item>
    <Question>text with (blank) highlighted</Question>
    <Correct>answer</Correct>
    <Wrong>wrong answer</Wrong>
  </Item>
</Activity>
```

The `Question` text uses `(` and `)` to delimit the blank word, which the `Game` script replaces with `<color=red>` rich text tags for display.

#### Scripts

##### `GameManager` (static class)

Same pattern as ID106. Single `Wrong` field instead of `Wrong1`/`Wrong2`.

##### `Character` (MonoBehaviour)

Controls the walking character sprite.

- `Initialize()` — resets position to `(0, 140)`.
- `Move(bool)` — enables/disables movement.
- Movement: **arrow keys only** (no WASD). `X`/`Y` direction → translates character.
- Animation triggers: `left`, `right`, `back`, `front`, `idle`.
- Clamped to `(-375, 375)` x and `(-280, 145)` y.

##### `Game` (MonoBehaviour)

- Randomly positions **starfish** and **shell** prefabs (no overlap with character).
- Calls `GameManager.NextItem()` to get question and answers.
- Rich text: `(word)` → `<color=red>word</color>`.
- Player walks character into starfish or shell to answer.
- Collision/trigger presumably handled by the prefabs or by proximity check (not shown in this file).

Inspector refs: `starfish`, `shell` (GameObjects); `character` (Character); `question`, `score`, `item`, `instructions` (Text); `minPos`, `maxPos` (Vector2 bounds); `correct`, `wrong` (Animators); `starfishAnswerText`, `shellAnswerText` (Text).

##### `Difficulty`, `Title`, `Instructions`, `Feedback` (MonoBehaviours)

Identical pattern to ID106 counterparts.

---

## 8. Server Data Layout

```
ServerData/Interactive/
├── Grade 1/
│   ├── catalog.json                    ← Grade 1 catalog
│   ├── grammar/
│   │   ├── unit1/
│   │   │   ├── ID106/
│   │   │   │   ├── englishtek.grade1.id106.assets
│   │   │   │   ├── englishtek.grade1.id106.scenes
│   │   │   │   ├── thumb.png
│   │   │   │   └── home.png
│   │   │   └── ID213/
│   │   │       ├── englishtek.grade1.id213.assets
│   │   │       ├── englishtek.grade1.id213.scenes
│   │   │       ├── thumb.png
│   │   │       └── home.png
│   │   └── unit2/
│   └── listening/
└── Grade 2/
    ├── catalog.json                    ← Grade 2 catalog
    ├── grammar/
    └── listening/
```

The server must serve files via HTTP. The default dev server runs at `http://localhost:8080`.

---

## 9. Catalog JSON Schema

```json
{
  "interactives": [
    {
      "id": "ID106",            // required — must match bundle name convention
      "title": "Whack-a-Mole", // display name
      "category": "grammar",   // lesson category (lowercase)
      "unit": "unit1",         // unit within category (lowercase)
      "image": "thumb.png",    // thumbnail (relative to entry folder)
      "home": "home.png",      // full-bleed background (relative to entry folder)
      "enabled": true,         // false = skip this entry
      "folder": "",            // optional: override full folder path
      "grade": "",             // optional: override grade
      "bundleBaseName": ""     // optional: override bundle file name base
    }
  ]
}
```

If `folder` is omitted, the path is built as: `{grade}/{category}/{unit}/{id}/`  
If `bundleBaseName` is omitted, it is built as: `englishtek.{grade}.{id}` (all lowercase).

---

## 10. Asset Bundle Naming Convention

| Part | Rule | Example |
|------|------|---------|
| Prefix | Always `englishtek` | `englishtek` |
| Grade | Grade string, lowercase, no spaces | `grade1` |
| ID | Interactive ID, lowercase | `id106` |
| Extension | `.assets` or `.scenes` | `.assets` |

Full example: `englishtek.grade1.id106.assets` and `englishtek.grade1.id106.scenes`

---

## 11. Data Flow — End to End

```
1. Container scene loads
       │
2. InteractiveController.Start()
       │
3. HTTP GET → Grade 1/catalog.json
       │
4. Parse JSON → List<InteractiveCatalogEntry>
       │
5. CatalogUpdated event fires
       │
6. InteractiveCatalogMenu / CatalogMenuNavigator renders buttons
       │
7. User taps an entry button
       │
8. InteractiveController.RequestGameLoad("ID106")
       │
9. Resolve folder:  Grade 1/grammar/unit1/ID106/
   Resolve bundle:  englishtek.grade1.id106
       │
10. Check local disk cache
    Hit  → load from Application.persistentDataPath/InteractiveCache/ID106/
    Miss → HTTP GET .assets + .scenes, save to cache
       │
11. GameSession.CurrentAssetBundle  = loadedAssetBundle
    GameSession.CurrentSceneBundle  = loadedSceneBundle
    GameSession.ContainerSceneName  = "ContainerScene"
       │
12. Build InteractiveManifest from scene bundle (fallback)
    GameSession.CurrentManifest = manifest
       │
13. ContainerReturnOverlay.EnsureExists() — DontDestroyOnLoad singleton
       │
14. SceneManager.LoadScene("Title", Single)
       │
15. Game runs:
    Title.Start() → GameManager.Initialize()
    Instructions → Difficulty → Game → Feedback
       │
16. Feedback.Start() → SubmitScore.PostScores(diff, score)
       │
17. User presses "Back" on ContainerReturnOverlay
       │
18. GameSession.CleanUp() — unloads both bundles
    SceneManager.LoadScene("ContainerScene")
       │
19. Back to step 2 (catalog already loaded; events re-fire)
```

---

## 12. Adding a New Interactive

### Step 1 — Create the game project

1. New Unity project.
2. Reference the `EnglishTek.Core` assembly so game scripts can access `GameSession`.
3. Use namespace `EnglishTek.Grade1.ID###` (or `Grade2` for Grade 2).
4. Follow the **Title → Instructions → Difficulty → Game → Feedback** scene pattern.

### Step 2 — Create the Manifest

1. In the game project, `Assets → Create → Interactive → Manifest`.
2. Set `firstSceneName` to your first scene name (e.g. `"Title"`).
3. Add all XML configs as `NamedXML` entries with matching keys.
4. Add all scene objects to `allScenes`.

### Step 3 — Build AssetBundles

1. Set all game assets and scenes to the bundle name `englishtek.grade1.id###`.
2. Build bundles for your target platform.
3. Rename outputs: `englishtek.grade1.id###.assets` and `englishtek.grade1.id###.scenes`.

### Step 4 — Deploy to Server

1. Create folder: `ServerData/Interactive/Grade 1/grammar/unit1/ID###/`
2. Copy `.assets`, `.scenes`, `thumb.png`, `home.png` into that folder.

### Step 5 — Update Catalog

Add an entry to `ServerData/Interactive/Grade 1/catalog.json`:

```json
{
  "id": "ID###",
  "title": "My New Game",
  "category": "grammar",
  "unit": "unit1",
  "image": "thumb.png",
  "home": "home.png",
  "enabled": true
}
```

### Step 6 — Verify

Start the local server and run the container. The new game should appear in the Grammar → Unit 1 list.

---

## 13. Adding a New Grade

1. In the Unity Inspector, create a new `InteractiveController` GameObject (or duplicate existing) with `grade` set to `"grade2"`.
2. On the server, create `ServerData/Interactive/Grade 2/catalog.json`.
3. Add game bundles under `ServerData/Interactive/Grade 2/grammar/unit1/ID###/`.
4. The Grade 2 controller will only read its own catalog — Grade 1 games will not appear.

Each grade needs its own `InteractiveController` instance and its own catalog.

---

## 14. LMS Score Submission (`SubmitScore`)

**File:** `Assets/Core/InteractiveScripts/ID###_Scripts/SubmitScore.cs`  
**Type:** `MonoBehaviour` (no namespace — legacy script)

Handles posting game scores to the **TekTeach LMS API**.

#### Endpoint

```
POST https://tekteachlms-api.com/api/Student/{studentId}/class/{classId}/interactive/{gameId}/score
```

#### Request Body (`ScoreModel`)

```json
{
  "dateTimeSubmitted": "MM/dd/yyyy HH:mm:ss tt",
  "isCompleted": false,
  "score": 8,
  "gameLevelId": 1,
  "completionTime": ""
}
```

`gameLevelId`: `1` = Practice, `2` = Quiz/Workout.

#### Platform Behaviour

| Platform | Behaviour |
|----------|-----------|
| `UNITY_EDITOR` | Uses dummy student `0`, class `0`, interactive `0`. Auth key = `"Test"`. |
| `UNITY_WEBGL` | Reads `studentId`, `gameId`, `classId` from URL query parameters (`?sid=&gid=&cid=`). Auth key read from URL fragment `#`. |
| `UNITY_ANDROID` | Logs `"android script here"` and exits (not implemented). |

**Usage:** Always call via `StartCoroutine(submitScore.PostScores(diff, score))`.

#### JS Interop (WebGL only)

Imports three JavaScript functions via `[DllImport("__Internal")]`:
- `GetURLFromPage()` — returns the full page URL.
- `openWindow(url)` — opens a new browser window.
- `closeWindow()` — closes the current window.

---

*End of documentation.*
