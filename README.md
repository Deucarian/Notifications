# Deucarian Notifications

## Asset selection and project defaults

Notification Lab's audio selector uses the configured project audio palette, falling back to the bundled Deucarian palette. Choose can find project and installed package assets; Create and Customize make project assets explicitly. The recipe picker can create a recipe from the current lab configuration; select a recipe and choose Load to apply it. Merely opening the Lab does not save a recipe or change runtime configuration.

## Registered definitions are required

Every activation, including `NotificationStore.ApplyBatch` and delayed conditions, must resolve an ID from the explicitly assigned catalog. Unknown IDs throw before state, events or feedback change. Creating a `NotificationDefinition` or `NotificationKey` alone is not registration. Content overrides remain supported for registered IDs.

Create definitions in the Lab's Definitions tab. The generated project catalog is loaded by `NotificationHost`; custom hosts pass it to `new NotificationStore(feedback, catalog)`. `Warn` preserves the registered warning's policy and rejects keys registered with a different severity.

The Lab renders the real runtime prefab in an isolated preview scene. Its temporary test messages use editor-only scoped declarations, never project definitions, and are removed on disconnect. Create a definition explicitly to use that message in the app.

## Typed definition workflow

Create or edit the notification in Definitions or the Notification Lab. Its title, message and audio policy are reused here.

Start with the [Definition Workflow walkthrough](Documentation~/DefinitionWorkflow.md).
Import **Definition Workflow** in Package Manager for a configured sample scene
and short caller scripts. Definitions can be edited as assets or editable C# declarations; generated keys
work in code and Inspector dropdowns.

After creating a definition named `ConnectionLost` and configuring the scene
host, callers only need:

```csharp
using Deucarian.Notifications;
using Deucarian.Generated;

NotificationManager.Show(ProjectNotifications.ConnectionLost);
NotificationManager.Resolve(ProjectNotifications.ConnectionLost);
```

The definition supplies severity, title, message, lifetime and sound. A
`NotificationTrigger` exposes the same Show/Resolve operations to Unity events;
choose its notification from the Inspector dropdown. No caller-owned store or
presenter is required. Follow the walkthrough below for the one-time host,
view and audio setup.


For simple calls and setup, see [Simple usage](Documentation~/SimpleUsage.md).

## One final preview

Test and Appearance share one live message list. Changing tabs preserves the
messages, timers, overflow, theme and transition progress. Adjust the visible
limit, entrance/exit method or duration in Appearance, then return to Test to
add or resolve messages using those exact settings. Replay entrance changes
only the animation, never the notification lifetime.

When project Visual styling is enabled, runtime rows and the editor preview
resolve colors from Theming. Disabling it restores authored runtime row colors
and typography. The editor-only sandbox uses the package's default row style;
a connected runtime list uses its theme override/provider and view-style roles.
Lazy follow still requires a running XR/camera-space list and never moves an
editor scene camera. Requires Editor 1.14.0 and Theming 1.13.0.

## In-window navigation

The left sidebar changes pages in the current window, keeping each page's draft and session alive. Right-click a sidebar item and choose **Open in new window** for an independent workspace. Closing a workspace releases its pages; ordinary page changes do not reset lab messages or stop package operations.


## Shared workspace (0.3.0)

Migrate the live Notification Lab to the shared Editor workspace, preserving test-message isolation, runtime targeting, lifetime controls, motion previews, audio and saved recipes.

Requires Editor 1.5.0 or newer. Development is delivered through Git `#develop`; this change does not promote the stable `#main` channel.

## Notification Lab workspace (development)

Open Notification Lab from Control Center. Its new Test, Appearance and Audio
tabs use the paired development version of `com.deucarian.editor`; the Editor
package owns all layout, colours, typography and reusable controls.

- Add persistent or timed messages; expand **Test scenarios** for batching,
  deduplication, delays and overflow tests. **Clear test messages** removes this
  lab's active and pending messages immediately; delayed recovery remains available
  as a separate scenario action.
- Select **Running app** in Play Mode to connect an active list. Multiple lists
  get an explicit target picker. Connecting never replays previous examples.
- Appearance changes preview limits locally and apply motion/lazy-follow overrides
  to a connected list. Its original settings are restored on disconnect. The
  editor's lifecycle preview does not simulate camera motion or runtime transitions.
- Audio uses palette audition in Editor preview, and the application's existing
  audio route when connected. It never plays both for the same activation.
- Recipes and inputs survive reopening; live messages and connections do not.

The sidebar opens the paired development Control Center, Package Installer,
Theme Manager, Audio Palette Lab and Diagnostics workspaces. Specialist workflows
remain owned by those packages.

`com.deucarian.notifications` owns reusable keyed notification lifecycle. It
keeps one authoritative immutable snapshot, applies atomic update batches, and
requests at most one semantic feedback cue for newly activated items in a
batch. Application adapters own the conditions; reusable notification definitions
own the default message copy and feedback.

## Advanced core composition

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

