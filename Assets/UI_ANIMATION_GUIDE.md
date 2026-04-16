# UI Animation System — Setup & Reference

This guide covers `UIGroup` and `CatalogMenuNavigator` — the two scripts that power animated UI navigation.

---

## Table of Contents

1. [Overview](#overview)
2. [UIGroup — Inspector Setup](#uigroup--inspector-setup)
3. [Animation Type Reference](#animation-type-reference)
4. [CatalogMenuNavigator — Inspector Setup](#catalogmenunavigator--inspector-setup)
5. [Step-by-Step: Category → Unit → Entry Flow](#step-by-step-category--unit--entry-flow)
6. [Back Button Setup](#back-button-setup)
7. [Calling from Code](#calling-from-code)
8. [DOTween Upgrade Path](#dotween-upgrade-path)

---

## Overview

The system has two scripts:

| Script | File | Purpose |
|---|---|---|
| `UIGroup` | `Assets/Core/Scripts/UIGroup.cs` | Attach to any UI container. Gives it `Show()` and `Hide()` with animation. |
| `CatalogMenuNavigator` | `Assets/Core/Scripts/Interactive/CatalogMenuNavigator.cs` | Extends `InteractiveCatalogMenu`. Wires UIGroup transitions to catalog navigation events. |

The key rule: **`Animation Type` on each `UIGroup` fully controls behavior.**

- `None` → `Show()` snaps in instantly. `Hide()` does **nothing** — the group stays on screen.
- Any other type → `Show()` animates in, `Hide()` animates out then deactivates the GameObject.

---

## UIGroup — Inspector Setup

Add `UIGroup` to a container GameObject in your Canvas hierarchy.

| Field | Type | Description |
|---|---|---|
| **Animation Type** | Enum | Which animation to play. See table below. |
| **Duration** | Float | How long the animation takes in seconds. Default `0.25`. |
| **Slide Distance** | Float | Pixels the element travels from its off-screen start. Only applies to Slide types. Default `60`. |
| **Start Hidden** | Bool | If checked, the GameObject is deactivated immediately on scene start. |

> **Note:** `UIGroup` automatically adds a `CanvasGroup` to the GameObject if one is not already present. Do not manually add one — it will be shared.

---

## Animation Type Reference

| Value | Show behaviour | Hide behaviour |
|--------|---------------|----------------|
| `None` | Snaps to full alpha, full scale, final position instantly | **Does nothing.** Group stays visible and active. |
| `Fade` | Fades from alpha 0 → 1 | Fades from alpha 1 → 0, then deactivates |
| `SlideFromLeft` | Slides in from the left + fades in | Slides out to the left + fades out, then deactivates |
| `SlideFromRight` | Slides in from the right + fades in | Slides out to the right + fades out, then deactivates |
| `SlideFromTop` | Slides in from the top + fades in | Slides out to the top + fades out, then deactivates |
| `SlideFromBottom` | Slides in from the bottom + fades in | Slides out to the bottom + fades out, then deactivates |
| `ScalePop` | Scales from 0 → 1 with a spring overshoot | Scales from 1 → 0, then deactivates |

---

## CatalogMenuNavigator — Inspector Setup

Use `CatalogMenuNavigator` **instead of** `InteractiveCatalogMenu` on loader GameObject.

Under the **"Navigation Groups"** header you will see three fields:

| Field | Assign | Behaviour |
|---|---|---|
| **Category Group** | The container holding category buttons | Hides when a category is clicked |
| **Unit Group** | The container holding unit buttons | Shows after category is picked; hides when a unit is clicked |
| **Entry Group** | The container holding interactive entry buttons | Shows after a unit is picked |

All three fields are optional. Leave a field empty to skip that transition entirely.

---

## Step-by-Step: Category → Unit → Entry Flow

### 1. Create the hierarchy

```
Canvas
├── CategoryGroup          ← UIGroup here
│   └── (category buttons)
├── UnitGroup              ← UIGroup here
│   └── (unit buttons or scroll view)
└── EntryGroup             ← UIGroup here
    └── (interactive entry buttons)
```

### 2. Configure CategoryGroup

- Select `CategoryGroup`
- Add Component → `UIGroup`
- **Animation Type:** `Fade` (or whatever you want)
- **Start Hidden:** ☐ unchecked (visible at scene start)

### 3. Configure UnitGroup

- Select `UnitGroup`
- Add Component → `UIGroup`
- **Animation Type:** `Fade` (or `None` if you want it to stay visible alongside the entry group)
- **Start Hidden:** checked (hidden at scene start; shown only after a category is picked)

### 4. Configure EntryGroup

- Select `EntryGroup`
- Add Component → `UIGroup`
- **Animation Type:** `Fade` (or any type)
- **Start Hidden:** checked (hidden at scene start; shown only after a unit is picked)

### 5. Set up CatalogMenuNavigator

- Select your loader/controller GameObject (the one that had `InteractiveCatalogMenu`)
- **Remove** `InteractiveCatalogMenu` (if present)
- Add Component → `CatalogMenuNavigator`
- Fill in all the existing `InteractiveCatalogMenu` fields as before
- Under **"Navigation Groups"**, assign:
  - `Category Group` → drag in `CategoryGroup`
  - `Unit Group` → drag in `UnitGroup`
  - `Entry Group` → drag in `EntryGroup`

### 6. The automatic flow

```
Player clicks category button
  → CategoryGroup.Hide() fires (fades out)
  → on complete: UnitGroup.Show() fires (fades in)

Player clicks unit button
  → UnitGroup.Hide() fires (fades out)
  → on complete: EntryGroup.Show() fires (fades in)
```

---

## Back Button Setup

1. Create a back button in your Canvas
2. Select it → Inspector → `Button` component → `On Click ()`
3. Click **+** → drag the loader GameObject into the slot
4. From the function dropdown: **CatalogMenuNavigator → GoBack ()**

The `GoBack()` method automatically detects what is currently visible:
- If `EntryGroup` is visible → hides it, shows `UnitGroup`
- If `UnitGroup` is visible → hides it, shows `CategoryGroup`

---

### CanvasGroup and why it is used

Unity's `CanvasGroup` component controls three properties for an entire container at once:
- `alpha` — opacity of the group and all children
- `interactable` — whether child buttons respond to input
- `blocksRaycasts` — whether the group blocks mouse/touch events

Using `CanvasGroup` means it can fade/disable an entire panel with one component instead of touching every child Image and Button individually.

### Buttons are interactable during the fade

When `SnapHidden()` runs (when hiding finishes), it sets `blocksRaycasts = false` and `interactable = false`. If `Show()` only restored these at the *end* of the coroutine, buttons would be invisible to clicks during the entire fade-in — you'd see them animating in but tapping them would do nothing.

The fix: `RunShow` sets `interactable = true` and `blocksRaycasts = true` **before** starting the alpha animation. This means buttons are immediately clickable the moment `Show()` is called, even while they are still fading in visually.

### Why `shownPosition` is recorded in Awake

Slide animations need to know where the "home" position of the panel is so they can calculate the offset start position and lerp back to it. `shownPosition` is recorded once in `Awake()` from `rectTransform.anchoredPosition` — the position you set in the Editor. If its not record this, the slide animation would not know where to slide *to*.

### The callback pattern (`Action onComplete`)

```csharp
categoryGroup.Hide(() => unitGroup.Show());
```

`Hide()` accepts an optional `Action` (a function with no parameters and no return value). The callback is invoked at the very end of the coroutine, after the animation completes and the GameObject is deactivated. This chains animations: category hides *fully*, then unit starts showing. Without the callback, both animations would start at the same time.

### Why `None` skips Hide entirely

When `Animation Type` is `None`, `Hide()` immediately calls `onComplete?.Invoke()` and returns — it does not deactivate the GameObject, does not change alpha, does nothing. This lets you use the same `CatalogMenuNavigator` wiring but keep a group permanently visible. For example, if you want the unit group to stay on screen while entry buttons appear alongside it, just set `Unit Group → Animation Type = None`.

### Easing functions explained

Raw linear interpolation (`Lerp`) moves at a constant speed — it feels mechanical. Easing functions remap the 0→1 progress value `t` to a curved one:

```csharp
// EaseOutQuad: starts fast, slows near the end (used for Show)
float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

// EaseInQuad: starts slow, speeds up at the end (used for Hide)
float EaseInQuad(float t) => t * t;

// EaseOutBack: overshoots slightly then springs back (used for ScalePop Show)
float EaseOutBack(float t)  // based on the standard "back" easing formula
```

`Show` uses `EaseOutQuad` so panels feel snappy — they arrive quickly then settle. `Hide` uses `EaseInQuad` so panels linger briefly then snap away — this gives the player time to register what's happening before it disappears.

`ScalePop` uses `EaseOutBack` which exceeds 1.0 temporarily (LerpUnclamped is needed for this), making the element scale slightly beyond its final size before snapping back. This gives a bouncy, satisfying feel.

### StopActive — interrupting animations

If `Show()` is called while a `Hide()` animation is still running (or vice versa), the old coroutine is stopped immediately via `StopActive()`. Without this, both coroutines would fight over `alpha` and `anchoredPosition` every frame, producing a flickery result.

---

## Calling from Code

```csharp
// Show a group (animate in)
myGroup.Show();

// Show, then do something after animation completes
myGroup.Show(() => Debug.Log("Fully visible"));

// Hide a group (animate out, then deactivate)
myGroup.Hide();

// Chain: hide one, then show another
panelA.Hide(() => panelB.Show());

// Instant, no animation
myGroup.ShowImmediate();
myGroup.HideImmediate();

// Check current visibility
if (myGroup.IsVisible) { ... }
```

---

## DOTween Upgrade Path

When DOTween is imported into the project, you can replace the coroutine bodies in `UIGroup.cs` with DOTween calls. The public API (`Show`, `Hide`, `ShowImmediate`, `HideImmediate`, `IsVisible`) stays identical — no changes needed in `CatalogMenuNavigator` or any other caller.

Example replacement for `RunShow` (Fade):

```csharp
// Current coroutine approach
private IEnumerator RunShow(Action onComplete) { ... }

// DOTween replacement (inside Show(), instead of StartCoroutine)
canvasGroup.alpha = 0f;
canvasGroup.DOFade(1f, duration).SetEase(Ease.OutQuad).OnComplete(() => onComplete?.Invoke());
```
