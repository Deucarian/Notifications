# Deucarian Notifications

`com.deucarian.notifications` owns reusable keyed notification lifecycle. It
keeps one authoritative immutable snapshot, applies atomic update batches, and
requests at most one semantic feedback cue for newly activated items in a
batch. Application adapters own the conditions and message copy.

## Runtime example

```csharp
using Deucarian.Notifications;

NotificationDefinition warning = new NotificationDefinition(
    "sample.connection.lost",
    NotificationSeverity.Warning,
    "Connection lost",
    "Please reconnect",
    priority: 100,
    feedbackRoleId: "deucarian.feedback.audio.warning");

using (NotificationStore store = new NotificationStore())
{
    store.ApplyBatch(new[] { NotificationCommand.Activate(warning) }, 1d);
    store.ApplyBatch(new[] { NotificationCommand.Resolve(warning.Id) }, 2d);
}
```

Use `NotificationEpisodeController` when conditions need activation and
recovery debounce. Use one controller per authoritative condition source and
call `EvaluateBatch` for values produced by the same source evaluation.

## Audio

The core only emits a semantic role ID through `INotificationFeedbackSink`.
`ThemedNotificationFeedbackSink` is the optional adapter to Deucarian Theming.
Missing audio is always a safe no-op and never affects visual state.

## Presentation

`NotificationPresenter` is engine-independent. `NotificationListView` and
`NotificationRowView` provide the uGUI implementation. Late/recreated views
render the current snapshot without replaying activation feedback.

The package ships `DefaultNotificationList.prefab` and
`DefaultNotificationViewStyle.asset` under its Resources defaults. The prefab
uses safe-area-relative normalized anchoring, leaves all graphics non-raycast,
and removes inactive rows without gaps. Use **Assets > Create > Deucarian >
Notifications > Repair Default View Assets** to reproduce both assets.

## Diagnostics and privacy

Every live `NotificationStore` and `NotificationEpisodeController`
automatically registers sanitized Diagnostics providers. They report active,
pending activation/recovery, transition, feedback, and scheduler lifecycle
counts only; message bodies, hardware identifiers, semantic IDs, URLs, and
application payloads are omitted.

## Host support

The core has no XR, Magic Leap, or application references. The same package is
intended for Unity WebGL, Windows, Android/mobile, and XR hosts. Hosts choose
their layout and feedback composition; `NotificationStore` behavior remains
identical.
