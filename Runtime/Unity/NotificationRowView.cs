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
        [SerializeField] private bool stretchSeverityWithRow = true;
        private NotificationSeverity currentSeverity;
        private NotificationRowVisualBaseline baseline;
        private NotificationRowLayout layout;

        public NotificationId NotificationId { get; private set; }
        public Color BodyColor => bodyText != null ? bodyText.color : Color.white;
        public NotificationViewStyle ViewStyle => style;
        public float PreferredHeight
        {
            get
            {
                var rect = transform as RectTransform;
                var element = GetComponent<LayoutElement>();
                return Mathf.Max(rect != null ? rect.rect.height : 0, element != null ? element.preferredHeight : 0);
            }
        }
        public NotificationRowAppearance AuthoredAppearance(NotificationSeverity severity)
        { EnsureBaseline(); return baseline.Colors(ResolveSeverityColor(severity)); }
        internal event System.Action AppearanceChanged;

        public void Configure(
            TMP_Text title,
            TMP_Text body,
            Graphic severity,
            NotificationViewStyle viewStyle = null,
            bool stretchSeverity = true)
        {
            titleText = title;
            bodyText = body;
            severityGraphic = severity;
            style = viewStyle;
            stretchSeverityWithRow = stretchSeverity;
            baseline = null;
            layout = null;
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
            EnsureBaseline();
            if (!DeucarianThemeRuntimeResolver.UseVisualStyling) { OnVisualStylingDisabled(); return; }
            DeucarianTheme theme = ResolveTheme(null);
            if (theme != null) ApplyResolvedTheme(theme);
            else OnVisualStylingDisabled();
        }

        protected override void ApplyResolvedTheme(DeucarianTheme theme)
        {
            EnsureBaseline();
            baseline.Restore();
            layout?.Restore();
            var appearance = NotificationRowAppearance.Resolve(theme, style, currentSeverity, baseline.Colors(ResolveSeverityColor(currentSeverity)));
            if (severityGraphic != null) severityGraphic.color = appearance.Severity;
            GetComponent<NotificationRowDecoration>()?.Apply(currentSeverity, appearance.Severity);
            if (backgroundGraphic != null) backgroundGraphic.color = appearance.Surface;
            if (titleText != null) titleText.color = appearance.Title;
            if (bodyText != null) bodyText.color = appearance.Body;
            if (theme.VisualStyle != null && theme.VisualStyle.TypographyProfile != null)
            {
                ApplyTypography(titleText, DeucarianThemeTextRole.Title, theme.VisualStyle);
                ApplyTypography(bodyText, DeucarianThemeTextRole.Body, theme.VisualStyle);
            }
            layout?.Fit(true);
            AppearanceChanged?.Invoke();
        }

        private void EnsureBaseline()
        {
            if (backgroundGraphic == null) backgroundGraphic = GetComponent<Graphic>();
            if (baseline == null) baseline = new NotificationRowVisualBaseline(backgroundGraphic, titleText, bodyText);
            if (layout == null) layout = new NotificationRowLayout(transform as RectTransform, titleText, bodyText, severityGraphic, stretchSeverityWithRow);
        }

        internal void CopyAuthoredBaselineFrom(NotificationRowView template)
        {
            if (template == null) throw new System.ArgumentNullException(nameof(template));
            template.EnsureBaseline();
            EnsureBaseline();
            baseline = template.baseline.CopyTo(backgroundGraphic, titleText, bodyText);
            layout = template.layout.CopyTo(transform as RectTransform, titleText, bodyText, severityGraphic);
            baseline.Restore();
            layout.Restore();
            if (isActiveAndEnabled) ApplyAppearance();
        }

        protected override void OnVisualStylingDisabled()
        {
            baseline?.Restore();
            layout?.Restore();
            layout?.Fit(true);
            if (severityGraphic != null) severityGraphic.color = ResolveSeverityColor(currentSeverity);
            GetComponent<NotificationRowDecoration>()?.Apply(currentSeverity, ResolveSeverityColor(currentSeverity));
            AppearanceChanged?.Invoke();
        }

        private void LateUpdate()
        {
            if (layout != null && layout.Fit()) AppearanceChanged?.Invoke();
        }

        protected override void OnRuntimeSettingsChanged(Object asset)
        {
            if (asset is DeucarianThemeRuntimeSettings || asset is DeucarianColorPalette || asset is DeucarianThemeStyle)
                ApplyAppearance();
        }

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
