using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationDefaultCardTests
    {
        [TestCase("Dark", NotificationSeverity.Error)]
        [TestCase("Dark", NotificationSeverity.Warning)]
        [TestCase("Dark", NotificationSeverity.Success)]
        [TestCase("Dark", NotificationSeverity.Info)]
        [TestCase("Light", NotificationSeverity.Error)]
        [TestCase("Light", NotificationSeverity.Warning)]
        [TestCase("Light", NotificationSeverity.Success)]
        [TestCase("Light", NotificationSeverity.Info)]
        public void DefaultCardUsesSubtleSeverityTintAndReadablePaletteText(string mode, NotificationSeverity severity)
        {
            var theme = AssetDatabase.LoadAssetAtPath<DeucarianTheme>(
                "Packages/com.deucarian.theming/Runtime/Resources/Deucarian/Theming/Visual/Defaults/Default" + mode + "Theme.asset");
            Assert.That(theme, Is.Not.Null);
            var style = NotificationViewDefaults.LoadRowPrefab().ViewStyle;
            Assert.That(style.SeverityTint, Is.EqualTo(.12f));
            var appearance = NotificationRowAppearance.Resolve(theme, style, severity, NotificationRowAppearance.Default(severity));
            Assert.That(theme.TryGetColorById(DeucarianBuiltinColorRoleIds.Surface, out var surface), Is.True);
            var expected = Color.Lerp(surface, appearance.Severity, .12f);
            if (theme.VisualStyle != null) expected = theme.VisualStyle.ResolveSurfaceColor(expected);
            Assert.That(appearance.Surface, Is.EqualTo(expected));
            theme.TryGetColorById(DeucarianBuiltinColorRoleIds.Background, out var backdrop);
            var visible = DeucarianForegroundContrast.Composite(appearance.Surface, backdrop);
            Assert.That(DeucarianForegroundContrast.Ratio(appearance.Title, visible), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(DeucarianForegroundContrast.Ratio(appearance.Body, visible), Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void DefaultResolveButtonUsesTheOwningServiceAndUnbindsOnDispose()
        {
            var instance = Object.Instantiate(NotificationViewDefaults.LoadListPrefab());
            var view = instance.GetComponent<NotificationListView>();
            var service = new NotificationService(view: view, definitions: new RegisteredTestDefinitions("card"));
            try
            {
                var definition = new NotificationDefinition("card", NotificationSeverity.Warning, "Warning", "Body");
                service.Show(definition);
                var action = view.GetComponentInChildren<NotificationRowAction>();
                Assert.That(action, Is.Not.Null);
                Assert.That(action.Button.gameObject.activeSelf, Is.True);
                Assert.That(action.Button.IsInteractable(), Is.True);
                action.Button.onClick.Invoke();
                Assert.That(service.Snapshot.Count, Is.Zero);
                service.Show(definition);
                service.Dispose();
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                foreach (var remaining in view.GetComponentsInChildren<NotificationRowAction>(true))
                    Assert.That(remaining.Button.gameObject.activeSelf, Is.False);
            }
            finally { service.Dispose(); Object.DestroyImmediate(instance); }
        }

        [Test]
        public void TimedAndReadOnlyCardsHaveNoResolveAction()
        {
            var instance = Object.Instantiate(NotificationViewDefaults.LoadListPrefab());
            var view = instance.GetComponent<NotificationListView>();
            try
            {
                using var service = new NotificationService(view: view, definitions: new RegisteredTestDefinitions("timed"));
                service.Show(new NotificationDefinition("timed", NotificationSeverity.Info, "Notice", "Body", lifetime: NotificationLifetime.Timed(5)));
                var action = view.GetComponentInChildren<NotificationRowAction>();
                Assert.That(action.Button.gameObject.activeSelf, Is.False);
                view.BindResolution(null);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(action.Button.targetGraphic.raycastTarget, Is.False);
            }
            finally { Object.DestroyImmediate(instance); }
        }
    }
}
