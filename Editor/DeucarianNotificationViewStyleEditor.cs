using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomEditor(typeof(NotificationViewStyle))]
    public sealed class DeucarianNotificationViewStyleEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Notification style");
            DeucarianEditorInspector.Properties(root, serializedObject);
            return root;
        }
    }
}
