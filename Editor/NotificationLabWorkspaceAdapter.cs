using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Binds notification state and commands to Editor-owned controls. No visual tokens or layout rules.</summary>
    internal sealed class NotificationLabWorkspaceAdapter : IDisposable
    {
        private readonly DeucarianNotificationLabWindow host;
        private readonly DeucarianEditorLabWorkspace view;
        private readonly Dictionary<string, NotificationEditorTarget> targets = new Dictionary<string, NotificationEditorTarget>();
        private readonly Dictionary<NotificationEditorTarget, string> targetIds = new Dictionary<NotificationEditorTarget, string>();
        private int nextTargetId;
        private bool refreshing;
        private bool disposed;
        private NotificationLabAudioPanel audioPanel;
        private Deucarian.Editor.Definitions.DeucarianDefinitionPanel definitions;
        private readonly NotificationRuntimePreview preview;

        internal NotificationLabWorkspaceAdapter(VisualElement root, DeucarianNotificationLabWindow host)
        {
            this.host = host;
            view = new DeucarianEditorLabWorkspace(root,
                System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Application.dataPath)),
                "Notifications", "Add and resolve messages in the editor or running app.", host.ClearMessages, SelectTarget, "Definitions");
            definitions = new Deucarian.Editor.Definitions.DeucarianDefinitionPanel(view.Definitions,
                new Definitions.NotificationDefinitionSchema(), asset => { host.PreviewSavedDefinition((NotificationDefinitionAsset)asset); view.SelectTab(0); }, host.DefinitionState);
            DeucarianEditorWorkspaceNavigation.Populate(view.Workspace, "deucarian.notifications.lab", host.OpenAudioLab);
            BindComposer();
            BindAppearance();
            BindAudio();
            preview = new NotificationRuntimePreview(view, resolve: id => { host.Session?.Resolve(id); Refresh(); });
            var replay = DeucarianEditorWorkspaceControls.IconButton("Replay entrance", DeucarianEditorIconIds.Play,
                preview.Replay);
            replay.name = "motion-preview-play";
            replay.tooltip = "Replays the visible messages without restarting their timers.";
            view.MotionPreviewRoot.Add(replay);
            view.TabChanged += value => host.SelectedTab = value;
            view.SelectTab(host.SelectedTab);
        }

        private void Change(Action<NotificationLabRecipeData> update)
        {
            var inputs = host.Inputs;
            update(inputs);
            host.Inputs = inputs;
        }

        private void BindComposer()
        {
            var form = view.Composer;
            form.Choice("lab-type", "Type", Enum.GetNames(typeof(NotificationSeverity)), () => (int)host.Inputs.severity,
                value => Change(x => x.severity = (NotificationSeverity)value), new[] {
                    DeucarianEditorIconIds.Info, DeucarianEditorIconIds.Success, DeucarianEditorIconIds.Warning, DeucarianEditorIconIds.Error });
            form.Text("lab-title", "Title", () => host.Inputs.title, value => Change(x => x.title = value));
            form.Text("lab-body", "Message", () => host.Inputs.body, value => Change(x => x.body = value), true);
            form.Choice("lab-dismissal", "Dismissal", new[] { "Until resolved", "After a delay" }, () => (int)host.Inputs.lifetime,
                value => Change(x => x.lifetime = (NotificationLifetimeKind)value));
            var seconds = form.Number("lab-duration", "Seconds", () => host.Inputs.lifetimeSeconds, value => Change(x => x.lifetimeSeconds = value));
            form.VisibleWhen(seconds, () => host.Inputs.lifetime == NotificationLifetimeKind.Timed);
            form.Action("lab-add", "Add message", host.AddCustom, () => host.Session != null && !string.IsNullOrWhiteSpace(host.Inputs.title), true);
            var advanced = form.Section("More options", true);
            var scenarios = advanced.Section("Test scenarios", true);
            scenarios.Action("lab-timed", "Timed notice · 5 seconds", () => { Change(x => { x.lifetime = NotificationLifetimeKind.Timed; x.lifetimeSeconds = 5; }); host.AddCustom(); });
            scenarios.Action("lab-persistent", "Persistent warning · resolve manually", () => { Change(x => x.lifetime = NotificationLifetimeKind.UntilResolved); host.AddCustom(); });
            scenarios.Action("lab-three", "Show three at once", host.ShowThree);
            scenarios.Action("lab-overflow-test", "Add 10 mixed messages", host.ShowMixed);
            scenarios.Action("lab-repeat", "Repeat same message 10×", host.RepeatLast);
            scenarios.Action("lab-update-last", "Update last message", host.ShowCustom, () => host.HasLast);
            scenarios.Action("lab-resolve-last", "Resolve last message", host.ResolveLast, () => host.HasLast);
            scenarios.Action("lab-resolve-all", "Resolve all with recovery delay", () => host.Session?.ResolveAll());
            scenarios.Number("lab-show-delay", "Show delay", () => host.Inputs.activationDelay, value => Change(x => x.activationDelay = value));
            scenarios.Number("lab-recovery-delay", "Recovery delay", () => host.Inputs.recoveryDelay, value => Change(x => x.recoveryDelay = value));
            scenarios.Note(() => "Repeated active messages share one row. A simultaneous batch requests one ping. Delays are in seconds.");
            host.Recipes.Bind(advanced.Section("Test recipes", true), () => host.Inputs, value => host.Inputs = value);
            advanced.Note(() => "Test messages are temporary. Clearing or disconnecting removes only this lab's messages.");
            form.EnabledWhen(() => host.Session != null);
        }

        private void BindAppearance()
        {
            var form = view.Appearance;
            NotificationPrefabSelection.Bind(form, () => host.Connection?.Target.View as Component);
            form.Stepper("lab-maximum", "Visible messages", 1, 20, () => host.Inputs.presentation.maxVisible,
                value => Change(x => x.presentation.maxVisible = Mathf.Clamp(value, 1, 20)));
            var overflow = form.Choice("lab-overflow-policy", "Overflow", new[] { "Queue" }, () => 0, _ => { });
            overflow.SetEnabled(false);
            overflow.tooltip = "Overflow remains active. Timed messages expire from activation; persistent messages wait for resolution.";
            string[] transitions = { "None", "Fade", "Scale", "Slide", "Fade + Scale",
                "Fade + Slide", "Scale + Slide", "Fade + Scale + Slide" };
            form.Choice("lab-show", "Enter", transitions, () => (int)host.Inputs.presentation.show,
                value => Change(x => x.presentation.show = (NotificationTransition)value));
            form.Choice("lab-hide", "Exit", transitions, () => (int)host.Inputs.presentation.hide,
                value => Change(x => x.presentation.hide = (NotificationTransition)value));
            form.Slider("lab-show-seconds", "Duration", 0, 2, () => host.Inputs.presentation.showSeconds,
                value => Change(x => { x.presentation.showSeconds = value; x.presentation.hideSeconds = value; }));
            form.Toggle("lab-follow", "Lazy follow", () => host.Inputs.presentation.lazyFollow, value => Change(x => x.presentation.lazyFollow = value));
            form.Toggle("lab-reflow", "Animate list changes", () => !host.Inputs.presentation.instantLayout,
                value => Change(x => x.presentation.instantLayout = !value));
            var advanced = form.Section("More motion options", true);
            advanced.Root.AddToClassList("dw-foldout-panel");
            advanced.Slider("lab-hide-seconds", "Exit duration", 0, 2, () => host.Inputs.presentation.hideSeconds, value => Change(x => x.presentation.hideSeconds = value));
            var reflow = advanced.Slider("lab-reflow-seconds", "List movement duration", .05f, 1,
                () => host.Inputs.presentation.Sanitized().reflowSeconds, value => Change(x => x.presentation.reflowSeconds = value));
            advanced.VisibleWhen(reflow, () => !host.Inputs.presentation.instantLayout);
            var tuning = advanced.Section("Follow tuning", true);
            tuning.Number("lab-follow-position", "Movement dead zone", () => host.Inputs.presentation.follow.positionDeadZone, value => Change(x => x.presentation.follow.positionDeadZone = value));
            tuning.Number("lab-follow-rotation", "Rotation dead zone", () => host.Inputs.presentation.follow.rotationDeadZone, value => Change(x => x.presentation.follow.rotationDeadZone = value));
            tuning.Number("lab-follow-seconds", "Response seconds", () => host.Inputs.presentation.follow.smoothingSeconds, value => Change(x => x.presentation.follow.smoothingSeconds = value));
            tuning.Action("lab-reset-follow", "Reset follow tuning", () => Change(x => x.presentation.follow = Deucarian.UI.DeucarianLazyFollowSettings.Default));
            tuning.EnabledWhen(() => host.Inputs.presentation.lazyFollow);
            advanced.Note(() => host.Connection == null
                ? "Test and Appearance share the same messages, theme and transitions. Lazy follow needs a running camera-space or XR list; this editor preview never moves a scene camera."
                : host.Connection.SupportsPresentation
                    ? "Live overrides affect this list only and are restored on disconnect. Colours and typography follow the application's theme."
                    : "This custom view does not expose presentation settings; its host controls layout and motion.");
            advanced.Note(() => "Overflow stays active. Timed messages expire from activation, including in overflow; persistent messages wait for resolution.");
            form.EnabledWhen(() => host.Session != null && (host.Connection == null || host.Connection.SupportsPresentation));
        }

        private void BindAudio() => audioPanel = new NotificationLabAudioPanel(view.Audio.Root, host);

        internal void Refresh()
        {
            if (refreshing || disposed) return;
            refreshing = true;
            try
            {
                RefreshTargets();
                var settings = host.Inputs.presentation.Sanitized();
                preview.Configure(settings, host.Connection?.Target.View as Component);
                var visibleSnapshot = NotificationVisibility.Select(host.Snapshot, settings.maxVisible);
                var visible = new List<DeucarianEditorMessageData>();
                var hidden = new List<DeucarianEditorMessageData>();
                var visibleIds = new HashSet<NotificationId>();
                foreach (var item in visibleSnapshot.Items) { visible.Add(Row(item)); visibleIds.Add(item.Id); }
                foreach (var item in host.Snapshot.Items) if (!visibleIds.Contains(item.Id)) hidden.Add(Row(item));
                view.SetMessages(visible, hidden, host.Session?.PendingCount ?? 0);
                preview.Render(host.Snapshot);
                view.RefreshForms();
                audioPanel.Refresh();
                view.Workspace.FooterLeading.text = host.Session == null ? "Session restarting…"
                    : (host.Connection == null ? "Editor preview" : "Connected · lab messages only") + " · " + host.Session.PingCount + " ping requests";
                view.Workspace.FooterTrailing.text = "Maximum " + settings.maxVisible + " · " + settings.show + " / " + settings.hide + " · Lazy follow " + (settings.lazyFollow ? "on" : "off");
            }
            finally { refreshing = false; }
        }

        private void RefreshTargets()
        {
            targets.Clear();
            var ids = new List<string>();
            var labels = new List<string>();
            string selected = null;
            foreach (var target in NotificationEditorTargets.Capture())
            {
                if (!DeucarianNotificationLabWindow.IsRuntimeTarget(target)) continue;
                var component = (Component)target.View;
                if (!targetIds.TryGetValue(target, out string id))
                {
                    id = (++nextTargetId).ToString();
                    targetIds.Add(target, id);
                }
                targets.Add(id, target); ids.Add(id);
                labels.Add(component.gameObject.scene.name + " / " + component.name + " (" + component.GetType().Name + ")");
                if (ReferenceEquals(target, host.Connection?.Target)) selected = id;
            }
            foreach (var target in new List<NotificationEditorTarget>(targetIds.Keys))
                if (!targets.ContainsKey(targetIds[target])) targetIds.Remove(target);
            string note = host.Connection == null
                ? !EditorApplication.isPlaying ? "Sandbox · Start Play Mode to connect a running list" : "Sandbox · Does not affect your app"
                : "Connected · only this lab's test messages are shown below";
            view.SetTargets(ids, labels, selected, string.IsNullOrEmpty(host.RuntimeStatus) ? note : host.RuntimeStatus);
        }

        private void SelectTarget(string id)
        {
            if (id == null) host.SelectRuntimeTarget(null);
            else if (targets.TryGetValue(id, out var target) && DeucarianNotificationLabWindow.IsRuntimeTarget(target)) host.SelectRuntimeTarget(target);
            Refresh();
        }

        private DeucarianEditorMessageData Row(NotificationItem item)
        {
            bool timed = item.Definition.Lifetime.Kind == NotificationLifetimeKind.Timed;
            bool recovering = host.Session?.IsRecovering(item.Id) == true;
            double remaining = Math.Max(0, item.Definition.Lifetime.Seconds - (EditorApplication.timeSinceStartup - item.ActivatedAtSeconds));
            string state = recovering ? "Recovering…" : timed ? "Expires in " + remaining.ToString("0.0") + " s" : "";
            var status = item.Definition.Severity == NotificationSeverity.Error ? DeucarianEditorStatus.Error :
                item.Definition.Severity == NotificationSeverity.Warning ? DeucarianEditorStatus.Warning :
                item.Definition.Severity == NotificationSeverity.Success ? DeucarianEditorStatus.Success : DeucarianEditorStatus.Info;
            return new DeucarianEditorMessageData(item.Id.Value, item.Definition.Title, item.Definition.Body, status, state,
                timed ? (float?)(remaining / item.Definition.Lifetime.Seconds) : null,
                timed ? null : "Resolve", timed ? (Action)null : () => { host.Session?.Resolve(item.Id); Refresh(); }, !recovering);
        }

        public void Dispose() { if (disposed) return; disposed = true; definitions?.Dispose(); audioPanel?.Dispose(); preview.Dispose(); view.Dispose(); targets.Clear(); targetIds.Clear(); }
    }
}
