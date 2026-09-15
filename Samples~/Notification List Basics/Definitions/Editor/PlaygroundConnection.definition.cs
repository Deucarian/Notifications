// <deucarian-definition schema="notifications" />
// Editable declaration. Use the Notification Lab or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundConnection
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 5.0f,
            Id = "sample.playground.connection",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.UntilResolved,
            Message = "Please reconnect",
            Name = "PlaygroundConnection",
            Priority = 100,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Warning,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Connection lost",
        };
        // end-definition-value
    }
}
