// <deucarian-definition schema="notifications" />
// Editable declaration. Use the package definition editor or edit the values below.
namespace Deucarian.ProjectDefinitions.Definition_notifications
{
    public static class Definition_NotificationSampleConnectionLost
    {
        public static global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec Value =>
        // definition-value
        new global::Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSpec
        {
            CustomSound = global::Deucarian.Editor.Definitions.DeucarianDefinitionAssets.Load<global::Deucarian.Theming.DeucarianAudioRole>("e712228afaa764540ab5d0c9be4884f3", 11400000L),
            DurationSeconds = 5f,
            Id = "d4c240db320644708ca0d9bf90b1b524",
            Lifetime = global::Deucarian.Notifications.NotificationLifetimeKind.UntilResolved,
            Message = "Please reconnect your device.",
            Name = "NotificationSampleConnectionLost",
            Priority = 0,
            Severity = global::Deucarian.Notifications.NotificationSeverity.Warning,
            Sound = global::Deucarian.Notifications.Unity.NotificationSoundPolicy.Custom,
            Title = "Connection lost",
        };
        // end-definition-value
    }
}
