using System;

namespace Deucarian.Notifications
{
    /// <summary>Main-thread convenience access to the explicitly configured notification scope.</summary>
    public static class NotificationManager
    {
        private static Registration current;
        public static bool IsConfigured => current != null;
        public static NotificationSnapshot Snapshot => Service.Snapshot;
        private static NotificationService Service => current?.Service ??
            throw new InvalidOperationException("NotificationManager has no configured host. Add an enabled NotificationHost on the notification list prefab in your startup scene, or bind an explicitly owned NotificationService during composition.");

        /// <summary>Registers a borrowed service. Disposing the registration never disposes the service.</summary>
        public static IDisposable Bind(NotificationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (current != null) throw new InvalidOperationException("A default notification service is already registered.");
            return current = new Registration(service);
        }

        public static void Warn(NotificationKey key, string title, string message) => Service.Warn(key, title, message);
        public static void Show(NotificationDefinition definition) => Service.Show(definition);
        public static void Resolve(NotificationKey key) => Service.Resolve(key);

        private sealed class Registration : IDisposable
        {
            public Registration(NotificationService service) { Service = service; }
            public NotificationService Service { get; }
            public void Dispose() { if (ReferenceEquals(current, this)) current = null; }
        }
    }
}
