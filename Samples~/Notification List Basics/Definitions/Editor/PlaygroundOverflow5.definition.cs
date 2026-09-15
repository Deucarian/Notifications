// <deucarian-definition schema="notifications" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundOverflow5
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 10.0f,
            Id = "sample.overflow.5",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.Timed,
            Message = "Observe the visible limit and overflow count.",
            Name = "PlaygroundOverflow5",
            Priority = 5,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Info,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Queued message 6",
        };
        // end-definition-value
    }
}
