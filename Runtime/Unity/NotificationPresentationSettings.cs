using System;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    public enum NotificationTransition { None, Fade, Scale, Slide }

    [Serializable]
    public struct NotificationPresentationSettings
    {
        [Range(1, 20)] public int maxVisible;
        public NotificationTransition show;
        public NotificationTransition hide;
        [Range(0, 2)] public float showSeconds;
        [Range(0, 2)] public float hideSeconds;

        public static NotificationPresentationSettings Default => new NotificationPresentationSettings
        {
            maxVisible = 5, show = NotificationTransition.Fade, hide = NotificationTransition.Fade,
            showSeconds = 0.18f, hideSeconds = 0.14f
        };

        public NotificationPresentationSettings Sanitized()
        {
            var result = this;
            if (result.maxVisible == 0) return Default;
            result.maxVisible = Mathf.Clamp(result.maxVisible, 1, 20);
            result.showSeconds = FiniteSeconds(result.showSeconds);
            result.hideSeconds = FiniteSeconds(result.hideSeconds);
            return result;
        }

        private static float FiniteSeconds(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0 : Mathf.Clamp(value, 0, 2);
    }

    public interface INotificationPresentationTarget
    {
        NotificationPresentationSettings Presentation { get; }
        void ConfigurePresentation(NotificationPresentationSettings settings);
    }
}
