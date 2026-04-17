# EnglishTek Container — Complete Documentation

> Last updated: April 17, 2026
> Unity project. Namespace root: `EnglishTek.Core` (container), `EnglishTek.Grade1.ID###` / `EnglishTek.Grade2.ID###` (interactives).

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
5. [Interactive Games Reference](#5-interactive-games-reference)
   - 5.1 [ID106 — Whack-a-Mole (Grade 1, Grammar)](#51-id106--whack-a-mole-grade-1-grammar)
   - 5.2 [ID213 — A Day at the Beach (Grade 1, Grammar)](#52-id213--a-day-at-the-beach-grade-1-grammar)
   - 5.3 [ID232 — Grade 2 Grammar (Robot Factory)](#53-id232--grade-2-grammar-robot-factory)
6. [Server Layout & Bundle Naming](#6-server-layout--bundle-naming)
7. [Catalog JSON Schema](#7-catalog-json-schema)
8. [Data Flow — End to End](#8-data-flow--end-to-end)
9. [UI Animation System](#9-ui-animation-system)
10. [LMS Score Submission](#10-lms-score-submission)
11. [Common Issues & Fixes](#11-common-issues--fixes)
12. [Adding a New Interactive — Step by Step](#12-adding-a-new-interactive--step-by-step)

---

## 1. Project Overview

**EnglishTek Container** is a Unity multi-platform shell that:

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
│   │   │   ├── UIGroup.cs                      ← Animated show/hide panels
│   │   │   ├── ContainerReturnOverlay.cs       ← Persistent back button
│   │   │   ├── Interactive/
│   │   │   │   ├── InteractiveController.cs    ← Main coordinator
│   │   │   │   ├── InteractiveCatalogMenu.cs   ← Catalog UI
│   │   │   │   ├── CatalogMenuNavigator.cs     ← Animated nav extension
│   │   │   │   ├── InteractiveCatalogEntry.cs  ← Data model
│   │   │   │   └── InteractiveManifest.cs      ← ScriptableObject for a game
│   │   │   ├── Catalog/
│   │   │   │   ├── CatalogFilter.cs
│   │   │   │   ├── CatalogStringHelper.cs
│   │   │   │   ├── CatalogThumbnailLoader.cs
│   │   │   │   └── CatalogUiFactory.cs
│   │   │   └── UI/
│   │   │       ├── ArcCarousel.cs
│   │   │       └── CarouselHomeBackground.cs
│   │   └── InteractiveScripts/
│   │       ├── ID106_Scripts/   (ID106.asmdef)
│   │       ├── ID213_Scripts/   (ID213.asmdef)
│   │       └── ID232_Scripts/   (ID232.asmdef)
│   └── link.xml                               ← IL2CPP stripping preservation list
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
**Type:** `static class` — `EnglishTek.Core`

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
string xml = GameSession.CurrentManifest.GetXMLText("ItemBankPractice_ET1ID106");
```

---

### 4.2 `InteractiveController`

**File:** `Assets/Core/Scripts/Interactive/InteractiveController.cs`
**Type:** `MonoBehaviour` — `EnglishTek.Core`

Central coordinator. One instance per grade, lives in the container scene.

#### Inspector Fields

| Field | Default | Description |
|-------|---------|-------------|
| `serverRoot` | `http://localhost:8080/Interactive/` | Base URL for all server requests. |
| `grade` | `grade1` | Grade prefix for catalog and bundle names. Use `grade2` for Grade 2. |
| `catalogFileName` | `catalog.json` | File appended to grade path to form the catalog URL. |
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
| `RefreshCatalog()` | Re-fetches catalog JSON from server. |
| `RequestGameLoad(string gameId)` | Downloads and launches the game. |
| `ResolveCatalogAssetUrl(entry, assetPath)` | Resolves relative image paths to absolute URLs. |

#### URL Conventions

| Pattern | Result |
|---------|--------|
| Catalog URL | `{serverRoot}/{grade}/catalog.json` |
| Folder URL | `{serverRoot}/{grade}/{category}/{unit}/{id}/` |
| Bundle base | `englishtek.{grade}.{id}` (all lowercase) |
| Asset bundle | `{folder}{bundleBase}.assets` |
| Scene bundle | `{folder}{bundleBase}.scenes` |

#### Local Cache

Bundles are cached at:
```
Application.persistentDataPath/InteractiveCache/{cacheKey}/
```
Cache is tried first on every load. Clear via **EnglishTek → Clear Interactive Cache** in the Unity toolbar.

---

### 4.3 `InteractiveManifest`

**File:** `Assets/Core/Scripts/Interactive/InteractiveManifest.cs`
**Type:** `ScriptableObject` (`[CreateAssetMenu]`) — global namespace

Describes a game bundle. Created in the game project and baked into the `.assets` bundle.

| Field | Type | Description |
|-------|------|-------------|
| `bundleName` | `string` | AssetBundle name (informational). |
| `firstSceneName` | `string` | Scene to load on startup. Set to `"Title"`. |
| `allScenes` | `List<Object>` | All scenes in the bundle (build tracking). |
| `xmlConfigs` | `List<NamedXML>` | Key → TextAsset XML pairs. |
| `prefabsToInclude` | `GameObject[]` | Prefabs force-included in `.assets`. |
| `GetXMLText(key)` | `string` | Returns XML text for key, or `null`. |

**`NamedXML`:**

| Field | Description |
|-------|-------------|
| `key` | Lookup key (e.g. `"ItemBankPractice_ET1ID106"`). |
| `xmlFile` | The `TextAsset` with raw XML. |

---

### 4.4 `InteractiveCatalogEntry`

**File:** `Assets/Core/Scripts/Interactive/InteractiveCatalogEntry.cs`
**Type:** `[Serializable]` class — `EnglishTek.Core`

One entry in `catalog.json`.

| Field | Required | Description |
|-------|----------|-------------|
| `id` | Yes | Unique ID (e.g. `"ID106"`). Used for bundle lookup. |
| `title` | No | Display name. Falls back to `id` if empty. |
| `category` | No | Lesson category (e.g. `"grammar"`). |
| `unit` | No | Unit (e.g. `"unit1"`). |
| `image` | No | Thumbnail path relative to entry folder. |
| `home` | No | Full-bleed background path. |
| `enabled` | Yes | `false` hides entry from UI without removing it. |

Root catalog document: `{ "interactives": [ ... ] }`.

---

### 4.5 `InteractiveCatalogMenu`

**File:** `Assets/Core/Scripts/Interactive/InteractiveCatalogMenu.cs`
**Type:** `MonoBehaviour` — `EnglishTek.Core`

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
| `SelectGrammarLesson()` | Shortcut for `SelectLesson("grammar")`. |
| `SelectReadingLesson()` | Shortcut for `SelectLesson("reading")`. |
| `SelectListeningLesson()` | Shortcut for `SelectLesson("listening")`. |
| `SelectVirtualDialogueLesson()` | Shortcut for `SelectLesson("virtual dialogue")`. |
| `virtual GoBack()` | Override in subclass for back navigation. |

---

### 4.6 `CatalogMenuNavigator`

**File:** `Assets/Core/Scripts/Interactive/CatalogMenuNavigator.cs`
**Type:** Extends `InteractiveCatalogMenu` — `EnglishTek.Core`

Adds animated UIGroup transitions between navigation levels.

#### Additional Inspector Fields

| Field | Assign | Behaviour |
|-------|--------|-----------|
| `categoryGroup` | Container holding category buttons | Hidden after category is picked. |
| `unitGroup` | Container holding unit buttons | Shown after category pick; hidden after unit pick. |
| `entryGroup` | Container holding entry buttons | Shown after unit pick. |

#### Additional Methods

| Method | Description |
|--------|-------------|
| `GoBack()` | Entry visible → shows unit. Unit visible → shows category. |
| `GoToCategories()` | Instantly returns to category group. Wire to a "home" button. |

---

### 4.7 `ContainerReturnOverlay`

**File:** `Assets/Core/Scripts/ContainerReturnOverlay.cs`
**Type:** `MonoBehaviour` (DontDestroyOnLoad singleton) — `EnglishTek.Core`

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

**File:** `Assets/Core/Scripts/UIGroup.cs`
**Type:** `MonoBehaviour` — `EnglishTek.Core`

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
| `ShowImmediate()` | Snap visible, no animation. |
| `HideImmediate()` | Snap hidden, deactivate, no animation. |
| `IsVisible` | `bool` — current visibility. |

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

All `internal static`, `EnglishTek.Core`.

**`CatalogFilter`** — `Assets/Core/Scripts/Catalog/CatalogFilter.cs`

| Method | Description |
|--------|-------------|
| `HasCategory(interactives, category)` | `true` if any entry matches category. |
| `BuildUniqueCategories(interactives)` | Deduplicated list of all categories. |
| `BuildUnitsForCategory(interactives, category)` | Deduplicated units for category. |

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
**Type:** `MonoBehaviour` (`[ExecuteAlways]`) — `EnglishTek.Core`

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
**Type:** `MonoBehaviour` — `EnglishTek.Core`

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
**Type:** `static class` (Editor only) — `EnglishTek.Core.Editor`

Unity toolbar menu at **EnglishTek → Clear Interactive Cache**.

| Menu Item | Action |
|-----------|--------|
| **All** | Deletes entire `InteractiveCache` folder (with confirmation). |
| **ID106** | Clears all cached bundles for ID106. |
| **ID213** | Clears all cached bundles for ID213. |
| **ID232** | Clears all cached bundles for ID232. |
| **Show Cache Folder** | Opens cache folder in Explorer. |

Cache root: `Application.persistentDataPath/InteractiveCache/`

> Use this whenever you push new bundles to the server to force fresh downloads.

---

## 5. Interactive Games Reference

All interactives follow this scene flow:
```
Title → Instructions → Difficulty → Game → Feedback
```

Scripts use `GameSession.CurrentManifest.GetXMLText(key)` to read XML from the bundle. **Never use `Resources.Load` in container interactives.**

---

### 5.1 ID106 — Whack-a-Mole (Grade 1, Grammar)

**Namespace:** `EnglishTek.Grade1.ID106` | **Assembly:** `ID106` | **Bundle:** `englishtek.grade1.id106`

#### XML Manifest Keys

| Key | Data |
|-----|------|
| `Instruction_ET1ID106` | Instructions text |
| `ItemBankPractice_ET1ID106` | Practice items |
| `ItembankWorkout_ET1ID106` | Workout items |
| `ItembankQuiz_ET1ID106` | Quiz items |
| `Feedback_ET1ID106` | Feedback strings |

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

#### XML Manifest Keys

| Key | Data |
|-----|------|
| `Instruction_ET1ID213` | Instructions text |
| `ItemBankPractice_ET1ID213` | Practice items |
| `ItembankWorkout_ET1ID213` | Workout items |
| `ItembankQuiz_ET1ID213` | Quiz items |
| `Feedback_ET1ID213` | Feedback strings |

#### Scripts

| Script | Description |
|--------|-------------|
| `GameManager` | Static data hub. XML loaded from manifest via `GetXMLTextWithFallback()`. |
| `Title` | Auto-wires Play button listener at runtime (`Awake`) in case Inspector OnClick is missing in container scene. |
| `Instructions` | Displays instructions. `StartGame()` → loads Difficulty. |
| `Difficulty` | Sets difficulty, calls `GenerateItem()`, loads Game. |
| `Game` | Countdown timer, left/right input, life system, scientist/robot animations. |
| `Feedback` | Displays feedback. `Play()` → reloads Title. |

#### Known Gotchas

| Issue | Cause | Fix |
|-------|-------|-----|
| Title script not attached in container | Stale bundle built before ID232 asmdef was created (Assembly-CSharp vs ID232 mismatch) | Clear cache via **EnglishTek → Clear Interactive Cache → ID232**, then reload |
| XML not loading | Manifest keys use `ETID232`, code expected `ET1ID232` | `GetXMLTextWithFallback()` tries both formats |

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
│   ├── Virtual Dialogue/
├── Grade 2/
│   ├── catalog.json
│   └── grammar/
│       └── unit1/
│           └── ID232/
│               ├── englishtek.grade2.id232.assets
│               ├── englishtek.grade2.id232.scenes
│               ├── thumb.png
│               └── home.png
```

#### Bundle Naming Rule

```
englishtek.{grade}.{id-lowercase}.assets
englishtek.{grade}.{id-lowercase}.scenes
```

Examples:
- `englishtek.grade1.id106.assets`
- `englishtek.grade2.id232.scenes`

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
2.  InteractiveController.Start() → HTTP GET Grade 1/catalog.json
3.  Parse JSON → List<InteractiveCatalogEntry>
4.  CatalogUpdated event → InteractiveCatalogMenu renders buttons
5.  User taps an interactive entry
6.  RequestGameLoad("ID106")
7.  Resolve: folder = Grade 1/grammar/unit1/ID106/
             bundleBase = englishtek.grade1.id106
8.  Check local cache:
      Hit  → load from InteractiveCache/
      Miss → HTTP GET .assets + .scenes → save to cache
9.  GameSession.CurrentAssetBundle  = loadedAssets
    GameSession.CurrentSceneBundle  = loadedScenes
    GameSession.ContainerSceneName  = "ContainerScene"
10. Build InteractiveManifest from scene bundle (fallback: find "Title" scene)
    GameSession.CurrentManifest = manifest
11. ContainerReturnOverlay.EnsureExists() → DontDestroyOnLoad
12. SceneManager.LoadScene("Title")
13. Game runs:
      Title  → GameManager.Initialize() → reads XML from manifest
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

## 10. LMS Score Submission (TODO)

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
```

---

## 11. Common Issues & Fixes

| Symptom | Cause | Fix |
|---------|-------|-----|
| "Associated script cannot be loaded" in Inspector | Bundle built from old Assembly-CSharp; container uses ID### asmdef | Clear cache for that ID, rebuild bundle from container-compatible project |
| Play button works but scene does not change | OnClick not wired in bundled scene | Title.Awake() auto-wires if no persistent listeners exist |
| XML key not found | Manifest key mismatch (e.g. ETID232 vs ET1ID232) | Use `GetXMLTextWithFallback()` with both variants |
| Stale bundle loaded after update | Local cache still has old file | **EnglishTek → Clear Interactive Cache → [ID]** or bump `bundleVersion` in catalog |
| Script attached but Play does nothing | `title_idle` / `title_entrance` null reference throws before `LoadScene` | Both refs are null-checked in ID232 Title (already patched) |
| Android build crashes on interactive load | IL2CPP stripped interactive assemblies | Ensure `link.xml` preserves all ID### assemblies |
| Bundle loads but scenes are empty | Bundle built for wrong platform (e.g. WebGL bundle on Android) | Rebuild bundle for target platform, clear cache |

---

## 12. Adding a New Interactive — Step by Step

### Step 1: Set Up Script Namespace and Assembly

1. Create folder `Assets/Core/InteractiveScripts/ID###_Scripts/`.
2. Create `ID###.asmdef`.
3. Use namespace `EnglishTek.Grade1.ID###`.
4. Add `<assembly fullname="ID###" preserve="all"/>` to `Assets/link.xml`.

### Step 2: Create the Five Game Scripts

Copy from ID106 or ID213 as a baseline and update all namespaces.

**Minimum required scripts:** `GameManager`, `Title`, `Instructions`, `Difficulty`, `Game`, `Feedback`, `SubmitScore`.

**GameManager pattern — always use bundle manifest XML:**
```csharp
using EnglishTek.Core;

// In GenerateItem():
string xmlContent = GameSession.CurrentManifest.GetXMLText("ItemBankPractice_ET1ID###");
if (string.IsNullOrEmpty(xmlContent)) { Debug.LogError("..."); return; }

// In GetInstructions():
string xmlContent = GameSession.CurrentManifest.GetXMLText("Instruction_ET1ID###");

// In Feedback():
string xmlContent = GameSession.CurrentManifest.GetXMLText("Feedback_ET1ID###");
```

### Step 3: The Scene Flow

Unity scenes: `Title`, `Instructions`, `Difficulty`, `Game`, `Feedback`.

Each scene uses `SceneManager.LoadScene("SceneName")` to transition.

### Step 4: Prepare XML Files

Create XML files for each data type. Recommended naming (for auto key inference):
- `Instruction.xml` → inferred key: `Instruction_ET1ID###`
- `Feedback.xml` → inferred key: `Feedback_ET1ID###`
- `Itembank_Practice.xml` → inferred key: `ItemBankPractice_ET1ID###`
- `Itembank_Workout.xml` → inferred key: `ItembankWorkout_ET1ID###`
- `Itembank_Quiz.xml` → inferred key: `ItembankQuiz_ET1ID###`

### Step 5: Create the InteractiveManifest

In the game's Unity project:
1. `Assets → Create → Interactive → Manifest`
2. Set `firstSceneName = "Title"`
3. Add each XML file as a `NamedXML` entry with its exact key
4. Add all scene assets to `allScenes`
5. Add any required prefabs to `prefabsToInclude`

### Step 6: Build AssetBundles

Assign bundle name `englishtek.grade1.id###` to:
- All scene assets → `.scenes` bundle
- Manifest, XML TextAssets, prefabs → `.assets` bundle

Build for each target platform:
- Build → output `englishtek.grade1.id###.assets` and `.scenes`
- Separate builds for Windows, WebGL, Android

### Step 7: Deploy to Server

1. Create: `ServerData/Interactive/Grade 1/grammar/unit1/ID###/`
2. Upload:
   - `englishtek.grade1.id###.assets`
   - `englishtek.grade1.id###.scenes`
   - `thumb.png` (thumbnail, shown in catalog list)
   - `home.png` (full-bleed background, shown in carousel)

### Step 8: Add Catalog Entry

Add to `ServerData/Interactive/Grade 1/catalog.json`:
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

### Step 9: Add Cache Clear Support (Optional)

Add a menu item to `Assets/Core/Editor/InteractiveCacheClearer.cs`:
```csharp
[MenuItem("EnglishTek/Clear Interactive Cache/ID###")]
private static void ClearID###() => ClearById("ID###");
```

### Step 10: Validate

Run the container and check:

- [ ] New game appears in catalog menu under the correct category and unit
- [ ] Pressing the entry loads the Title scene
- [ ] Instructions text loads correctly from XML
- [ ] All three difficulty levels load item banks correctly
- [ ] Game plays through all 10 items
- [ ] Feedback screen shows and score is correct
- [ ] Score submits to LMS (check network tab)
- [ ] Back button returns to container
- [ ] No errors in Unity Console

### Troubleshooting Checklist

| Check | How |
|-------|-----|
| Bundle URL reachable? | Open `http://localhost:8080/Interactive/...` in browser |
| Bundle for correct platform? | Rebuild for target (Windows ≠ Android ≠ WebGL) |
| Manifest firstSceneName matches? | Should be exactly `"Title"` |
| XML keys match code? | Compare `GetXMLText("key")` calls to manifest NamedXML keys |
| Script assembly correct? | ID### asmdef must match what was used when building the bundle |
| Stale cache? | Clear via EnglishTek menu or bump `bundleVersion` |
| link.xml updated? | `<assembly fullname="ID###" preserve="all"/>` |
