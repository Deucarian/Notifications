// <deucarian-definition schema="notifications" />
// Editable declaration. Use the Notification Lab or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundOverflow3
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 8.0f,
            Id = "sample.overflow.3",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.Timed,
            Message = "Observe the visible limit and overflow count.",
            Name = "PlaygroundOverflow3",
            Priority = 3,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Info,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Queued message 4",
        };
        // end-definition-value
    }
}
