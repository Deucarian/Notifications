# Changelog

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
