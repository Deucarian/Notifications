// <deucarian-definition schema="notifications" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_PlaygroundSaved
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = null,
            DurationSeconds = 3.0f,
            Id = "sample.playground.saved",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.Timed,
            Message = "This message closes automatically.",
            Name = "PlaygroundSaved",
            Priority = 10,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Success,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.SeverityDefault,
            Title = "Saved",
        };
        // end-definition-value
    }
}
