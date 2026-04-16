# Interactive Container Process and Setup Guide

This document explains how the current project loads an interactive and how to add a new one safely.

## 1. Project Purpose

The container app downloads two AssetBundles at runtime:
- `.assets` bundle for manifest, prefabs, and XML data.
- `.scenes` bundle for Unity scenes.

After download, it builds `GameSession` state and loads the first scene for the selected interactive.

## 2. Runtime Architecture

Core runtime scripts:
- `Core/Scripts/InteractiveController.cs` — catalog fetch, bundle download/load, URL building
- `Core/Scripts/GameSession.cs` — static state holder for current session bundles and manifest
- `Core/Scripts/InteractiveManifest.cs` — ScriptableObject holding scene name and XML config entries
- `Core/Scripts/InteractiveCatalogMenu.cs` — MonoBehaviour UI that drives category/unit/entry selection
- `Core/Scripts/CatalogFilter.cs` — static helpers for filtering and querying catalog entries
- `Core/Scripts/CatalogStringHelper.cs` — string normalization and display formatting for catalog values
- `Core/Scripts/CatalogThumbnailLoader.cs` — async `UnityWebRequest` thumbnail image loader
- `Core/Scripts/CatalogUiFactory.cs` — procedural UI element factory (panels, buttons, text, images)

Interactive gameplay scripts (example implementation):
- `Core/InteractiveScripts/ID106_Scripts/*.cs`

Data flow:
1. `InteractiveController.RequestGameLoad(gameId)` starts the download coroutine.
2. The controller builds the folder path from the catalog entry's `category`, `unit`, and `id` fields:
   - `{serverRoot}/{category}/{unit}/{gameId}/englishtek.{grade}.{gameId-lower}.assets`
   - `{serverRoot}/{category}/{unit}/{gameId}/englishtek.{grade}.{gameId-lower}.scenes`
   If the catalog entry has an explicit `folder` field, that overrides the auto-built path.
   If the catalog entry has an explicit `bundleBaseName` field, that overrides the auto-built file name.
3. Bundles are stored in `GameSession.CurrentAssetBundle` and `GameSession.CurrentSceneBundle`.
4. Manifest resolution runs in this order:
   - Direct cast: `LoadAsset<InteractiveManifest>`.
   - Reflection-based recovery from raw bundle objects.
   - Scene fallback from scene bundle (prefers `Title` scene if present).
5. XML assets in the `.assets` bundle are mapped to manifest keys if missing.
6. `SceneManager.LoadScene(manifest.firstSceneName)` starts the interactive.

## 3. Current Naming Contract (Important)

### 3.1 Bundle naming
`InteractiveController` builds the bundle base name from the configured `grade` and the catalog entry's `id`.

Expected files on server:
- `englishtek.{grade}.{gameId-lower}.assets`
- `englishtek.{grade}.{gameId-lower}.scenes`

The `gameId` is lowercased as-is. Use the full ID string (including the `ID` prefix) consistently.

Example for `grade=grade1` and `gameId=ID106`:
- `englishtek.grade1.id106.assets`
- `englishtek.grade1.id106.scenes`

Example for `grade=grade1` and `gameId=ID213`:
- `englishtek.grade1.id213.assets`
- `englishtek.grade1.id213.scenes`

### 3.2 Folder layout on server
Default URL pattern (derived from catalog `category`, `unit`, and `id`):
- `{serverRoot}/{category}/{unit}/{gameId}/...bundle files...`

Explicit override using `folder` field in catalog entry:
- `{serverRoot}/{folder}/...bundle files...`

Default `serverRoot` in script:
- `http://localhost:8080/Interactive/`

Example for `category=grammar`, `unit=unit1`, `grade=grade1`, `gameId=ID213`:
- `http://localhost:8080/Interactive/grammar/unit1/ID213/englishtek.grade1.id213.assets`
- `http://localhost:8080/Interactive/grammar/unit1/ID213/englishtek.grade1.id213.scenes`

**Note:** Bundle file names are always lowercase. The gameId is lowercased as-is, so `ID106` → `id106`.

### 3.3 Scene entry
Best practice:
- Keep `firstSceneName` in manifest set to `Title`.

Fallback behavior:
- If manifest cannot be resolved, container tries to find `Title` in scene bundle.

### 3.4 XML keys expected by game logic
Current `GameManager` for ID106 uses these keys:
- `Instruction_ET1ID106`
- `Feedback_ET1ID106`
- `ItemBankPractice_ET1ID106`
- `ItembankWorkout_ET1ID106`
- `ItembankQuiz_ET1ID106`

