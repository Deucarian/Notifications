using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Owns only the editor row animations; notification lifetime remains owned by the lab session.</summary>
    internal sealed class NotificationLabRowPreview : IDisposable
    {
        private readonly DeucarianEditorLabWorkspace view;
        private readonly Dictionary<DeucarianEditorMessageRow, Motion> motions = new Dictionary<DeucarianEditorMessageRow, Motion>();
        private NotificationPresentationSettings settings;
        private DeucarianTheme theme;
        private NotificationViewStyle style;
        private NotificationRowView template;
        private bool disposed;

        internal NotificationLabRowPreview(DeucarianEditorLabWorkspace view)
        {
            this.view = view;
            view.PresentRow = Present;
            view.PlaceRow = Place;
            view.DismissRow = Dismiss;
            EditorApplication.update += Tick;
        }

        internal void Configure(NotificationPresentationSettings settings, Component runtimeView)
        {
            this.settings = settings.Sanitized();
            theme = null; style = null;
            template = runtimeView is NotificationListView list ? list.RowTemplate :
                runtimeView != null ? runtimeView.GetComponentInChildren<NotificationRowView>(true) : null;
            style = template != null ? template.ViewStyle : null;
            if (DeucarianThemeRuntimeResolver.UseVisualStyling)
            {
                var provider = runtimeView != null ? DeucarianThemeRuntimeResolver.FindProvider(runtimeView) : null;
                theme = template != null ? template.ThemeOverride : null;
                if (theme == null) theme = provider != null ? provider.CurrentTheme : null;
                if (theme == null) theme = DeucarianThemeRuntimeResolver.LoadSettings()?.ResolvedDefaultTheme;
            }
            foreach (var motion in motions.Values) motion.Transition.Configure(this.settings);
            view.PreviewRoot.tooltip = theme != null ? "Project theme: " + theme.DisplayName : "Visual styling is off or no theme is assigned. Authored notification defaults are previewed.";
        }

        private void Present(DeucarianEditorMessageRow row, DeucarianEditorMessageData data)
        {
            NotificationSeverity severity = data.Status == DeucarianEditorStatus.Error ? NotificationSeverity.Error :
                data.Status == DeucarianEditorStatus.Warning ? NotificationSeverity.Warning :
                data.Status == DeucarianEditorStatus.Success ? NotificationSeverity.Success : NotificationSeverity.Info;
            var fallback = template != null ? template.AuthoredAppearance(severity) : NotificationRowAppearance.Default(severity);
            var colors = NotificationRowAppearance.Resolve(theme, style, severity, fallback);
            row.SetColors(colors.Surface, colors.Title, colors.Body, colors.Severity);
            ApplyTypography(row.Title, theme?.VisualStyle?.TypographyProfile, DeucarianThemeTextRole.Title);
            ApplyTypography(row.Body, theme?.VisualStyle?.TypographyProfile, DeucarianThemeTextRole.Body);
        }

        private void Place(DeucarianEditorMessageRow row, VisualElement destination, int index)
        {
            if (!motions.TryGetValue(row, out var motion))
            { motion = new Motion(row); motions.Add(row, motion); }
            motion.Destination = destination; motion.Index = index;
            bool wantsVisible = destination == view.VisibleRows;
            if (row.parent == view.VisibleRows && motion.Started && !wantsVisible)
            {
                if (!motion.Exiting) BeginExit(motion);
                return;
            }
            if (wantsVisible && motion.Exiting && motion.Complete == null)
            {
                motion.Exiting = false;
                motion.Transition.SetVisible(true, settings);
            }
            Insert(motion);
            if (!wantsVisible)
            {
                motion.Started = false;
                motion.Row.style.display = DisplayStyle.Flex;
                motion.Row.style.opacity = 1;
                motion.Row.transform.scale = Vector3.one;
                motion.Row.transform.position = Vector3.zero;
            }
            else if (!motion.Started)
            { motion.Row.style.display = DisplayStyle.None; motion.Row.style.opacity = 0; }
        }

        private static void Insert(Motion motion)
        {
            var destination = motion.Destination;
            int index = Math.Min(motion.Index, destination.childCount);
            if (motion.Row.parent != destination || destination.IndexOf(motion.Row) != index)
                destination.Insert(index, motion.Row);
        }

        private static void ApplyTypography(Label label, DeucarianThemeTypographyProfile typography, DeucarianThemeTextRole role)
        {
            var font = typography?.ResolvedFontAsset?.sourceFontFile;
            label.style.unityFont = font != null ? new StyleFont(font) : new StyleFont(StyleKeyword.Null);
            if (typography == null)
            {
                label.style.fontSize = StyleKeyword.Null;
                label.style.unityFontStyleAndWeight = StyleKeyword.Null;
                return;
            }
            var text = typography.GetStyle(role);
            label.style.fontSize = text.FontSize;
            bool bold = (text.FontStyle & TMPro.FontStyles.Bold) != 0;
            bool italic = (text.FontStyle & TMPro.FontStyles.Italic) != 0;
            label.style.unityFontStyleAndWeight = bold ? (italic ? FontStyle.BoldAndItalic : FontStyle.Bold) : italic ? FontStyle.Italic : FontStyle.Normal;
        }

        private void Dismiss(DeucarianEditorMessageRow row, Action complete)
        {
            if (!motions.TryGetValue(row, out var motion) || !motion.Started || row.parent != view.VisibleRows)
            { motions.Remove(row); complete(); return; }
            motion.Complete = complete;
            if (!motion.Exiting) BeginExit(motion);
            if (motion.Transition.IsHidden) FinishExit(motion);
            row.SetEnabled(false);
        }

        private void BeginExit(Motion motion)
        {
            motion.Exiting = true;
            motion.Transition.SetVisible(false, settings);
            motion.Apply();
        }

        private void FinishExit(Motion motion)
        {
            if (motion.Complete != null)
            { motions.Remove(motion.Row); motion.Complete(); return; }
            motion.Started = false; motion.Exiting = false;
            Insert(motion);
            motion.Row.style.display = DisplayStyle.Flex;
            motion.Row.style.opacity = 1;
            motion.Row.transform.scale = Vector3.one;
            motion.Row.transform.position = Vector3.zero;
        }

        internal void Replay()
        {
            foreach (var motion in motions.Values)
                if (!motion.Exiting && motion.Row.parent == view.VisibleRows)
                { motion.Started = false; motion.Transition = new NotificationRowTransition(); motion.Row.style.opacity = 0; }
        }

        private void Tick()
        {
            if (disposed) return;
            Advance(EditorApplication.timeSinceStartup);
        }

        internal void Advance(double now)
        {
            if (disposed) return;
            int occupied = 0;
            foreach (var motion in motions.Values)
                if (motion.Started && motion.Row.parent == view.VisibleRows) occupied++;
            foreach (var pair in new List<KeyValuePair<DeucarianEditorMessageRow, Motion>>(motions))
            {
                var motion = pair.Value;
                if (motion.Row.parent == null) { motions.Remove(pair.Key); motion.Complete?.Invoke(); continue; }
                if (motion.Row.parent != view.VisibleRows)
                    continue;
                if (!motion.Started)
                {
                    if (occupied >= settings.maxVisible) continue;
                    motion.Started = true; motion.LastTime = now; occupied++;
                    motion.Row.style.display = DisplayStyle.Flex;
                    motion.Transition.SetVisible(true, settings);
                }
                motion.Transition.Advance(Mathf.Max(0, (float)(now - motion.LastTime)));
                motion.LastTime = now;
                motion.Apply();
                if (motion.Exiting && motion.Transition.IsHidden)
                { FinishExit(motion); occupied--; }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; EditorApplication.update -= Tick;
            view.PresentRow = null; view.PlaceRow = null; view.DismissRow = null;
            foreach (var motion in motions.Values) motion.Complete?.Invoke();
            motions.Clear();
        }

        private sealed class Motion
        {
            internal readonly DeucarianEditorMessageRow Row;
            internal bool Started, Exiting;
            internal double LastTime;
            internal NotificationRowTransition Transition = new NotificationRowTransition();
            internal VisualElement Destination;
            internal int Index;
            internal Action Complete;
            internal Motion(DeucarianEditorMessageRow row) => Row = row;
            internal void Apply()
            {
                Row.style.opacity = Transition.Alpha;
                Row.transform.scale = Vector3.one * Transition.Scale;
                Row.transform.position = Transition.Offset;
            }
        }
    }
}
