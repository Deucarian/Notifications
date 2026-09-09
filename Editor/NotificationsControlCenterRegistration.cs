using System;
using System.Collections.Generic;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [InitializeOnLoad]
    internal static class NotificationsControlCenterRegistration
    {
        internal const string ToolId = "deucarian.notifications.lab";
        private const string PackageId = "com.deucarian.notifications";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;

        static NotificationsControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(new DeucarianToolDescriptor(
                ToolId, "Notification Lab", "Add test warnings to your running app or preview messages without hardware.",
                DeucarianControlCenterArea.Experience, DeucarianNotificationLabWindow.OpenWindow, PackageId,
                searchTerms: new[] { "notification", "warning", "message", "test", "preview", "audio", "runtime", "play mode" }, order: 140, createPage: DeucarianNotificationLabWindow.CreatePage));
            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(new CardProvider());
        }

        private sealed class CardProvider : IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";
            public IEnumerable<DeucarianControlCenterCard> Capture(DeucarianControlCenterContext context)
            {
                return new[]
                {
                    new DeucarianControlCenterCard(PackageId + ".lab", DeucarianControlCenterArea.Experience,
                        "Notifications", "Test messages in the editor or inject them into a running application's warning list.",
                        PackageId, DeucarianControlCenterStatus.Success, "Ready to test", order: 140,
                        actions: new[] { new DeucarianControlCenterAction(ToolId, "Open Notification Lab",
                            DeucarianNotificationLabWindow.OpenWindow) },
                        searchTerms: new[] { "notification", "warning", "test", "preview" })
                };
            }
        }
    }
}
