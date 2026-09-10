using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomEditor(typeof(NotificationListView))]
    public sealed class DeucarianNotificationListViewEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            DeucarianEditorChrome.DrawPackageHeader("notifications", "Notification List",
                "Bounded messages, reversible visibility motion and Deucarian visual theming.");
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            var view = (NotificationListView)target;
            if (changed && EditorApplication.isPlaying) view.ConfigurePresentation(view.Presentation);
            DeucarianEditorTextGUI.HelpBox("Overflow stays active; the limit never resolves messages. None disables motion. " +
                "Rows use the nearest Deucarian theme provider or project theme, with fallback colors when unavailable.", MessageType.Info);
        }
    }
}
