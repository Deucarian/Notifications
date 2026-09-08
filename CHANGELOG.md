# Changelog

## [0.1.0] - Unreleased

### Added

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
