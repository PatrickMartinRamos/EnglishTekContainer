# EnglishTekContainer

Unity container application that downloads an interactive catalog, loads remote AssetBundles, and launches interactive scenes (for example ID106 and ID213).

## What This Project Does

- Downloads catalog JSON from server.
- Shows interactives grouped by category and unit.
- Downloads and caches two bundles per interactive:
  - `.assets`
  - `.scenes`
- Opens the interactive start scene (prefers scene named `Title` when available).
- Shows an in-game overlay button so users can return to the container scene.

## Main Runtime Flow

1. `InteractiveController` loads `catalog.json`.
2. `InteractiveCatalogMenu` renders category/unit/interactive buttons.
3. User selects an interactive button.
4. `InteractiveController.RequestGameLoad(id)` resolves folder + bundle names.
5. Controller loads local cached bundles first, then server if cache is missing/invalid.
6. Controller stores loaded bundles in `GameSession`.
7. Controller resolves first scene from scene bundle and calls `SceneManager.LoadScene(...)`.
8. `ContainerReturnOverlay` appears on `Title` scene and returns to container when clicked.

## Important Files

- `Assets/Core/Scripts/Interactive/InteractiveController.cs`
  - Catalog download, bundle URL resolution, local cache behavior, scene launch.
- `Assets/Core/Scripts/Interactive/InteractiveCatalogEntry.cs`
  - Catalog JSON data model (`interactives[]` entries).
- `Assets/Core/Scripts/Interactive/InteractiveCatalogMenu.cs`
  - Builds catalog UI and binds buttons to `RequestGameLoad`.
- `Assets/Core/Scripts/Interactive/InteractiveCatalogMenu.Buttons.cs`
  - Partial class handling dynamic button creation for the catalog menu.
- `Assets/Core/Scripts/Interactive/BundleUrlHelper.cs`
  - Utility for composing AssetBundle download URLs from catalog entry fields.
- `Assets/Core/Scripts/Interactive/InteractiveManifest.cs`
  - Manifest model used for XML lookups and startup scene metadata.
- `Assets/Core/Scripts/Catalog/CatalogMenuNavigator.cs`
  - Optional animated category -> unit -> interactive navigation.
- `Assets/Core/Scripts/Catalog/CatalogFilter.cs`
  - Filters catalog entries by category, unit, or enabled state.
- `Assets/Core/Scripts/Catalog/CatalogStringHelper.cs`
  - String formatting helpers used by the catalog UI.
- `Assets/Core/Scripts/Catalog/CatalogThumbnailLoader.cs`
  - Loads and assigns thumbnail images for catalog buttons.
- `Assets/Core/Scripts/Catalog/CatalogUiFactory.cs`
  - Instantiates and configures catalog UI elements at runtime.
- `Assets/Core/Scripts/GameSession.cs`
  - Holds currently loaded bundles and container return scene name.
- `Assets/Core/Scripts/ContainerReturnOverlay.cs`
  - Runtime return button and scene return behavior.
- `Assets/Core/Scripts/UI/ArcCarousel.cs`
  - Arc-based carousel layout for interactive selection UI.
- `Assets/Core/Scripts/UI/CarouselHomeBackground.cs`
  - Swaps background image based on selected carousel item (uses catalog `home` field).
- `Assets/Core/Scripts/UI/CatalogStatusOverlay.cs`
  - Overlay showing download/load status for the selected interactive.
- `Assets/Core/Scripts/UI/AspectRatioEnforcer.cs`
  - Enforces a fixed aspect ratio on UI elements.
- `Assets/Core/Scripts/UI/UIGroup.cs`
  - Helper for showing/hiding groups of UI elements together.

### Per-Interactive Assemblies

The following interactive assemblies are present in this repository:

| ID | Script Folder | Assembly | Notes |
|----|--------------|----------|-------|
| ID101 | `Assets/Core/InteractiveScripts/ID101_Scripts/` | `ID101.asmdef` | |
| ID102 | `Assets/Core/InteractiveScripts/ID102_Scripts/` | `ID102.asmdef` | |
| ID106 | `Assets/Core/InteractiveScripts/ID106_Scripts/` | `ID106.asmdef` | Whack-a-Mole |
| ID213 | `Assets/Core/InteractiveScripts/ID213_Scripts/` | `ID213.asmdef` | |
| ID232 | `Assets/Core/InteractiveScripts/ID232_Scripts/` | `ID232.asmdef` | |
| ID313 | `Assets/Core/InteractiveScripts/ID313_Scripts/` | `ID313.asmdef` | **Missing from `link.xml`** — add before release |
| ID107 | _(no scripts yet)_ | `ID107.csproj` | Planned |
| ID324 | _(no scripts yet)_ | `ID324.csproj` | Planned |

## Catalog JSON Contract

Location expected by default:

- `{serverRoot}/{grade}/catalog.json`

Top-level schema:

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

### Field Notes

