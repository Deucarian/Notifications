using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomEditor(typeof(NotificationViewStyle))]
    public sealed class DeucarianNotificationViewStyleEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DeucarianEditorChrome.DrawPackageHeader(
                "notifications",
                "Notification View Style",
                "Reusable severity accents for non-modal notification rows.");
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
