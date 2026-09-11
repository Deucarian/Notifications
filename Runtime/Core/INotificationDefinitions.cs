namespace Deucarian.Notifications
{
    /// <summary>Read-only reusable definitions supplied once by the application's composition root.</summary>
    public interface INotificationDefinitions
    {
        bool TryGet(NotificationKey key, out NotificationDefinition definition);
    }
}
