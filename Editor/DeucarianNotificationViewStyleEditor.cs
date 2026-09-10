using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomEditor(typeof(NotificationViewStyle))]
    public sealed class DeucarianNotificationViewStyleEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI() =>
            DeucarianEditorInspector.Create(OnInspectorGUI);

        public override void OnInspectorGUI()
        {
            DeucarianEditorChrome.DrawPackageHeader(
                "notifications",
                "Notification View Style",
                "Theming color roles and fallback severity accents for notification rows.");
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
