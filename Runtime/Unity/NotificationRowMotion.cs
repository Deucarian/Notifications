using Deucarian.UI;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Notification-row uGUI adapter for the UI package's reversible visibility transition.</summary>
    internal sealed class NotificationRowMotion
    {
        private readonly DeucarianVisibilityTransition transition =
            new DeucarianVisibilityTransition(DeucarianMotionProfile.UiPanel);
        private readonly RectTransform rect;
        private readonly CanvasGroup group;
        private NotificationPresentationSettings settings;
        private Vector2 position;
        private bool showing;

        public NotificationRowMotion(NotificationRowView row)
        {
            rect = (RectTransform)row.transform;
            CanvasGroup existing = row.GetComponent<CanvasGroup>();
            group = existing != null ? existing : row.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public bool IsHidden => transition.Phase == DeucarianVisibilityPhase.Hidden;
        public bool IsShowing => showing;

        public void SetVisible(bool visible, NotificationPresentationSettings value, bool animate)
        {
            settings = value;
            showing = visible;
            if (visible) transition.Show(); else transition.Hide();
            if (!animate || Mode == NotificationTransition.None || Duration <= 0) transition.Complete();
            Apply();
        }

        public void Advance(float seconds)
        {
            if (Duration <= 0) transition.Complete();
            else transition.Advance(seconds * transition.Profile.Duration(showing) / Duration);
            Apply();
        }

        public void Position(Vector2 value) { position = value; Apply(); }
        public void Complete() { transition.Complete(); Apply(); }
        private NotificationTransition Mode => showing ? settings.show : settings.hide;
        private float Duration => showing ? settings.showSeconds : settings.hideSeconds;

        private void Apply()
        {
            if (rect == null || group == null) return;
            float progress = transition.Progress;
            group.alpha = Mode == NotificationTransition.Fade ? progress : progress > 0 ? 1 : 0;
            rect.localScale = Vector3.one * (Mode == NotificationTransition.Scale ? Mathf.Lerp(0.85f, 1f, progress) : 1f);
            rect.anchoredPosition = position + (Mode == NotificationTransition.Slide ? new Vector2(-60f * (1f - progress), 0) : Vector2.zero);
        }
    }
}
