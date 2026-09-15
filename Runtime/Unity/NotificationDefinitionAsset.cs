using System;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    public enum NotificationSoundPolicy { SeverityDefault, Custom, Silent }

    /// <summary>Reusable immutable-at-runtime content; active notification state belongs to the service.</summary>
    public sealed class NotificationDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private NotificationSeverity severity = NotificationSeverity.Warning;
        [SerializeField] private string title = "Connection lost";
        [SerializeField, TextArea] private string message = "Please reconnect your device.";
        [SerializeField] private int priority;
        [SerializeField] private NotificationLifetimeKind lifetime;
        [SerializeField, Min(0.01f)] private float durationSeconds = 5;
        [SerializeField] private NotificationSoundPolicy sound = NotificationSoundPolicy.SeverityDefault;
        [SerializeField] private DeucarianAudioRole customSound;

        public string Id => id;
        public string DisplayName => displayName;
        public NotificationKey Key => new AssetKey(id);
        public NotificationDefinition CreateDefinition()
        {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Notification definition '" + name + "' needs an ID. Repair it in the Notification Lab.");
            string feedback = string.Empty;
            if (sound == NotificationSoundPolicy.Custom)
                feedback = customSound != null ? customSound.Id : throw new InvalidOperationException("Select a custom audio role for notification '" + displayName + "', or choose Severity Default.");
            else if (sound == NotificationSoundPolicy.SeverityDefault)
                switch (severity)
                {
                    case NotificationSeverity.Info: feedback = AudioRoles.Feedback.Info.Id; break;
                    case NotificationSeverity.Success: feedback = AudioRoles.Feedback.Success.Id; break;
                    case NotificationSeverity.Warning: feedback = AudioRoles.Feedback.Warning.Id; break;
                    case NotificationSeverity.Error: feedback = AudioRoles.Feedback.Error.Id; break;
                }
            return new NotificationDefinition(id, severity, title, message, priority, feedback,
                lifetime == NotificationLifetimeKind.Timed ? NotificationLifetime.Timed(durationSeconds) : default);
        }
        private sealed class AssetKey : NotificationKey { internal AssetKey(string value) : base(value) { } }
    }
}
