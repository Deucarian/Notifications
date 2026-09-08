using System;
using Deucarian.UI;
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
        [Tooltip("Let a world-space warning list settle behind its moving parent anchor. Screen-space lists stay fixed.")]
        public bool lazyFollow;
        public DeucarianLazyFollowSettings follow;

        public static NotificationPresentationSettings Default => new NotificationPresentationSettings
        {
            maxVisible = 5, show = NotificationTransition.Fade, hide = NotificationTransition.Fade,
            showSeconds = 0.18f, hideSeconds = 0.14f, lazyFollow = false,
            follow = DeucarianLazyFollowSettings.Default
        };

        public NotificationPresentationSettings Sanitized()
        {
            var result = this;
            if (result.maxVisible == 0) return Default;
            result.maxVisible = Mathf.Clamp(result.maxVisible, 1, 20);
            result.showSeconds = FiniteSeconds(result.showSeconds);
            result.hideSeconds = FiniteSeconds(result.hideSeconds);
            result.follow = result.follow.Sanitized();
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
