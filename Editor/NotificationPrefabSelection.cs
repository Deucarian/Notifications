using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Persists project presentation and prefab choices; the package owns the default assets.</summary>
    internal static class NotificationPrefabSelection
    {
        internal const string SettingsPath = "Assets/DeucarianSettings/Resources/Deucarian/Notifications/NotificationViewSettings.asset";

        internal static void Bind(DeucarianEditorWorkspaceForm form, System.Func<Component> runtimeView)
        {
            form.AssetWithActions("lab-notification-prefab", "Notification prefab", typeof(NotificationRowView),
                () => NotificationViewDefaults.ResolveRowPrefab(),
                value => SetPrefab(value as NotificationRowView, runtimeView()));
            form.Action("lab-default-notification", "Change notification to default", () => SetPrefab(null, runtimeView()));
            form.Note(() => NotificationViewSettings.Load()?.UsesDefault != false
                ? "Using the package default. Its prefab follows package updates; colours and text follow Theming."
                : "Using a custom notification prefab. Choose default to restore the package design.");
        }

        internal static void SetPrefab(NotificationRowView prefab, Component runtimeView = null)
        {
            if (prefab == NotificationViewDefaults.LoadRowPrefab()) prefab = null;
            if (prefab != null && !PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new System.ArgumentException("Choose a notification row prefab asset.", nameof(prefab));
            var settings = NotificationViewSettings.Load();
            if (settings == null && prefab != null) settings = CreateSettings();
            if (settings != null)
            {
                Undo.RecordObject(settings, "Change notification prefab");
                if (prefab == null) settings.ChangeToDefault(); else settings.UseCustomPrefab(prefab);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            var list = runtimeView as NotificationListView;
            if (list == null && runtimeView != null) list = runtimeView.GetComponentInChildren<NotificationListView>(true);
            if (list != null)
            {
                Undo.RecordObject(list, "Change notification prefab");
                list.UseProjectRowPrefab(settings);
            }
        }

        internal static void SavePresentation(NotificationPresentationSettings presentation)
        {
            var settings = NotificationViewSettings.Load();
            if (settings == null) settings = CreateSettings();
            Undo.RecordObject(settings, "Save notification appearance");
            settings.SetPresentation(presentation);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            // Existing views must also discover a settings asset created for the first time in Play mode.
            foreach (var list in Resources.FindObjectsOfTypeAll<NotificationListView>())
                if (list.gameObject.scene.IsValid() && list.UsesProjectRowPrefab)
                    list.UseProjectRowPrefab(settings);
        }

        private static NotificationViewSettings CreateSettings()
        {
            var parts = SettingsPath.Split('/');
            string folder = parts[0];
            for (int i = 1; i < parts.Length - 1; i++)
            {
                string next = folder + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(folder, parts[i]);
                folder = next;
            }
            var settings = ScriptableObject.CreateInstance<NotificationViewSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            Undo.RegisterCreatedObjectUndo(settings, "Create notification view settings");
            return settings;
        }
    }
}
