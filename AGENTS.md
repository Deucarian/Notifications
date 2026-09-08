# Deucarian Notifications Agent Notes

Package ID: `com.deucarian.notifications`
Repository: `Deucarian/Notifications`

Follow the canonical Deucarian governance in Package Registry `ARCHITECTURE.md`.

## Ownership

This package owns keyed notification lifecycle, immutable active snapshots,
atomic update batches, deterministic ordering, activation/resolution episodes,
coalesced semantic feedback requests, and notification-specific presenters.

Registered capability:

- `notification-lifecycle`

This package must not own modal routing, generic UI primitives, audio palettes,
generic media playback, localization, XR hardware, or application health policy.

## Dependencies

- `com.deucarian.diagnostics`: mandatory sanitized operational diagnostics.
- `com.deucarian.theming`: semantic audio and visual theme integration.
- `com.deucarian.ui`: reusable visibility transitions; notification views only adapt them to their rows.
- Unity UGUI and TextMeshPro: notification-specific uGUI presenter.

No production logging is emitted. If logging is introduced, add Deucarian
Logging and update all manifests together. Direct Unity Debug calls are
forbidden.

## Validation

Run the shared Package Registry validator and all EditMode/PlayMode tests.
Work on `develop`; do not edit `main`, publish, or create releases as part of
ordinary feature work.
