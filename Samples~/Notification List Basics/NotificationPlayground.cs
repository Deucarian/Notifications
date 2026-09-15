using UnityEngine;

namespace Deucarian.Notifications.Samples.Basic
{
    public sealed class NotificationPlayground : MonoBehaviour
    {
        [SerializeField] private NotificationKey warning = ExampleKeys.Connection;
        [SerializeField] private string title = "Connection lost";
        [SerializeField] [TextArea] private string message = "Reconnect the device, then press Resolve.";

        public void ShowWarning() => NotificationManager.Warn(warning, title, message);
        public void ResolveWarning() => NotificationManager.Resolve(warning);
        public void ShowTimed() => NotificationManager.Show(ExampleKeys.Saved);
        public void ShowOverflow()
        {
            foreach (var key in ExampleKeys.Overflow) NotificationManager.Show(key);
        }
    }

    [NotificationKeySet]
    public static class ExampleKeys
    {
        public static NotificationKey Connection => new Declared("sample.playground.connection");
        public static NotificationKey Saved => new Declared("sample.playground.saved");
        public static readonly NotificationKey[] Overflow = { new Declared("sample.overflow.0"), new Declared("sample.overflow.1"), new Declared("sample.overflow.2"), new Declared("sample.overflow.3"), new Declared("sample.overflow.4"), new Declared("sample.overflow.5"), new Declared("sample.overflow.6"), new Declared("sample.overflow.7") };
        private sealed class Declared : NotificationKey { internal Declared(string id) : base(id) { } }
    }
}
