using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Notification-row uGUI adapter for the UI package's reversible visibility transition.</summary>
    internal sealed class NotificationRowMotion
    {
        private readonly NotificationRowTransition transition = new NotificationRowTransition();
        private readonly RectTransform rect;
        private readonly CanvasGroup group;
        private Vector2 position;

        public NotificationRowMotion(NotificationRowView row)
        {
            rect = (RectTransform)row.transform;
            CanvasGroup existing = row.GetComponent<CanvasGroup>();
            group = existing != null ? existing : row.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        public bool IsHidden => transition.IsHidden;
        public bool IsShowing => transition.IsShowing;

        public void SetVisible(bool visible, NotificationPresentationSettings value, bool animate)
        {
            transition.SetVisible(visible, value, animate);
            Apply();
        }

        public void Advance(float seconds)
        {
            transition.Advance(seconds);
            Apply();
        }

        public void Position(Vector2 value) { position = value; Apply(); }
        public void Complete() { transition.Complete(); Apply(); }

        private void Apply()
        {
            if (rect == null || group == null) return;
            group.alpha = transition.Alpha;
            rect.localScale = Vector3.one * transition.Scale;
            rect.anchoredPosition = position + transition.Offset;
        }
    }
}