- `id`: Required for loading and button wiring.
- `enabled`: Required for visibility in menu.
- `title`: Optional label override (falls back to `id`).
- `image`: Optional thumbnail filename/path.
- `home`: Optional background image used by `CarouselHomeBackground`.
- `category`: Optional, defaults to `general` when empty.
- `unit`: Optional, defaults to `general` when empty.
- `bundleBaseName`: Optional but strongly recommended. Exact bundle base name used to compose:
  - `<bundleBaseName>.assets`
  - `<bundleBaseName>.scenes`
- `bundleVersion`: Optional but strongly recommended. Used by cache key to invalidate stale device cache.

## Bundle Naming And Paths

When `folder` and `bundleBaseName` are provided:

- Assets URL: `{serverRoot}/{folder}/{bundleBaseName}.assets`
- Scenes URL: `{serverRoot}/{folder}/{bundleBaseName}.scenes`

When omitted, defaults are computed from grade/category/unit/id.

## Device Cache Behavior

Bundles are cached under:

- `Application.persistentDataPath/InteractiveCache/<cacheKey>/`

Cache key includes:

- interactive id
- bundle base name
- bundle version (if provided)

If you publish new bundles and do not change `bundleVersion`, old local cache may continue to load.

## Add A New Interactive (Detailed Checklist)

1. Create scripts and assembly
- Add scripts under `Assets/Core/InteractiveScripts/IDXXX_Scripts/`.
- Create an asmdef for the new interactive assembly (for example `ID324.asmdef`).
- Ensure namespaces and script class names match scene components.

2. Create/build interactive scenes and assets
- Ensure scene bundle contains at least one playable scene.
- Prefer having a scene named `Title` to match current startup preference.
- Build Android AssetBundles for both:
  - `<bundleBaseName>.assets`
  - `<bundleBaseName>.scenes`

3. Host bundles on server
- Upload bundles to folder that matches catalog `folder`.
- Verify files are reachable from device network.

4. Update `catalog.json`
- Add a new entry to `interactives`.
- Set `enabled: true`.
- Set `folder`, `bundleBaseName`, and bump `bundleVersion`.

5. If adding a new assembly, update stripping preservation
- Create or update `Assets/link.xml` and preserve interactive assemblies used only by bundle-loaded scenes.
- Current `Assets/link.xml` (add your new ID here):

```xml
<linker>
  <assembly fullname="Core" preserve="all"/>
  <assembly fullname="Assembly-CSharp" preserve="all"/>
  <assembly fullname="ID101" preserve="all"/>
  <assembly fullname="ID102" preserve="all"/>
  <assembly fullname="ID106" preserve="all"/>
  <assembly fullname="ID213" preserve="all"/>
  <assembly fullname="ID232" preserve="all"/>
  <assembly fullname="ID313" preserve="all"/><!-- add ID313 — currently missing -->
  <!-- add new interactive IDs here -->
</linker>
```

6. Android build settings sanity check
- In `ProjectSettings/ProjectSettings.asset`:
  - `stripEngineCode: 0`
  - `managedStrippingLevel.Android: 0`
- Rebuild and reinstall player after stripping-related changes.

7. Clear stale cache for validation
- Uninstall/reinstall app or clear app data on test device.

8. Validation run
- Open container scene.
- Confirm catalog loads and interactive appears.
- Tap interactive and verify:
  - bundles download/load
  - start scene opens
  - return overlay appears in `Title`
  - return button goes back to container scene

## What To Change When Updating Existing Interactives

### Only content changed (same script API)

- Rebuild bundles.
- Upload new bundles.
- Increase `bundleVersion` in catalog entry.

### Script/API changed for interactive

- Rebuild interactive assemblies and bundles.
- Ensure player build includes required assemblies via `link.xml`.
- Rebuild Android player.
- Increase `bundleVersion`.

### Folder/name changed on server

- Update catalog `folder` and/or `bundleBaseName`.
- Increase `bundleVersion`.

## Android Crash Troubleshooting

Symptom:

- `Could not produce class with ID 124`
- Unity log mentions class stripped from build.

Common causes:

- Required scripts/modules stripped by IL2CPP/managed stripping.
- Scene bundle built with mismatched platform/editor settings.
- Device loading stale cached bundles.

Resolution order:

1. Ensure `stripEngineCode: 0` and Android managed stripping disabled.
2. Preserve interactive assemblies in `Assets/link.xml`.
3. Rebuild Android player.
4. Rebuild Android bundles and upload.

## Editor Setup Notes

- `InteractiveController.serverRoot` should point to reachable server endpoint.
- `InteractiveController.grade` + `catalogFileName` determine catalog URL.
- `InteractiveCatalogMenu` must reference:
  - controller
  - unit/interactive button containers and prefabs (if using prefab-driven UI)
- Optional: use `CatalogMenuNavigator` for animated screen transitions.

## Known Constraints

- Loader currently relies on scene-bundle fallback for first scene, rather than strict manifest deserialization from asset bundle.
- Startup scene preference is `Title` if present; otherwise first scene path in bundle.
- If `catalog.json` is malformed or empty, menu remains unavailable and logs warnings.

## Recommended Operational Workflow

