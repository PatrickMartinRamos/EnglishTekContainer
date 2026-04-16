# Scripts Reference

All scripts live under `Assets/Core/Scripts/`. They are part of the `EnglishTek.Core` assembly (`Core.asmdef`).

---

## InteractiveController.cs

**Type:** `MonoBehaviour`  
**Namespace:** `EnglishTek.Core`

The main controller. Attach to a persistent GameObject in your container scene.

### Inspector fields

| Field | Default | Description |
|---|---|---|
| `serverRoot` | `http://localhost:8080/Interactive/` | Base URL for all server requests |
| `grade` | `grade1` | Grade prefix used in bundle file names |
| `catalogFileName` | `catalog.json` | File name appended to `serverRoot` to fetch the catalog |
| `defaultCategory` | _(empty)_ | Fallback category when catalog entry has none |
| `defaultUnit` | _(empty)_ | Fallback unit when catalog entry has none |
| `refreshCatalogOnStart` | `true` | Whether to auto-fetch the catalog on `Start` |

### Key methods

| Method | Description |
|---|---|
| `RefreshCatalog()` | Fetches `catalog.json` from server and rebuilds the available interactives list |
| `RequestGameLoad(gameId)` | Downloads `.assets` and `.scenes` bundles for the given ID and loads the first scene |
| `ResolveCatalogAssetUrl(entry, assetPath)` | Resolves a relative asset path (e.g. thumbnail) to a full URL |

### Events

| Event | Signature | Description |
|---|---|---|
| `CatalogUpdated` | `Action<IReadOnlyList<InteractiveCatalogEntry>>` | Fired after a successful catalog load |
| `CatalogLoadFailed` | `Action<string>` | Fired when catalog download or parsing fails |

### URL building

Default folder path is built from the catalog entry's `category`, `unit`, and `id`:
```
{serverRoot}/{category}/{unit}/{id}/
```
Default bundle base name is built from `grade` and `id` (lowercased):
```
englishtek.{grade}.{id-lower}
```
Both can be overridden per catalog entry using `folder` and `bundleBaseName` fields.

### Local cache

Downloaded bundles are saved to:
```
Application.persistentDataPath/InteractiveCache/{id}/
```
On next load, the cached file is tried first before re-downloading.

---

## InteractiveCatalogEntry / InteractiveCatalogDocument

**Defined in:** `InteractiveController.cs`  
**Namespace:** `EnglishTek.Core`

`InteractiveCatalogEntry` maps to one entry in `catalog.json`.

| Field | Required | Description |
|---|---|---|
| `id` | Yes | Unique identifier (e.g. `ID106`). Used in URL and bundle name building |
| `title` | No | Display name shown in UI. Falls back to `id` if empty |
| `category` | No | Category string (e.g. `grammar`, `listening`). Used in folder URL |
| `unit` | No | Unit string (e.g. `unit1`). Used in folder URL |
| `image` | No | Thumbnail file name relative to the interactive's folder (e.g. `thumb.png`) |
| `grade` | No | Overrides the controller's `grade` field for this entry only |
| `folder` | No | Explicit server folder path, overrides the auto-built `{category}/{unit}/{id}` path |
| `bundleBaseName` | No | Explicit bundle file name base, overrides `englishtek.{grade}.{id-lower}` |
| `enabled` | Yes | Set `false` to hide the entry from the catalog without removing it |

`InteractiveCatalogDocument` is the root object: `{ "interactives": [ ... ] }`.

---

## GameSession.cs

**Type:** `static class`  
**Namespace:** `EnglishTek.Core`

Holds the active session state. All fields are public static and accessible from gameplay scripts.

| Field | Type | Description |
|---|---|---|
| `CurrentManifest` | `InteractiveManifest` | Manifest resolved from the loaded `.assets` bundle |
| `CurrentAssetBundle` | `AssetBundle` | Loaded `.assets` AssetBundle |
| `CurrentSceneBundle` | `AssetBundle` | Loaded `.scenes` AssetBundle |

### CleanUp()

Call `GameSession.CleanUp()` when returning to the container shell from a game. It unloads both bundles, clears all fields, and calls `Resources.UnloadUnusedAssets()`.

---

## InteractiveManifest.cs

**Type:** `ScriptableObject`  
**Namespace:** _(global)_

Asset created inside the interactive's `.assets` bundle. Drives what the container boots.

| Field | Description |
|---|---|
| `bundleName` | Name of the bundle (informational) |
| `firstSceneName` | Scene name loaded by the container to start the interactive |
| `allScenes` | List of scene objects included in the bundle |
| `xmlConfigs` | List of `NamedXML` entries mapping a string key to a `TextAsset` |
| `prefabsToInclude` | Prefabs to include in the `.assets` bundle |

### GetXMLText(key)

Returns the text content of the XML `TextAsset` associated with `key`, or `null` if not found.

```csharp
string xml = GameSession.CurrentManifest.GetXMLText("ItemBankPractice_ET1ID106");
```

