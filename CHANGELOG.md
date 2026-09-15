# Changelog

## [0.7.1] - 2026-09-15

- Render sandbox notifications with the staged visual theme and explain draft versus runtime selection. Preserve Lab tabs, selection and authoring state through reload.


## [0.7.0] - 2026-09-15

- Restore the Lab's icon-and-border card as a package-owned runtime row prefab, shared by the Lab and default lists.
- Add a project notification prefab selector and **Change notification to default** in Appearance. Default follows the package prefab without copying it into Assets.
- Preserve active notification state, timers and feedback when changing row prefabs; retain the chosen prefab across disable/enable.
- Keep severity icons square when themed typography increases the row height.
- Fit the real runtime preview to its pane without squeezing the card, and inherit runtime canvas layers for external row prefabs.

## [0.6.0] - 2026-09-15

- Require registered definitions at the store and scheduler boundary, including direct C# calls. Reject invalid batches before state or feedback changes.
- Render the actual runtime prefab in the Lab, with shared layout, typography and transitions. Temporary editor definitions are scoped and cleaned up.
- Migrate the Basic sample to editable declarations and explicit catalog lookup.

## Asset workflow — Unreleased

- Use shared package-aware palette/recipe selection and create/customize actions; choose configured audio before the bundled fallback.

## [0.5.0] - Unreleased

- Add reusable notification content, severity, lifetime and audio defaults; expose one-line typed Show/Resolve and Inspector triggers in the shared Notification Lab workflow.
- Include a playable Definition Workflow sample with configured hosts, short callers and usage documentation.
- Align declared package dependencies with the definition-authoring development wave.

- Smoothly reflow existing runtime and Lab rows when items are inserted, reordered or removed. Keep exiting slots until their exit finishes; use a stable top anchor so growing lists no longer recenter.
- Add independent Animate list changes and List movement duration controls. Existing serialized settings default to smooth movement; instant layout remains available.

- Add Fade + Scale, Fade + Slide, Scale + Slide and Fade + Scale + Slide for entrance/exit in both the Lab and runtime rows. Preserve existing serialized transition values and defaults.
- Rename the Lab's misleading Advanced positioning foldout to More motion options; actual list position is configured on NotificationListView, not in this foldout.

- Add a playable scene demonstrating persistent warnings, resolution, timed success, bounded overflow and bundled semantic audio.
- Expose sample title, message and timeout in the Inspector while using the package-owned notification host and facade.


## [0.4.0] - 2026-09-11

- Share one live preview between Test and Appearance, preserving rows, countdowns and selected presentation settings when changing tabs.
- Apply the runtime-owned semantic color policy to editor rows; preview actual entrance/exit transitions and reserve exiting row slots before showing replacements.
- Share renderer-independent reversible row transitions with runtime: identical easing, scale and slide distance; None transitions complete immediately. Overflow moves retain outgoing slots without hidden rows consuming layout space.
- Preview the connected list's authored row palette when visual styling is disabled or a semantic role is missing.
- Replace application-specific bundled font references with Theming's Inter assets. Expand stacked rows for larger typography while preserving padding, visible glyphs and chosen font sizes; document TMP's official Essential Resources prerequisite.
- Restore authored runtime row colors, typography and geometry when visual styling is disabled, including clones of already-themed inactive templates and pooled rows; reapply on settings/palette changes.
- Defer appearance-triggered row sorting until reconciliation finishes so priority changes and removals cannot skip rows or retain stale capacity.
- Require Editor 1.11.0 and Theming 1.7.0 for shared row presentation ports and visual feature lifecycle support.

## [0.3.5] - 2026-09-11

- Match Test, Appearance, Audio and custom Inspectors to the shared style; retain isolated previews, runtime injection, dismissal, sound and motion settings.
- Require Editor 1.10.6 for the shared native controls, typography, responsive layouts and accessible interaction states.

## [0.3.4] - 2026-09-10

- Use shared sliders for message capacity and the shared audio-disabled state. Keep message injection, lifetime, motion and lazy follow independent of visual palette adoption.

## [0.3.3] - 2026-09-09

### Changed

- Adopt the shared Editor 1.7 workspace presentation: neutral surfaces, readable typography, consistent actions and aligned controls.
- Preserve package workflows and native serialized editing; this is an editor-only presentation update.

## [0.3.2] - 2026-09-09

- Register package tooling and navigation actions as shared Control Center pages. Preserve the domain workflow while using Editor-owned submenus, in-window navigation, and UI scaling.

## [0.3.1] - 2026-09-09

- Keep sidebar navigation in the current workspace and retain page drafts while switching tools.
- Support explicitly opening independent workspaces through the sidebar context menu.


## 0.3.0 - 2026-09-09

- Connect Notification Lab to the shared Editor-owned workspace. Test, Appearance
  and Audio tabs now bind the existing lifecycle, runtime targeting, presentation
  overrides, palette audition and recipe workflows through a notification adapter.
- Show real visible/overflow counts and live countdowns, preserve row identity
  while updating, and keep test-message cleanup isolated from application warnings.
- Remove the lab's package-local IMGUI layout. No runtime dependencies or
  notification lifetime policies changed.

## 0.2.0 - Unreleased

- Separate notification read/command ports; add optional Deucarian lazy follow, a simplified runtime lab, pinned actions, saved test recipes and processed semantic audio preview.

## [0.1.0] - Unreleased

### Fixed

- Separated title and instruction text in the shared row prefab and its repair generator.
  Added geometry and rendered-glyph regression coverage, including long content and pooled rows.

### Added

- Persistent and timed notification lifetimes, with expiry after activation debounce
  and no timer restart on repeated active updates.
- Five-slot default presentation with priority/age ordering, non-destructive overflow
  and a +N more indicator; fading rows keep their slot until exit completes.
- Independent None/Fade/Scale/Slide show and hide controls using Deucarian UI motion.
- Live Deucarian visual theme roles and optional typography on notification rows.
- Lifetime, countdown, mixed-overflow scenarios and temporary runtime presentation
  overrides in Notification Lab, plus a shared-chrome list inspector.

- Package-level Notification Lab in Deucarian Control Center with isolated editor
  message preview, editable examples, activation/recovery timing, repeated and
  batched requests, per-row resolution, reset, and semantic audio audition.
- Lab lifecycle, batching, isolation, profile selection and cleanup regression tests.
- Play Mode destinations in Notification Lab: inject uniquely identified test
  messages into existing scene presenters with real application layout and audio.
- Add independent messages, update the last message, and clean up only lab-owned
  messages on reset, destination changes, scene loss, window close and Play Mode exit.
- Editor-only weak presenter discovery; no player registry or host scene changes.

- Keyed, persistent notification store with atomic batches.
- Activation and recovery episode controller with an injectable monotonic clock.
- Coalesced semantic feedback requests.
- Sanitized automatic Diagnostics provider.
- Reusable presenter contracts and Unity uGUI presentation.
- Optional Theming audio bridge.
- Sanitized pending-activation/recovery scheduler diagnostics.
- Reproducible non-modal uGUI prefab and view style with PlayMode coverage.
