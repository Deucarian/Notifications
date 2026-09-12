using Deucarian.Editor;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.Editor
{
    public static class NotificationDefinitionPreview
    {
        public static void Open(NotificationDefinitionAsset asset)
        {
            DeucarianNotificationLabWindow.OpenWindow();
            var window = DeucarianEditorWindowPages.GetStandalone<DeucarianNotificationLabWindow>("Notification Lab");
            window.PreviewSavedDefinition(asset);
        }
    }
    public sealed partial class DeucarianNotificationLabWindow
    {
        internal void PreviewSavedDefinition(NotificationDefinitionAsset asset)
        {
            SelectRuntimeTarget(null);
            session.Show(asset.CreateDefinition(), default);
            workspace?.Refresh();
        }
    }
}
