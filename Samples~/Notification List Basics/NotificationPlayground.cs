using UnityEngine;

namespace Deucarian.Notifications.Samples.Basic
{
    public sealed class NotificationPlayground : MonoBehaviour
    {
        [SerializeField] private NotificationKey warning = ExampleKeys.Connection;
        [SerializeField] private string title = "Connection lost";
        [SerializeField] [TextArea] private string message = "Reconnect the device, then press Resolve.";
        [SerializeField] [Min(0.1f)] private float timedSeconds = 3;

        public void ShowWarning() => NotificationManager.Warn(warning, title, message);
        public void ResolveWarning() => NotificationManager.Resolve(warning);
        public void ShowTimed() => NotificationManager.Show(new NotificationDefinition(ExampleKeys.Saved.Id,
            NotificationSeverity.Success, "Saved", "This message closes automatically.", 10,
            "deucarian.feedback.audio.success", NotificationLifetime.Timed(Mathf.Max(0.1f, timedSeconds))));
        public void ShowOverflow()
        {
            for (int i = 0; i < 8; i++) NotificationManager.Show(new NotificationDefinition("sample.overflow." + i,
                NotificationSeverity.Info, "Queued message " + (i + 1), "Observe the visible limit and overflow count.",
                i, "deucarian.feedback.audio.info", NotificationLifetime.Timed(5 + i)));
        }
    }

    [NotificationKeySet]
    public static class ExampleKeys
    {
        public static NotificationKey Connection => new Declared("sample.playground.connection");
        public static NotificationKey Saved => new Declared("sample.playground.saved");
        private sealed class Declared : NotificationKey { internal Declared(string id) : base(id) { } }
    }
}
