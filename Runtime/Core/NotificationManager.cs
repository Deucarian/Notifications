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
            throw new InvalidOperationException("Configure a NotificationHost before using NotificationManager.");

        /// <summary>Registers a borrowed service. Disposing the registration never disposes the service.</summary>
        public static IDisposable Bind(NotificationService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (current != null) throw new InvalidOperationException("A default notification service is already registered.");
            return current = new Registration(service);
        }

        public static void Warn(string id, string title, string message) => Service.Warn(id, title, message);
        public static void Show(NotificationDefinition definition) => Service.Show(definition);
        public static void Resolve(string id) => Service.Resolve(id);

        private sealed class Registration : IDisposable
        {
            public Registration(NotificationService service) { Service = service; }
            public NotificationService Service { get; }
            public void Dispose() { if (ReferenceEquals(current, this)) current = null; }
        }
    }
}
