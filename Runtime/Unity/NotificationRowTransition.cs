using Deucarian.UI;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Renderer-independent notification motion shared by runtime rows and editor previews.</summary>
    public sealed class NotificationRowTransition
    {
        private readonly DeucarianVisibilityTransition transition =
            new DeucarianVisibilityTransition(DeucarianMotionProfile.UiPanel);
        private NotificationPresentationSettings settings;
        public bool IsShowing { get; private set; }
        public bool IsHidden => transition.Phase == DeucarianVisibilityPhase.Hidden;
        public float Progress => transition.Progress;
        public float Alpha => Fades ? Progress : Progress > 0 ? 1 : 0;
        public float Scale => Scales ? Mathf.Lerp(.85f, 1, Progress) : 1;
        public Vector2 Offset => Slides ? new Vector2(-60 * (1 - Progress), 0) : Vector2.zero;
        private bool Fades => Mode == NotificationTransition.Fade || Mode == NotificationTransition.FadeAndScale ||
            Mode == NotificationTransition.FadeAndSlide || Mode == NotificationTransition.FadeScaleAndSlide;
        private bool Scales => Mode == NotificationTransition.Scale || Mode == NotificationTransition.FadeAndScale ||
            Mode == NotificationTransition.ScaleAndSlide || Mode == NotificationTransition.FadeScaleAndSlide;
        private bool Slides => Mode == NotificationTransition.Slide || Mode == NotificationTransition.FadeAndSlide ||
            Mode == NotificationTransition.ScaleAndSlide || Mode == NotificationTransition.FadeScaleAndSlide;
        private NotificationTransition Mode => IsShowing ? settings.show : settings.hide;
        private float Duration => IsShowing ? settings.showSeconds : settings.hideSeconds;

        public void Configure(NotificationPresentationSettings value) => settings = value.Sanitized();
        public void SetVisible(bool visible, NotificationPresentationSettings value, bool animate = true)
        {
            Configure(value);
            IsShowing = visible;
            if (visible) transition.Show(); else transition.Hide();
            if (!animate || Mode == NotificationTransition.None || Duration <= 0) transition.Complete();
        }
        public void Advance(float seconds)
        {
            if (Mode == NotificationTransition.None || Duration <= 0) transition.Complete();
            else transition.Advance(seconds * transition.Profile.Duration(IsShowing) / Duration);
        }
        public void Complete() => transition.Complete();
    }
}
