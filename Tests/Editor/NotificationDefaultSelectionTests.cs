using System.Linq;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationDefaultSelectionTests
    {
        [Test]
        public void DefaultIsOnePackagePrefabAndResetDoesNotCopyItIntoTheProject()
        {
            var settings = ScriptableObject.CreateInstance<NotificationViewSettings>();
            var customRoot = new GameObject("Custom row", typeof(RectTransform), typeof(NotificationRowView));
            try
            {
                var packageRow = NotificationViewDefaults.LoadRowPrefab();
                Assert.That(AssetDatabase.GetAssetPath(packageRow), Is.EqualTo(
                    "Packages/com.deucarian.notifications/Runtime/Resources/Deucarian/Notifications/Defaults/DefaultNotificationRow.prefab"));
                Assert.That(NotificationViewDefaults.LoadListPrefab().GetComponent<NotificationListView>().RowTemplate, Is.SameAs(packageRow));
                settings.UseCustomPrefab(customRoot.GetComponent<NotificationRowView>());
                Assert.That(NotificationViewDefaults.ResolveRowPrefab(settings), Is.SameAs(settings.CustomPrefab));
                settings.ChangeToDefault();
                Assert.That(settings.CustomPrefab, Is.Null);
                Assert.That(NotificationViewDefaults.ResolveRowPrefab(settings), Is.SameAs(packageRow));
                settings.ChangeToDefault();
                Assert.That(NotificationViewDefaults.ResolveRowPrefab(settings), Is.SameAs(packageRow));
            }
            finally { Object.DestroyImmediate(customRoot); Object.DestroyImmediate(settings); }
        }

        [Test]
        public void ChangingPrefabPreservesMessagesTimersAndFeedbackAndRebindsAfterDisable()
        {
            var settings = ScriptableObject.CreateInstance<NotificationViewSettings>();
            var instance = Object.Instantiate(NotificationViewDefaults.LoadListPrefab());
            var custom = Object.Instantiate(NotificationViewDefaults.LoadRowPrefab());
            custom.name = "Custom template";
            var view = instance.GetComponent<NotificationListView>();
            var sink = new Feedback();
            using var store = new NotificationStore(sink, new RegisteredTestDefinitions("warning"));
            using var presenter = new NotificationPresenter(store, view);
            try
            {
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 8;
                view.UseProjectRowPrefab(settings);
                presenter.Activate();
                store.ApplyBatch(new[] { NotificationCommand.Activate(new NotificationDefinition("warning", NotificationSeverity.Warning,
                    "Warning", "Body", feedbackRoleId: DeucarianBuiltinAudioRoleIds.Warning, lifetime: NotificationLifetime.Timed(20))) }, 7);
                var snapshot = store.Snapshot;
                settings.UseCustomPrefab(custom);
                Assert.That(view.RowTemplate, Is.SameAs(custom));
                Assert.That(view.VisibleCount, Is.EqualTo(1));
                settings.ChangeToDefault();
                Assert.That(view.RowTemplate, Is.SameAs(NotificationViewDefaults.LoadRowPrefab()));
                Assert.That(view.GetComponentsInChildren<Transform>(true).All(child => child.gameObject.layer == 8), Is.True,
                    "External default and custom row prefabs must inherit their runtime canvas layer.");
                Assert.That(store.Snapshot, Is.SameAs(snapshot));
                Assert.That(store.Snapshot[0].ActivatedAtSeconds, Is.EqualTo(7));
                Assert.That(sink.Count, Is.EqualTo(1));
                instance.SetActive(false);
                settings.UseCustomPrefab(custom);
                instance.SetActive(true);
                Assert.That(view.RowTemplate, Is.SameAs(custom));
                Assert.That(view.VisibleCount, Is.EqualTo(1));
                Assert.That(sink.Count, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(instance); Object.DestroyImmediate(custom.gameObject); Object.DestroyImmediate(settings); }
        }

        [Test]
        public void DefaultCardHasTheLabIconAndBorderAndLargeTextDoesNotStretchTheIcon()
        {
            var row = Object.Instantiate(NotificationViewDefaults.LoadRowPrefab());
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            var style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            var typography = ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>();
            try
            {
                var icon = row.transform.Find("Severity").GetComponent<UnityEngine.UI.Image>();
                Vector2 iconSize = icon.rectTransform.sizeDelta;
                var font = row.GetComponentsInChildren<TMPro.TMP_Text>(true).First().font;
                typography.Configure(font, new DeucarianThemeTextStyle(72), new DeucarianThemeTextStyle(48), new DeucarianThemeTextStyle(24));
                style.SetComposition(null, null, null, DeucarianThemeDensity.Standard, typography);
                role.Configure(DeucarianBuiltinColorRoleIds.Info, "Info", "", "", Color.magenta, false);
                palette.SetColor(role, Color.magenta);
                theme.Configure("test", "Test", palette, style);
                row.ApplyTheme(theme);
                Assert.That(icon.sprite, Is.Not.Null);
                Assert.That(icon.color, Is.EqualTo(Color.magenta));
                Assert.That(icon.rectTransform.sizeDelta, Is.EqualTo(iconSize));
                var border = row.transform.Find("Border").GetComponent<UnityEngine.UI.Image>();
                Assert.That(border.color, Is.EqualTo(new Color(1, 0, 1, .45f)));
                Assert.That(border.sprite, Is.Not.Null);
                Assert.That(row.GetComponent<Outline>(), Is.Null, "A transparent panel must not be tinted by repeated outline fills.");
                Assert.That(row.GetComponent<UnityEngine.UI.Image>().type, Is.EqualTo(UnityEngine.UI.Image.Type.Sliced));
            }
            finally
            {
                Object.DestroyImmediate(row.gameObject); Object.DestroyImmediate(theme); Object.DestroyImmediate(palette);
                Object.DestroyImmediate(role); Object.DestroyImmediate(style); Object.DestroyImmediate(typography);
            }
        }

        private sealed class Feedback : INotificationFeedbackSink
        {
            public int Count { get; private set; }
            public bool TryRequestFeedback(NotificationFeedbackRequest request) { Count++; return true; }
        }
    }
}
