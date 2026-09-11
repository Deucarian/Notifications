namespace Deucarian.Notifications.Samples.SimpleUsage
{
    // Declare each application notification once. Every caller reuses the same typed key.
    [NotificationKeySet]
    public static class NotificationKeys
    {
        public static NotificationKey ConnectionLost => new ConnectionLostKey();

        private sealed class ConnectionLostKey : NotificationKey
        {
            public ConnectionLostKey() : base("connection.lost") { }
        }
    }
}
