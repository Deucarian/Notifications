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

## Notification Lab (Unity Editor)

Open **Deucarian Control Center > Experience > Notifications > Open Notification Lab**.
The lab also registers as **Notification Lab** in Control Center search.

1. Press **Add new message** to display an example warning. Every click adds a
   separate message. Edit the title, body, severity and delays to try other messages.
   **Update last** changes the last message without adding a duplicate or another ping.
2. Use **Repeat same message 10×** to verify one row and one ping per active episode.
3. Use **Show three at once** to test ordering and one coalesced ping.
4. **Resolve** or **Resolve all** simulates recovery using the delay captured when
   the message was shown. Resolving a pending activation cancels it.
5. **Reset session** clears messages, pending work and counters immediately.

Select an **Audio Palette Set**, choose Default / XR / WebGL / Desktop / Mobile,
and enable **Sound on new messages** to audition the resolved feedback clips.
The lab adopts a selected palette set, or the only palette set under Assets;
otherwise it uses the package defaults. Missing or muted audio never blocks messages.
The ping counter counts lifecycle requests, including muted requests, not successful playback.

### Test in your running application

1. Start **Play Mode** in your existing scene and open the Notification Lab.
2. In **Message destination > Destination**, select your application's warning
   list instead of **Editor preview only**. Active `NotificationPresenter` instances
   with scene-component views appear automatically; no host code or prefab change is needed.
3. Press **Add new message**. It appears in the application's existing list, using
   its real layout, audio palette, volume and pitch. Repeated clicks stack separate messages.
4. Use each row's **Resolve**, **Resolve all**, or **Reset session** to remove test
   messages. These actions never resolve the application's real hardware warnings.

Runtime injection uses unique lab-owned IDs. Changing destination, closing the
lab, recompiling scripts, leaving Play Mode, or losing the target removes its test
messages and pending work. Selecting a destination clears old examples without
playing a sound. If the scene/list disappears, select a new destination manually.
The lab does not create a camera, canvas, singleton, or scene object, and does not
change sensor state. Visibility and audio still follow the application's own
configuration (including hidden UI, muted audio, and missing listeners).

**Editor preview only** remains available without Play Mode. Its clip audition
reuses Theming's editor preview service and editor preview volume. When connected
to a runtime list, this audition is disabled to avoid double audio; the application's
configured feedback sink handles the ping instead. The counter measures test
activation batches, not successful playback.

Opening the lab creates no scene objects and never writes project assets. Presenter
discovery is compiled only for the Unity Editor and is absent from player builds.
Switching palettes or profiles never auto-plays a sound.

## Audio integration

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
