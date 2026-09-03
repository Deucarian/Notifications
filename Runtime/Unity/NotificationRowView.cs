using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Presentation for one keyed notification row.</summary>
    public sealed class NotificationRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Graphic severityGraphic;
        [SerializeField] private Color infoColor = new Color(0.2f, 0.65f, 1f, 1f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.45f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.15f, 1f);
        [SerializeField] private Color errorColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private NotificationViewStyle style;

        public NotificationId NotificationId { get; private set; }

        public void Configure(
            TMP_Text title,
            TMP_Text body,
            Graphic severity,
            NotificationViewStyle viewStyle = null)
        {
            titleText = title;
            bodyText = body;
            severityGraphic = severity;
            style = viewStyle;
            DisableRaycasts();
        }

        public void Render(NotificationItem item)
        {
            NotificationId = item.Id;
            if (titleText != null)
            {
                titleText.text = item.Definition.Title;
                titleText.raycastTarget = false;
            }

            if (bodyText != null)
            {
                bodyText.text = item.Definition.Body;
                bodyText.raycastTarget = false;
            }

            if (severityGraphic != null)
            {
                severityGraphic.color = ResolveSeverityColor(item.Definition.Severity);
                severityGraphic.raycastTarget = false;
            }

            DisableRaycasts();
        }

        private Color ResolveSeverityColor(NotificationSeverity severity)
        {
            if (style != null)
            {
                return style.Resolve(severity);
            }

            switch (severity)
            {
                case NotificationSeverity.Success:
                    return successColor;
                case NotificationSeverity.Warning:
                    return warningColor;
                case NotificationSeverity.Error:
                    return errorColor;
                default:
                    return infoColor;
            }
        }

        private void DisableRaycasts()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        private void Reset()
        {
            TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length > 0)
            {
                titleText = labels[0];
            }

            if (labels.Length > 1)
            {
                bodyText = labels[1];
            }

            DisableRaycasts();
        }
    }
}
