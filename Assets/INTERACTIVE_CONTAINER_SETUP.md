# Interactive Container Process and Setup Guide

This document explains how the current project loads an interactive and how to add a new one safely.

## 1. Project Purpose

The container app downloads two AssetBundles at runtime:
- `.assets` bundle for manifest, prefabs, and XML data.
- `.scenes` bundle for Unity scenes.

After download, it builds `GameSession` state and loads the first scene for the selected interactive.

## 2. Runtime Architecture

Core runtime scripts:
- `Core/Scripts/InteractiveController.cs`
- `Core/Scripts/GameSession.cs`
- `Core/Scripts/InteractiveManifest.cs`

Interactive gameplay scripts (example implementation):
- `Core/InteractiveScripts/ID106_Scripts/*.cs`

Data flow:
1. `InteractiveController.RequestGameLoad(gameId)` starts the download coroutine.
2. The controller requests:
   - `{serverRoot}/{gameId}/englishtek.{grade}.{gameId-lower}.assets`
   - `{serverRoot}/{gameId}/englishtek.{grade}.{gameId-lower}.scenes`
3. Bundles are stored in `GameSession.CurrentAssetBundle` and `GameSession.CurrentSceneBundle`.
4. Manifest resolution runs in this order:
   - Direct cast: `LoadAsset<InteractiveManifest>`.
   - Reflection-based recovery from raw bundle objects.
   - Scene fallback from scene bundle (prefers `Title` scene if present).
5. XML assets in the `.assets` bundle are mapped to manifest keys if missing.
6. `SceneManager.LoadScene(manifest.firstSceneName)` starts the interactive.

## 3. Current Naming Contract (Important)

### 3.1 Bundle naming
`InteractiveController` builds bundle base from the configured `grade` field.

Expected files on server:
- `englishtek.{grade}.{gameId-lower}.assets`
- `englishtek.{grade}.{gameId-lower}.scenes`

Example for `grade=grade1` and `gameId=106`:
- `englishtek.grade1.106.assets`
- `englishtek.grade1.106.scenes`

Example for `grade=grade2` and `gameId=106`:
- `englishtek.grade2.106.assets`
- `englishtek.grade2.106.scenes`

### 3.2 Folder layout on server
Expected URL pattern:
- `{serverRoot}/{gameId}/...bundle files...`

Default `serverRoot` in script:
- `http://localhost:8080/Interactive/`

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
Place files under:
- `{serverRoot}/{id}/`

Example:
- `http://localhost:8080/Interactive/107/englishtek.grade1.107.assets` (if grade is `grade1`)
- `http://localhost:8080/Interactive/107/englishtek.grade1.107.scenes` (if grade is `grade1`)

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