### Persistent and timed lifetimes

Severity and lifetime are independent. Existing definitions default to
`NotificationLifetime.UntilResolved` and never expire automatically. For a timed
notice, pass `lifetime: NotificationLifetime.Timed(5)` to its definition and use
`NotificationEpisodeController.EvaluateBatch` and `Tick` to drive its lifecycle.
The store itself has no background clock: applying commands directly does not
schedule expiry.

The timer begins when activation debounce finishes, including if the message is
in overflow or its view is hidden. Updating an active notice does not restart its
timer. Expiry resolves immediately, independent of recovery debounce; an explicit
resolve may finish earlier. An expired notice stays gone until explicitly requested
again. Use persistent lifetimes for ongoing hardware faults.

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

Choose **Lifetime > Until Resolved / Timed** and set **Duration** for timed notices.
The preview shows remaining time. **Add 10 mixed messages (overflow test)** adds
different severities and both lifetimes in one batch with one ping.

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

The **Presentation** card controls **Maximum visible**, **Show transition**, **Hide
transition**, and their durations. These controls apply live to a connected
`INotificationPresentationTarget`, and restore its original settings on disconnect.
Custom hosts can delegate this interface to their package `NotificationListView`.
Editor-only preview limits the displayed rows but motion and visual themes are
previewed in the actual runtime list, not simulated in IMGUI.

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

The bundled uGUI prefab uses Theming's licensed Inter font and atlas material;
it does not reference a font from HoloHelmet or another application. Import
Unity's **TMP Essential Resources** in the consuming project (Window > TextMeshPro
> Import TMP Essential Resources) for TMP's required shaders and settings. Unity 6
ships this official resource archive with uGUI; older Unity versions ship it with
TextMeshPro. A package font does not replace this Unity prerequisite.

Notification rows preserve their authored padding and expand their line slots
and list height for larger themed typography. The selected font size is not
silently reduced. Title and instruction stay separate, including when rows are
reused or typography changes live. Custom non-stacked row layouts remain authored.

`NotificationPresenter` is engine-independent. `NotificationListView` and
`NotificationRowView` provide the uGUI implementation. Late/recreated views
render the current snapshot without replaying activation feedback.

Lists show **five messages by default**, configurable from 1 to 20, plus a compact
**+N more** indicator. Selection is highest priority, then severity, then oldest
activation/episode first. Overflow stays active in the store: clearing a visible
message promotes the next one without another activation ping. A more urgent
message can displace a lower-priority one. Timed overflow can expire before it is
visible; it is not a backlog of stale notices.

Choose **None**, **Fade**, **Scale**, or **Slide** separately for showing and hiding.
Defaults are a 0.18-second fade in and 0.14-second fade out. None (or duration zero)
disables motion. Deucarian UI owns the reversible transition; Notifications adapts
its progress to rows using unscaled time. Rows are keyed, so content updates do not
restart motion and reappearing rows can reverse an exit. Exiting rows retain a slot
until their animation completes: even during replacement the maximum is respected.
Animation never resolves a notification or requests sound.

Rows consume Deucarian Theming's nearest/active provider or project theme. Surface,
primary/secondary text, semantic status colors and an optional typography profile
update with the theme. `NotificationViewStyle` selects text/surface role IDs and
supplies fallback severity accents. The list inspector uses the shared Deucarian
editor UI. The existing Theming dependency now covers visual styling as well as audio;
`com.deucarian.ui` supplies motion, without introducing a second animation system.

The package ships `DefaultNotificationList.prefab` and
`DefaultNotificationViewStyle.asset` under its Resources defaults. The prefab
uses safe-area-relative normalized anchoring, leaves all graphics non-raycast,
and removes inactive rows without gaps. Use **Assets > Create > Deucarian >
Notifications > Repair Default View Assets** to reproduce both assets.

The default 620 × 82 row reserves separate title and instruction rectangles, with
nine-unit vertical padding and a four-unit gap between labels. Long text uses
ellipsis within its own rectangle; the severity stripe and ten-unit row spacing
stay independent of text and animation.

## Testing and optional motion

Open **Notification Lab** from the Deucarian Control Center. The pinned
destination identifies editor-only lifecycle preview versus the running
application. Start with the message, severity, lifetime and **Add message**.
Advanced controls contain overflow/repetition tests, audio, presentation and
reusable recipe assets; recipes never save a live connection or injected rows.

Presentation settings include **Lazy follow anchor**. This is Deucarian UI
motion applied to the whole world-space or screen-space-camera list, with movement/rotation dead zones
and response tuning. Turning it off or disabling the list restores fixed local
placement. Screen-space overlays (and camera-space canvases without an assigned
camera) remain fixed. The list never creates, replaces, or moves a camera.
Editor-only lifecycle preview does
not claim to reproduce runtime theme or motion.

Presenters consume `INotificationSource`; condition adapters and test injection
consume `INotificationCommands`. Diagnostic observers only receive immutable
snapshot functions, not the store's mutation API.

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
