using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Adapts a notification's semantic severity to the default card icon and rim.</summary>
    public sealed class NotificationRowDecoration : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Image rim;
        [SerializeField] private Sprite info, success, warning, error;

        public void Configure(Image target, Image border, Sprite infoIcon, Sprite successIcon, Sprite warningIcon, Sprite errorIcon)
        { icon = target; rim = border; info = infoIcon; success = successIcon; warning = warningIcon; error = errorIcon; }

        internal void Apply(NotificationSeverity severity, Color color)
        {
            if (icon != null)
            {
                icon.sprite = severity == NotificationSeverity.Warning ? warning : severity == NotificationSeverity.Error ? error
                    : severity == NotificationSeverity.Success ? success : info;
                icon.color = color;
            }
            if (rim != null) rim.color = new Color(color.r, color.g, color.b, color.a * .45f);
        }
    }
}
