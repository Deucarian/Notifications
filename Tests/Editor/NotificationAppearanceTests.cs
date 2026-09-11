using Deucarian.Theming;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;
using Deucarian.Notifications.Editor;
using System.Reflection;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationAppearanceTests
    {
        [Test]
        public void TurningStylingOffRestoresAuthoredRowColors()
        {
            var parent = new GameObject("Row theme test", typeof(RectTransform));
            parent.SetActive(false);
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            try
            {
                var row = DeucarianNotificationPrefabFactory.CreateRowTemplate((RectTransform)parent.transform, null);
                Color original = row.BodyColor;
                role.Configure(DeucarianBuiltinColorRoleIds.TextSecondary, "Body", "", "", Color.magenta, false);
                palette.SetColor(role, Color.magenta);
                theme.Configure("row.test", "Test", palette);
                row.ApplyTheme(theme);
                Assert.That(row.BodyColor, Is.EqualTo(Color.magenta));
                typeof(NotificationRowView).GetMethod("OnVisualStylingDisabled", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(row, null);
                Assert.That(row.BodyColor, Is.EqualTo(original));
                row.ApplyTheme(theme);
                Assert.That(row.BodyColor, Is.EqualTo(Color.magenta));
            }
            finally { Object.DestroyImmediate(parent); Object.DestroyImmediate(theme); Object.DestroyImmediate(palette); Object.DestroyImmediate(role); }
        }

        [Test]
        public void NullThemeKeepsAuthoredDefaultsAndAssignedThemeResolvesSemanticSeverity()
        {
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            try
            {
                var fallback = NotificationRowAppearance.Default(NotificationSeverity.Warning);
                var unchanged = NotificationRowAppearance.Resolve(null, null, NotificationSeverity.Warning, fallback);
                Assert.That(unchanged.Surface, Is.EqualTo(fallback.Surface));
                Assert.That(unchanged.Severity, Is.EqualTo(fallback.Severity));
                role.Configure(DeucarianBuiltinColorRoleIds.Warning, "Warning", "", "", Color.magenta, false);
                palette.SetColor(role, Color.magenta);
                theme.Configure("test", "Test", palette);
                var themed = NotificationRowAppearance.Resolve(theme, null, NotificationSeverity.Warning, fallback);
                Assert.That(themed.Severity, Is.EqualTo(Color.magenta));
                Assert.That(themed.Body, Is.EqualTo(fallback.Body));
            }
            finally { Object.DestroyImmediate(theme); Object.DestroyImmediate(palette); Object.DestroyImmediate(role); }
        }
    }
}
