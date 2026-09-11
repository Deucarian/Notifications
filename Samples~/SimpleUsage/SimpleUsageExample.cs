using UnityEngine;

namespace Deucarian.Notifications.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public void ShowWarning() => NotificationManager.Warn("connection.lost", "Connection lost", "Please reconnect your device.");
        public void ResolveWarning() => NotificationManager.Resolve("connection.lost");
    }
}
