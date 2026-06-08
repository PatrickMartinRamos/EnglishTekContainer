# TekContainer Documentation (Important Parts)

Last updated: June 5, 2026

## 1) What This Project Does
TekContainer is a Unity shell that:
- Loads interactive game metadata (catalog) from online source or local cache.
- Downloads interactive AssetBundles (.assets + .scenes).
- Caches bundles locally and runs interactive scenes.
- Returns back to container scene after gameplay.

Core namespace: Tek.Core

## 2) Core Files You Actually Need
- Assets/Core/Scripts/Interactive/InteractiveController.cs
- Assets/Core/Scripts/Interactive/InteractiveCatalogService.cs
- Assets/Core/Scripts/Interactive/InteractiveBundleService.cs
- Assets/Core/Scripts/Interactive/InteractivePathResolver.cs
- Assets/Core/Scripts/Interactive/InteractiveCatalogEntry.cs
- Assets/Core/Scripts/Interactive/InteractiveManifest.cs
- Assets/Core/Scripts/UI/GameSession.cs
- Assets/Core/Scripts/UI/ContainerReturnOverlay.cs
- Assets/link.xml

## 3) Required InteractiveController Setup
In InteractiveController inspector:
- serverRoot: base HTTP root (used when useGoogleSheetCatalogs = false).
- useGoogleSheetCatalogs:
  - true: catalog fetched from webAppUrl with query param tek=CurrentTek.
  - false: catalog fetched from serverRoot/{TekName}/{GradeName}/catalog.json.
- webAppUrl: Apps Script endpoint for catalog.
- grade: Grade1..Grade10.
- currentTek: englishtek, sciencetek, filipinotek, mathtek, aptek.
- refreshCatalogOnStart: usually true.

Important behavior:
- Bundle prefix is now derived from currentTek at runtime.
- Example: currentTek = filipinotek => bundle base uses filipinotek.

## 4) URL and Naming Rules
Tek and grade display names are resolved as:
- Tek: EnglishTek, ScienceTek, FilipinoTek, MathTek, APTek
- Grade: Grade 1, Grade 2, ... Grade 10

When using static server catalogs (useGoogleSheetCatalogs = false):
- Catalog URL:
  {serverRoot}/{TekName}/{GradeName}/catalog.json

Bundle folder URL:
- {serverRoot}/{TekName}/{folder}/
- folder comes from catalog entry folder, or default:
  {grade}/{category}/{unit}/{id}

Default bundle base name:
- {currentTek}.{grade}.{id}
- Built lowercase by helper before final filename.

Final files:
- {bundleBase}.assets
- {bundleBase}.scenes

## 5) Minimal catalog.json Schema
Top-level object:
- interactives: array

Per entry:
- Required:
  - id
- Recommended:
  - title
  - enabled (default true)
  - category
  - unit
  - image
  - home
  - grade
- Optional overrides:
  - folder
  - bundleBaseName
  - bundleVersion

Notes:
- Disabled entries are ignored.
- Entries without id are ignored.
- Catalog is sorted by DisplayName (title fallback to id).

## 6) Runtime Flow (Short)
1. InteractiveController.RefreshCatalog() loads catalog from network.
2. On network failure, local catalog cache is used.
3. RequestGameLoad(id) resolves folder and bundle names.
4. Bundle loader tries local bundle cache first, then downloads if needed.
5. On success:
   - GameSession.CurrentAssetBundle and CurrentSceneBundle are set.
   - Container return overlay is prepared.
   - First scene in bundle is loaded.
6. On return/cleanup: bundles are unloaded via GameSession.CleanUp().

## 7) Cache Locations and Invalidation
Catalog cache:
- Application.persistentDataPath/CatalogCache/

Bundle cache:
- Application.persistentDataPath/InteractiveCache/{cacheKey}/

Thumbnail cache:
- Application.persistentDataPath/ThumbnailCache/

Cache version reset:
- InteractiveController clears CatalogCache, InteractiveCache, and ThumbnailCache when Application.version changes.

## 8) Offline Behavior
- If bundle is already cached, interactive can still load offline.
- If bundle is not cached and download fails due to network, GameLoadOfflineBlocked is fired.

## 9) Android IL2CPP Critical Note
If interactive scenes fail to instantiate classes from bundles:
- Ensure assembly preservation is configured in Assets/link.xml.
- This repo has known cases requiring preservation for some ID assemblies.
- If needed during validation, temporarily reduce/disable managed stripping for Android builds.

## 10) Quick Add-New-Interactive Checklist
1. Build and upload .assets and .scenes to server folder.
2. Add/enable catalog entry with valid id (and folder/grade overrides if needed).
3. Verify bundle naming matches currentTek + grade + id rule, unless bundleBaseName override is provided.
4. Confirm first scene can load and return overlay works.
5. If update is not reflected, bump bundleVersion (or clear app cache).

## 11) Fast Troubleshooting
- Catalog not showing:
  - Verify webAppUrl or serverRoot path.
  - Verify Tek + Grade path mapping.
  - Check if cache has stale/empty catalog.

- Bundle 404:
  - Check folder path and filename case.
  - Check generated bundle base naming.
  - Confirm currentTek matches expected prefix.

- Old content still loading:
  - Cache likely stale.
  - Bump bundleVersion or clear InteractiveCache.

- Scene load/class ID errors on Android:
  - Check link.xml preservation and stripping level.

---
This file intentionally keeps only high-value operational details.
