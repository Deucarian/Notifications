// <deucarian-definition schema="notifications" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundOverflow4
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 9.0f,
            Id = "sample.overflow.4",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.Timed,
            Message = "Observe the visible limit and overflow count.",
            Name = "PlaygroundOverflow4",
            Priority = 4,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Info,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Queued message 5",
        };
        // end-definition-value
    }
}
