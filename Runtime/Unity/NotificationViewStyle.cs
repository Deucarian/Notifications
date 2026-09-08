using UnityEngine;
using Deucarian.Theming;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Reusable visual defaults for notification rows without owning application theme data.</summary>
    [CreateAssetMenu(
        fileName = "Notification View Style",
        menuName = "Deucarian/Notifications/View Style")]
    public sealed class NotificationViewStyle : ScriptableObject
    {
        [SerializeField] private string surfaceRole = DeucarianBuiltinColorRoleIds.SurfaceRaised;
        [SerializeField] private string titleRole = DeucarianBuiltinColorRoleIds.TextPrimary;
        [SerializeField] private string bodyRole = DeucarianBuiltinColorRoleIds.TextSecondary;
        public string SurfaceRole => surfaceRole;
        public string TitleRole => titleRole;
        public string BodyRole => bodyRole;
        [SerializeField] private Color infoColor = new Color(0.2f, 0.65f, 1f, 1f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.45f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.15f, 1f);
        [SerializeField] private Color errorColor = new Color(1f, 0.25f, 0.2f, 1f);

        public Color InfoColor => infoColor;
        public Color SuccessColor => successColor;
        public Color WarningColor => warningColor;
        public Color ErrorColor => errorColor;

        public void Configure(Color info, Color success, Color warning, Color error)
        {
            infoColor = info;
            successColor = success;
            warningColor = warning;
            errorColor = error;
        }

        public Color Resolve(NotificationSeverity severity)
        {
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
    }
}
