using TMPro;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Presentation for one keyed notification row.</summary>
    public sealed class NotificationRowView : DeucarianThemeTargetBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Graphic severityGraphic;
        [SerializeField] private Color infoColor = new Color(0.2f, 0.65f, 1f, 1f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.45f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.7f, 0.15f, 1f);
        [SerializeField] private Color errorColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private NotificationViewStyle style;
        [SerializeField] private Graphic backgroundGraphic;
        private NotificationSeverity currentSeverity;

        public NotificationId NotificationId { get; private set; }
        public Color BodyColor => bodyText != null ? bodyText.color : Color.white;
        internal event System.Action AppearanceChanged;

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
            currentSeverity = item.Definition.Severity;
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
            ApplyAppearance();
        }

        protected override void OnEnable()
        {
            ApplyOnEnable = false;
            base.OnEnable();
            ApplyAppearance();
        }

        private void ApplyAppearance()
        {
            DeucarianTheme theme = ResolveTheme(null);
            if (theme != null) ApplyResolvedTheme(theme);
        }

        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            if (backgroundGraphic == null) backgroundGraphic = GetComponent<Graphic>();
            string severityRole = currentSeverity == NotificationSeverity.Error ? DeucarianBuiltinColorRoleIds.Error
                : currentSeverity == NotificationSeverity.Warning ? DeucarianBuiltinColorRoleIds.Warning
                : currentSeverity == NotificationSeverity.Success ? DeucarianBuiltinColorRoleIds.Success : DeucarianBuiltinColorRoleIds.Info;
            if (severityGraphic != null) severityGraphic.color = Resolve(theme, severityRole, ResolveSeverityColor(currentSeverity));
            if (backgroundGraphic != null)
            {
                Color surface = Resolve(theme, style != null ? style.SurfaceRole : DeucarianBuiltinColorRoleIds.SurfaceRaised,
                    new Color(0.07f, 0.08f, 0.10f, 0.92f));
                backgroundGraphic.color = surface;
                DeucarianUGUIThemeStyleUtility.ApplyPanel(backgroundGraphic, surface, theme.VisualStyle);
            }
            if (titleText != null) titleText.color = Resolve(theme,
                style != null ? style.TitleRole : DeucarianBuiltinColorRoleIds.TextPrimary, Color.white);
            if (bodyText != null) bodyText.color = Resolve(theme,
                style != null ? style.BodyRole : DeucarianBuiltinColorRoleIds.TextSecondary, Color.white);
            if (theme.VisualStyle != null && theme.VisualStyle.TypographyProfile != null)
            {
                ApplyTypography(titleText, DeucarianThemeTextRole.Title, theme.VisualStyle);
                ApplyTypography(bodyText, DeucarianThemeTextRole.Body, theme.VisualStyle);
            }
            AppearanceChanged?.Invoke();
        }

        private static Color Resolve(DeucarianTheme theme, string role, Color fallback) =>
            theme.TryGetColorById(role, out Color value) ? value : fallback;

        private static void ApplyTypography(TMP_Text text, DeucarianThemeTextRole role, DeucarianThemeStyle visualStyle)
        {
            if (text == null) return;
            var target = text.GetComponent<DeucarianTMPThemeTypography>();
            if (target == null) target = text.gameObject.AddComponent<DeucarianTMPThemeTypography>();
            target.TextRole = role;
            target.ApplyStyle(visualStyle);
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
