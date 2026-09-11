using System;
using Deucarian.UI;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    public enum NotificationTransition
    {
        None = 0, Fade = 1, Scale = 2, Slide = 3,
        FadeAndScale = 4, FadeAndSlide = 5, ScaleAndSlide = 6, FadeScaleAndSlide = 7
    }

    [Serializable]
    public struct NotificationPresentationSettings
    {
        [Range(1, 20)] public int maxVisible;
        public NotificationTransition show;
        public NotificationTransition hide;
        [Range(0, 2)] public float showSeconds;
        [Range(0, 2)] public float hideSeconds;
        [Tooltip("Snap layout changes instead of smoothly moving the existing messages. Independent of Enter and Exit.")]
        public bool instantLayout;
        [Range(.05f, 1)] public float reflowSeconds;
        public float ReflowDuration => instantLayout ? 0 : ValidReflowSeconds(reflowSeconds);
        [Tooltip("Let a world-space or camera-space warning list settle behind its moving parent anchor. Overlay lists stay fixed.")]
        public bool lazyFollow;
        public DeucarianLazyFollowSettings follow;

        public static NotificationPresentationSettings Default => new NotificationPresentationSettings
        {
            maxVisible = 5, show = NotificationTransition.Fade, hide = NotificationTransition.Fade,
            showSeconds = 0.18f, hideSeconds = 0.14f, lazyFollow = false,
            reflowSeconds = DeucarianLayoutTransition.DefaultDurationSeconds,
            follow = DeucarianLazyFollowSettings.Default
        };

        public NotificationPresentationSettings Sanitized()
        {
            var result = this;
            if (result.maxVisible == 0) return Default;
            result.maxVisible = Mathf.Clamp(result.maxVisible, 1, 20);
            result.show = SanitizeTransition(result.show);
            result.hide = SanitizeTransition(result.hide);
            result.showSeconds = FiniteSeconds(result.showSeconds);
            result.hideSeconds = FiniteSeconds(result.hideSeconds);
            result.reflowSeconds = ValidReflowSeconds(result.reflowSeconds);
            result.follow = result.follow.Sanitized();
            return result;
        }

        private static float FiniteSeconds(float value) => float.IsNaN(value) || float.IsInfinity(value)
            ? 0 : Mathf.Clamp(value, 0, 2);

        private static float ValidReflowSeconds(float value) => value <= 0 || float.IsNaN(value) || float.IsInfinity(value)
            ? DeucarianLayoutTransition.DefaultDurationSeconds : Mathf.Clamp(value, .05f, 1);

        private static NotificationTransition SanitizeTransition(NotificationTransition value) =>
            value >= NotificationTransition.None && value <= NotificationTransition.FadeScaleAndSlide
                ? value : NotificationTransition.None;
    }

    public interface INotificationPresentationTarget
    {
        NotificationPresentationSettings Presentation { get; }
        void ConfigurePresentation(NotificationPresentationSettings settings);
    }
}
