using UnityEngine;

namespace Deucarian.Notifications.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private NotificationKey warning = NotificationKeys.ConnectionLost;

        public void ShowWarning() => NotificationManager.Warn(warning, "Connection lost", "Please reconnect your device.");
        public void ResolveWarning() => NotificationManager.Resolve(warning);
    }
}