If manifest keys are missing, container tries to infer keys from XML filenames.

## 4. Step-by-Step: Add a New Interactive

### Step 1: Duplicate an existing interactive as baseline
Use `ID106` as a reference implementation.

Create a new folder, for example:
- `Core/InteractiveScripts/ID107_Scripts/`

Copy required gameplay scripts and update namespaces/class references as needed.

### Step 2: Ensure gameplay scripts read from `GameSession.CurrentManifest`
Do not use `Resources.Load` for runtime XML in container mode.
Read CODE_TO_REPLACE_IN_GAMEMANAGER.md

Use:
- `GameSession.CurrentManifest.GetXMLText("...")`

### Step 3: Create or update your interactive manifest asset
Manifest type should match `InteractiveManifest` fields:
- `bundleName`
- `firstSceneName`
- `allScenes`
- `xmlConfigs`
- `prefabsToInclude`

Set `firstSceneName` to your intended start scene (recommended: `Title`).

### Step 4: Prepare XML files
Include XML files for:
- Instruction
- Feedback
- Practice ItemBank
- Workout ItemBank
- Quiz ItemBank

Keep naming predictable (`instruction`, `feedback`, `practice`, `workout`, `quiz` in filename) to help automatic key inference.

### Step 5: Build AssetBundles for the new ID
Build two bundles:
- `.assets` bundle containing manifest, prefabs, xml text assets.
- `.scenes` bundle containing all required scenes.

Name exactly as required:
- `englishtek.{grade}.{id-lower}.assets`
- `englishtek.{grade}.{id-lower}.scenes`

### Step 6: Upload to container server
Place files under the path matching the catalog entry's category, unit, and id:
- `{serverRoot}/{category}/{unit}/{id}/`

Example (category=grammar, unit=unit1, grade=grade1, id=ID107):
- `http://localhost:8080/Interactive/grammar/unit1/ID107/englishtek.grade1.id107.assets`
- `http://localhost:8080/Interactive/grammar/unit1/ID107/englishtek.grade1.id107.scenes`
- `http://localhost:8080/Interactive/grammar/unit1/ID107/thumb.png`

If you use a custom folder path, set the `folder` field in the catalog entry to that path.

### Step 7: Trigger load from container UI/code
Call:
- `RequestGameLoad("ID106")`

### Step 8: Validate runtime logs
Success indicators:
- Manifest loaded or recovered log.
- Scene load to `Title`.
- No XML key-missing errors.

If errors occur, check:
- Bundle URL reachability.
- Bundle naming and folder casing.
- Presence of XML files and scenes.
- `firstSceneName` in manifest.

## 5. Recommended Checklist Before Shipping a New Interactive

- Bundle files exist and URLs are reachable.
- `.assets` and `.scenes` names match the configured grade base (`englishtek.{grade}`).
- Start scene exists and is correctly named (`Title` preferred).
- XML files are present and parseable.
- Gameplay script keys match available XML keys.
- No null references from missing prefabs/assets.
- `GameSession.CleanUp()` is called when returning to container shell.

## 6. Known Limits in Current Structure

1. `grade` is Inspector-configurable per container instance, so server bundle naming must match this value exactly.
2. XML key mapping is convention-based for fallback; explicit manifest keys are safer.
3. Interactive script expectations can differ per ID, so each interactive should keep a clear key contract.

## 7. Suggested Future Improvement

To scale faster, introduce a small configuration file per interactive ID:
- Grade/base bundle prefix.
- Entry scene name.
- XML key map.

This removes hardcoded assumptions and makes adding new interactives mostly data-only.

## 8. Catalog-Based Selection

Folder discovery at runtime is not a reliable contract for HTTP servers.
Recommended approach:
- Host `catalog.json` at `{serverRoot}/catalog.json`
- Let Container fetch that file and build the selectable list in-game
- Keep per-interactive metadata in the catalog when bundle folder or bundle base differs from the default convention

Example:

```json
{
  "interactives": [
    {
      "id": "ID106",
      "title": "Whack-a-Mole",
      "category": "grammar",
      "unit": "unit1",
      "image": "thumb.png",
      "enabled": true
    },
    {
      "id": "ID213",
      "title": "A Day at the Beach",
      "category": "grammar",
      "unit": "unit1",
      "image": "thumb.png",
      "enabled": true
    }
  ]
}
```

Best practice:
- Generate `catalog.json` during your publish or upload step
- Do not maintain it by hand if new interactives are added frequently
