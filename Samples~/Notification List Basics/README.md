# Notification List Basics

1. Drag the bundled `DefaultNotificationList` prefab into a Canvas.
2. Add `NotificationListExample` to a GameObject and assign that list view.
3. Connect buttons to `ShowWarning` and `ResolveWarning`.

The sample composes a `NotificationPresenter` and restores the current snapshot
when enabled. Provide a semantic feedback sink only when audio is desired.
