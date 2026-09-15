using System;

namespace Deucarian.Notifications
{
    /// <summary>Resolves declared content before any lifecycle or feedback state changes.</summary>
    public static class NotificationDefinitions
    {
        public static NotificationDefinition Require(INotificationDefinitions catalog, NotificationKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (catalog == null || !catalog.TryGet(key, out var definition) || definition == null)
                throw new InvalidOperationException("Notification '" + key.Id +
                    "' is not registered. Create or register its definition in the Notification Lab and assign the catalog to the notification host/store.");
            if (definition.Id.Value != key.Id)
                throw new InvalidOperationException("The notification catalog returned a different identity for '" + key.Id + "'.");
            return definition;
        }

        internal static NotificationDefinition Require(INotificationDefinitions catalog, NotificationId id) =>
            Require(catalog, new LookupKey(id.Value));

        private sealed class LookupKey : NotificationKey
        {
            internal LookupKey(string id) : base(id) { }
        }
    }
}