---

## InteractiveCatalogMenu.cs

**Type:** `MonoBehaviour`  
**Namespace:** `EnglishTek.Core`

UI driver for the catalog. Attach alongside `InteractiveController` or on a separate GameObject. Requires a reference to the `InteractiveController` in its Inspector field.

### Inspector fields

| Field | Default | Description |
|---|---|---|
| `controller` | _(required)_ | Reference to `InteractiveController` |
| `targetCanvas` | _(auto-found)_ | Canvas to attach the built-in panel to |
| `refreshOnStart` | `true` | Whether to call `RefreshCatalog()` on `Start` |
| `showBuiltInCatalogPanel` | `false` | If true, generates a panel with category/entry buttons procedurally at runtime |
| `autoGenerateLessonButtons` | `true` | Generates category tab buttons from catalog data |
| `unitButtonContainer` | _(optional)_ | Parent `Transform` for unit buttons (prefab mode) |
| `unitButtonPrefab` | _(optional)_ | Prefab used for each unit button |
| `interactiveButtonContainer` | _(optional)_ | Parent `Transform` for entry buttons (prefab mode) |
| `interactiveButtonPrefab` | _(optional)_ | Prefab used for each interactive entry button |
| `autoSelectFirstUnit` | `true` | Automatically selects the first unit when a category is applied |
| `panelSize` | `(360, 220)` | Size of the auto-generated panel (when `showBuiltInCatalogPanel` is true) |
| `anchoredPosition` | `(0, -200)` | Position of the auto-generated panel |

### Public methods for button wiring

| Method | Description |
|---|---|
| `SelectLesson(category)` | Filters by category name (normalized) |
| `SelectUnit(unit)` | Filters by unit within the current category |
| `SelectGrammarLesson()` | Shortcut for `SelectLesson("grammar")` |
| `SelectReadingLesson()` | Shortcut for `SelectLesson("reading")` |
| `SelectListeningLesson()` | Shortcut for `SelectLesson("listening")` |
| `SelectVirtualDialogueLesson()` | Shortcut for `SelectLesson("virtual dialogue")` |

---

## CatalogFilter.cs

**Type:** `internal static class`  
**Namespace:** `EnglishTek.Core`

Stateless helpers for querying catalog data. Used by `InteractiveCatalogMenu`.

| Method | Description |
|---|---|
| `HasCategory(interactives, category)` | Returns true if any entry matches the given category |
| `BuildUniqueCategories(interactives)` | Returns a deduplicated list of all categories present |
| `BuildUnitsForCategory(interactives, category)` | Returns a deduplicated list of units within a given category |

Category and unit values default to `"general"` when the entry field is empty.

---

## CatalogStringHelper.cs

**Type:** `internal static class`  
**Namespace:** `EnglishTek.Core`

String normalization and display formatting for catalog values.

| Method | Description |
|---|---|
| `NormalizeCategory(value)` | Trims and lowercases a category string |
| `NormalizeUnit(value)` | Trims and lowercases a unit string |
| `FormatCategoryLabel(category)` | Returns a title-cased display string (e.g. `grammar` → `Grammar`) |
| `FormatUnitLabel(unit)` | Returns a title-cased display string (e.g. `unit1` → `Unit1`) |

---

## CatalogThumbnailLoader.cs

**Type:** `internal MonoBehaviour`  
**Namespace:** `EnglishTek.Core`  
**Attribute:** `[DisallowMultipleComponent]`

Handles async HTTP downloads of catalog thumbnail images. Added automatically to the same GameObject as `InteractiveCatalogMenu` if not present.

| Method | Description |
|---|---|
| `TryLoadThumbnail(entry, target, controller)` | Starts a coroutine to download the thumbnail and apply it to a `RawImage` |
| `StopAll()` | Stops all in-flight thumbnail coroutines |
| `ClearTextures()` | Destroys all `Texture2D` objects loaded this session to free memory |

---

## CatalogUiFactory.cs

**Type:** `internal static class`  
**Namespace:** `EnglishTek.Core`

Procedural UI element creation helpers. Used by `InteractiveCatalogMenu` when `showBuiltInCatalogPanel` is true or when no button prefabs are assigned.

| Method | Description |
|---|---|
| `CreateTextElement(name, parent, fontSize)` | Creates a `GameObject` with a `Text` component, white Arial font, white color |
| `CreateThumbnailElement(parent)` | Creates a left-anchored `RawImage` placeholder (grey, 40px wide) |
| `CreateCategoryRow(parent)` | Creates a `HorizontalLayoutGroup` row for category tab buttons |
| `CreateEntriesContainer(parent)` | Creates a `VerticalLayoutGroup` container for interactive entry buttons |

---

## Prefabs

| Prefab | Description |
|---|---|
| `Interactive.prefab` | Root prefab for the container scene. Attach `InteractiveController` and `InteractiveCatalogMenu` here |
| `Unit.prefab` | Default prefab used as the unit button when `unitButtonPrefab` is not set |
