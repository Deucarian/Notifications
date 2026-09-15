// <deucarian-definition schema="notifications" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundOverflow0
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 5.0f,
            Id = "sample.overflow.0",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.Timed,
            Message = "Observe the visible limit and overflow count.",
            Name = "PlaygroundOverflow0",
            Priority = 0,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Info,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Queued message 1",
        };
        // end-definition-value
    }
}
