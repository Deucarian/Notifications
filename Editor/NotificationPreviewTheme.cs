using System;
using Deucarian.Common;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Adapts Theming's editor selection to the real row's theme contract without editing source assets.</summary>
    internal sealed class NotificationPreviewTheme : IDisposable
    {
        private DeucarianTheme composed;
        private DeucarianTheme source;
        private DeucarianThemeStyle style;
        internal string Description { get; private set; }
        internal int Revision { get; private set; }

        internal DeucarianTheme Resolve(NotificationRowView row, Component runtimeView)
        {
            if (runtimeView != null)
            {
                var runtimeTheme = row != null && row.ThemeOverride != null ? row.ThemeOverride
                    : DeucarianThemeRuntimeResolver.ResolveTheme(row != null ? (Component)row : runtimeView);
                Description = "Running list theme · " + (runtimeTheme != null ? runtimeTheme.DisplayName : "Authored colors")
                    + ". Uses the connected list's current theme, including any active editor preview.";
                if (!DeucarianThemeRuntimeResolver.UseVisualStyling)
                    Description = "Running list · Visual styling is off. Showing the runtime prefab's authored colors.";
                return runtimeTheme;
            }

            var selection = DeucarianEditorThemePreview.Capture();
            Description = (selection.IsDraft ? "Visual palettes draft · " : "Project theme · ") + selection.Label
                + (selection.IsDraft ? ". Preview only; use Apply to project in Visual palettes to change the project default."
                    : ". Matches the project default.");
            if (!DeucarianThemeRuntimeResolver.UseVisualStyling)
                Description = "Visual styling is off · Showing the runtime prefab's authored colors. Enable it in Theming Project setup.";
            if (selection.Theme == null || selection.Style == selection.Theme.VisualStyle) return selection.Theme;
            if (composed == null)
            {
                composed = ScriptableObject.CreateInstance<DeucarianTheme>();
                composed.hideFlags = HideFlags.HideAndDontSave;
            }
            if (source != selection.Theme || style != selection.Style || composed.ColorPalette != selection.Theme.ColorPalette)
            {
                source = selection.Theme;
                style = selection.Style;
                EditorUtility.CopySerialized(source, composed);
                composed.hideFlags = HideFlags.HideAndDontSave;
                composed.SetVisualStyle(style);
                Revision++;
            }
            return composed;
        }

        public void Dispose()
        {
            UnityObjectUtility.DestroySafely(composed);
            composed = source = null;
            style = null;
        }
    }
}
