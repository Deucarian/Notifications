# Notification List Basics

Open **NotificationPlayground.unity** and press Play. Use Add warning, Resolve
warning, Timed success and Queue eight. The sample demonstrates stable IDs,
automatic expiry, the five-message visible limit, overflow and transition timing.

Inspect **Example canvas** for the message, timeout and audio-player references.
Inspect **DefaultNotificationList** for presentation settings and the host.
The host owns the service; NotificationPlayground uses the typed manager facade.
The bundled Deucarian theme/audio are explicitly wired. Feature switches in
Theming > Project setup are respected. Unmute Game view for the warning ping.

The built-in input module supports this scene without another input dependency.
In Input-System-only projects, use InputSystemUIInputModule instead.

For a lower-level, explicitly composed presenter example:

1. Drag the bundled `DefaultNotificationList` prefab into a Canvas.
2. Add `NotificationListExample` to a GameObject and assign that list view.
3. Connect buttons to `ShowWarning` and `ResolveWarning`.

The sample composes a `NotificationPresenter` and restores the current snapshot
when enabled. Provide a semantic feedback sink only when audio is desired.
## Smooth list changes

The list keeps its first-row anchor at the configured screen percentages (default: 5% from the left, 50% vertically). Existing rows move smoothly when a message changes the ordering, and close a gap after an exit finishes. This is independent of Fade/Scale/Slide on the message itself.

Use **Notification Lab → Appearance → Animate list changes**; tune **More motion options → List movement duration**, or turn list animation off for instant placement. In code, configure `NotificationPresentationSettings.reflowSeconds` and `instantLayout` on the list. Preview overrides are temporary; saving a Lab recipe preserves these settings.
