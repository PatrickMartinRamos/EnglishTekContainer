# Interactive Authoring Guide for Container Compatibility

This document explains how to prepare an interactive so it works inside the Container runtime.

## 1. Goal

Your interactive must produce 2 platform-specific AssetBundles that follow the Container naming contract:
- `.assets` bundle: manifest, prefabs, XML/text data
- `.scenes` bundle: all playable scenes

The Container loads these bundles at runtime from server and local cache.

## 2. Required Runtime Contract

The current Container loader expects:
- Folder per interactive ID on server: `{serverRoot}/{gameId}/`
- Bundle base name format: `englishtek.{grade}.{gameId-lower}`

Required files per ID:
- `englishtek.{grade}.{id-lower}.assets`
- `englishtek.{grade}.{id-lower}.scenes`

Example for `grade=grade1`, ID 107:
- `/Interactive/107/englishtek.grade1.107.assets`
- `/Interactive/107/englishtek.grade1.107.scenes`

## 3. Scene Structure Requirements

Recommended scenes:
- `Title` as startup scene
- `Instructions`
- Gameplay scene(s)
- `Feedback`

Notes:
- Set manifest `firstSceneName = Title`.
- If manifest is not readable, Container fallback prefers `Title` in scene bundle.

## 4. Manifest Requirements

Use a manifest compatible with `InteractiveManifest` fields:
- `bundleName`
- `firstSceneName`
- `allScenes`
- `xmlConfigs`
- `prefabsToInclude`

Minimum requirement to boot correctly:
- Valid `firstSceneName`

Strong recommendation:
- Fill `xmlConfigs` explicitly with final keys used by gameplay scripts.

## 5. XML Data Requirements

Your game scripts should read XML from manifest keys, not `Resources.Load`.

For the current Grade1/ID106-style logic, the expected keys are:
- `Instruction_ET1ID###`
- `Feedback_ET1ID###`
- `ItemBankPractice_ET1ID###`
- `ItembankWorkout_ET1ID###`
- `ItembankQuiz_ET1ID###`

Replace `###` with your game ID (for example `ID107`).

Filename recommendation (for fallback key inference):
- include words: `instruction`, `feedback`, `practice`, `workout`, `quiz`

## 6. Authoring Workflow (Step by Step)

1. Duplicate a known-good interactive (for example ID106) as a template.
2. Rename namespace/classes/folders to the new ID.
3. Prepare scenes and set intended start scene to `Title`.
4. Create/update manifest with `firstSceneName` and XML config mappings.
5. Add/update prefabs and audio assets.
6. Add XML files for instruction, feedback, and all item banks.
7. Ensure gameplay code reads data via `GameSession.CurrentManifest.GetXMLText(...)`.
8. Assign assets/scenes to the correct AssetBundle names (`.assets`, `.scenes`).
9. Build AssetBundles for the target platform.
10. Upload files to server under `{gameId}` folder.
11. Trigger load from Container using `RequestGameLoad("{id}")`.
12. Validate logs and full flow: Title -> Instructions -> Game -> Feedback.

## 7. Platform Build Rules

AssetBundles are platform-specific.

Must build and upload separate bundles for each platform you support:
- Windows build target bundles for Windows container
- Android build target bundles for Android container

Do not reuse Windows bundles on Android.

## 8. Android-Specific Notes

1. Build interactive bundles with Android build target.
2. Ensure server URL is reachable from device.
3. If using HTTP (not HTTPS), Android cleartext policy may block network calls unless configured.
4. Test on real device for memory and download timing.

## 9. Server Deployment Checklist

- Folder exists: `/Interactive/{id}/`
- Both bundles uploaded: `.assets` and `.scenes`
- Bundle names exactly match expected pattern
- Correct platform version uploaded
- MIME type and file serving configured correctly
- Device can reach URL

## 10. Validation Checklist Before Release

- Container loads interactive without null reference errors
- Correct first scene opens (`Title`)
- Instruction text loads from XML
- Difficulty/item banks load for Practice/Workout/Quiz
- Feedback XML loads at end of game
- Prefabs/animations/audio appear correctly
- Return flow and cleanup work

## 11. Common Failures and Fixes

1. Error: cannot find manifest
- Check manifest included in `.assets`
- Confirm field compatibility and `firstSceneName`
- Ensure scene bundle contains `Title` as fallback

2. Error: XML key not found
- Check `xmlConfigs` keys and script key names match exactly
- Ensure xml files are in `.assets`

3. Wrong first scene opens
- Set manifest `firstSceneName = Title`
- Verify scene names in bundle

4. Works in Editor but not Android
- Rebuild AssetBundles for Android
- Verify Android network policy and URL access

## 12. Recommended Team Practice

Maintain one release checklist per interactive ID containing:
- Game ID
- Bundle names
- Platform target
- Manifest firstSceneName
- XML key table
- Server upload URLs
- Test evidence (logs/screenshots)

This will reduce regressions when scaling to many interactives.
