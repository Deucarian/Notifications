using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomEditor(typeof(NotificationListView))]
    public sealed class DeucarianNotificationListViewEditor : UnityEditor.Editor
    {
        public override UnityEngine.UIElements.VisualElement CreateInspectorGUI()
        {
            var root = DeucarianEditorInspector.CreateToolkit("Notification list");
            var fields = DeucarianEditorInspector.Properties(root, serializedObject);
            fields.RegisterCallback<UnityEditor.UIElements.SerializedPropertyChangeEvent>(_ =>
            {
                if (target is NotificationListView view && EditorApplication.isPlaying)
                    view.ConfigurePresentation(view.Presentation);
            });
            root.Add(DeucarianEditorWorkspaceControls.Label(
                "Overflow stays active until a visible slot is available.", "dw-muted"));
            return root;
        }
    }
}
