using System;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Inspector/event access to the same default service used by ordinary C# callers.</summary>
    [AddComponentMenu("Deucarian/Notifications/Notification Trigger")]
    public sealed class NotificationTrigger : MonoBehaviour
    {
        [SerializeField] private NotificationKey notification;
        [SerializeField] private bool overrideTitle;
        [SerializeField] private string title;
        [SerializeField] private bool overrideMessage;
        [SerializeField, TextArea] private string message;
        [SerializeField] private bool overrideSound;
        [SerializeField] private AudioRoleKey sound = AudioRoles.Feedback.Warning;

        public NotificationKey Notification { get => notification; set => notification = value; }
        public void Show()
        {
            if (notification == null) throw new InvalidOperationException("NotificationTrigger '" + name + "' needs a notification. Select an existing definition in its Inspector dropdown.");
            NotificationManager.Show(notification, new NotificationContentOverrides(overrideTitle ? title : null, overrideMessage ? message : null,
                overrideSound ? sound?.Id ?? string.Empty : null));
        }
        public void Resolve()
        {
            if (notification == null) throw new InvalidOperationException("Select the notification to resolve on NotificationTrigger '" + name + "'.");
            NotificationManager.Resolve(notification);
        }
    }
}
