using System;
using Deucarian.Editor;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ui = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.Notifications.Editor
{
    internal sealed class NotificationLabAudioPanel : IDisposable
    {
        private readonly DeucarianNotificationLabWindow host;
        private readonly DeucarianEditorWorkspaceForm form;
        private readonly DeucarianThemingEditorFeatureGate gate;
        private readonly VisualElement cueRoot;
        private readonly Button preview;
        private readonly Button save;
        private DeucarianAudioCueForm cue;
        private NotificationSeverity severity = NotificationSeverity.Warning;
        private string boundRole;
        private bool disposed;

        internal NotificationLabAudioPanel(VisualElement root, DeucarianNotificationLabWindow host)
        {
            this.host = host;
            var content = new VisualElement();
            var panel = Ui.IconPanel("lab-audio-card", DeucarianEditorIconIds.Notifications, content);
            form = new DeucarianEditorWorkspaceForm(content);
            form.AssetWithActions("lab-palette", "Palette set", typeof(DeucarianAudioPaletteSet), () => host.Palette,
                value => { host.Palette = (DeucarianAudioPaletteSet)value; Refresh(); },
                DeucarianThemeAssetCustomization.CreateAudio, DeucarianThemeAssetCustomization.Customize,
                () => DeucarianThemeRuntimeResolver.LoadSettings()?.DefaultAudioPaletteSet ?? DeucarianAudioDefaults.LoadPaletteSet());
            form.Choice("lab-feedback-role", "Role", Enum.GetNames(typeof(NotificationSeverity)), () => (int)severity,
                value => { severity = (NotificationSeverity)value; Refresh(); });
            cueRoot = new VisualElement(); content.Add(cueRoot);
            content.Add(Ui.Divider());
            form.Note(() => host.Connection == null ? "One ping for messages arriving together."
                : "Connected to your app · its palette controls playback.");
            preview = Ui.IconButton("Preview ping", DeucarianEditorIconIds.Play, () => host.PreviewPing(severity), DeucarianEditorButtonRole.Primary);
            preview.name = "lab-preview-ping";
            var open = Ui.IconButton("Open audio palettes", DeucarianEditorIconIds.ChevronRight, host.OpenAudioLab);
            open.name = "lab-open-audio";
            content.Add(Ui.Actions(preview, open));
            var advanced = form.Section("More options", true);
            advanced.Choice("lab-experience", "Experience", Enum.GetNames(typeof(DeucarianAudioExperience)),
                () => (int)host.Inputs.experience, value => Change(x => x.experience = (DeucarianAudioExperience)value));
            advanced.Toggle("lab-sound", "Ping new messages", () => host.Inputs.sound, value => Change(x => x.sound = value));
            advanced.Action("lab-selected-palette", "Use selected palette", () => { host.AdoptPaletteSelection(); Refresh(); });
            save = advanced.Action("lab-save-cue", "Save sound changes", () =>
            {
                var source = Resolve().SourcePalette;
                if (CanSave(source)) AssetDatabase.SaveAssetIfDirty(source);
            });
            advanced.Action("lab-stop-audio", "Stop sound", host.StopAudio);
            advanced.Note(() => host.AudioStatus);
            form.EnabledWhen(() => host.Session != null && host.Connection == null);
            gate = new DeucarianThemingEditorFeatureGate(panel, true, host.StopAudio);
            root.Add(gate.Root);
            Refresh();
        }

        private string RoleId => NotificationLabSession.FeedbackRole(severity);
        private DeucarianAudioResolution Resolve() => host.Palette != null && host.Palette.TryResolveById(RoleId, host.Inputs.experience, out var value)
            ? value : DeucarianAudioResolution.Missing;
        private bool CanSave(DeucarianAudioPalette palette) => palette != null && EditorUtility.IsDirty(palette) &&
            AssetDatabase.GetAssetPath(palette).Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal) &&
            host.Connection == null && DeucarianThemeRuntimeResolver.UseAudio;
        private void Change(Action<NotificationLabRecipeData> update)
        {
            var inputs = host.Inputs; update(inputs); host.Inputs = inputs; Refresh();
        }

        internal void Refresh()
        {
            if (disposed) return;
            if (cue == null || boundRole != RoleId || !cue.MatchesSource)
            {
                cue?.Dispose(); cueRoot.Clear(); boundRole = RoleId;
                cue = new DeucarianAudioCueForm(cueRoot, RoleId, Resolve, Refresh, false, "Sound");
            }
            cue.Refresh();
            form.Refresh();
            preview.SetEnabled(host.AudioAvailable && host.Connection == null && Resolve().IsAudible);
            save.SetEnabled(CanSave(Resolve().SourcePalette));
            gate.Refresh();
        }

        public void Dispose() { if (disposed) return; disposed = true; cue?.Dispose(); gate.Dispose(); }
    }
}
