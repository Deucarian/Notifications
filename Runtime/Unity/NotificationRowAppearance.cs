using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Resolved row colors shared by the runtime view and editor preview.</summary>
    public readonly struct NotificationRowAppearance
    {
        public readonly Color Surface, Title, Body, Severity;
        public NotificationRowAppearance(Color surface, Color title, Color body, Color severity)
        { Surface = surface; Title = title; Body = body; Severity = severity; }

        public static NotificationRowAppearance Default(NotificationSeverity severity) =>
            new NotificationRowAppearance(new Color(0.07f, 0.08f, 0.10f, 0.92f), Color.white, Color.white,
                severity == NotificationSeverity.Error ? new Color(1f, 0.25f, 0.2f, 1f) :
                severity == NotificationSeverity.Warning ? new Color(1f, 0.7f, 0.15f, 1f) :
                severity == NotificationSeverity.Success ? new Color(0.2f, 0.8f, 0.45f, 1f) : new Color(0.2f, 0.65f, 1f, 1f));

        public static NotificationRowAppearance Resolve(DeucarianTheme theme, NotificationViewStyle style,
            NotificationSeverity severity, NotificationRowAppearance fallback)
        {
            if (theme == null) return new NotificationRowAppearance(fallback.Surface, fallback.Title, fallback.Body,
                style != null ? style.Resolve(severity) : fallback.Severity);
            string severityRole = severity == NotificationSeverity.Error ? DeucarianBuiltinColorRoleIds.Error
                : severity == NotificationSeverity.Warning ? DeucarianBuiltinColorRoleIds.Warning
                : severity == NotificationSeverity.Success ? DeucarianBuiltinColorRoleIds.Success : DeucarianBuiltinColorRoleIds.Info;
            Color surface = Resolve(theme, style != null ? style.SurfaceRole : DeucarianBuiltinColorRoleIds.SurfaceRaised, fallback.Surface);
            Color severityColor = Resolve(theme, severityRole, style != null ? style.Resolve(severity) : fallback.Severity);
            if (style != null && style.SeverityTint > 0) surface = Color.Lerp(surface, severityColor, style.SeverityTint);
            if (theme.VisualStyle != null && (style == null || style.UseThemeSurfaceTreatment)) surface = theme.VisualStyle.ResolveSurfaceColor(surface);
            Color title = Resolve(theme, style != null ? style.TitleRole : DeucarianBuiltinColorRoleIds.TextPrimary, fallback.Title);
            Color body = Resolve(theme, style != null ? style.BodyRole : DeucarianBuiltinColorRoleIds.TextSecondary, fallback.Body);
            Color backdrop = Resolve(theme, DeucarianBuiltinColorRoleIds.Background, surface);
            Color visibleSurface = DeucarianForegroundContrast.Composite(surface, backdrop);
            var foregrounds = DeucarianForegroundPalette.FromTheme(theme, backdrop, title);
            return new NotificationRowAppearance(surface,
                DeucarianForegroundContrast.Resolve(title, visibleSurface, foregrounds),
                DeucarianForegroundContrast.Resolve(body, visibleSurface, foregrounds), severityColor);
        }

        private static Color Resolve(DeucarianTheme theme, string role, Color fallback) =>
            theme.TryGetColorById(role, out var color) ? color : fallback;
    }
}
