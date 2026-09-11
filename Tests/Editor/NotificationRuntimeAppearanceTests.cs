using System;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Image = UnityEngine.UI.Image;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationRuntimeAppearanceTests
    {
        [Test]
        public void VisualOffRestoresRuntimeTypographyAndPreviewsTheConnectedListsAuthoredColors()
        {
            Assert.That(DeucarianThemeRuntimeResolver.LoadSettings(), Is.Null,
                "Run this isolated resource test in a consumer without project settings.");
            string folder = "NotificationAppearance-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            AssetDatabase.CreateFolder("Assets/" + folder, "Resources");
            var settings = ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/" + folder + "/Resources/" + DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            var parent = new GameObject("Appearance fixture", typeof(RectTransform));
            parent.SetActive(false);
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            var typography = ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>();
            var rowStyle = ScriptableObject.CreateInstance<NotificationViewStyle>();
            try
            {
                rowStyle.Configure(Color.blue, Color.green, Color.magenta, Color.red);
                var template = DeucarianNotificationPrefabFactory.CreateRowTemplate((RectTransform)parent.transform, rowStyle);
                var text = template.transform.Find("Title").GetComponent<TMP_Text>();
                text.fontSize = 17; text.color = Color.yellow;
                template.GetComponent<Image>().color = Color.cyan;
                typography.Configure(null, new DeucarianThemeTextStyle(32, FontStyles.Bold),
                    new DeucarianThemeTextStyle(20), new DeucarianThemeTextStyle(14));
                style.SetComposition(null, null, null, DeucarianThemeDensity.Standard, typography);
                theme.Configure("fixture", "Fixture", palette);
                theme.SetVisualStyle(style);
                settings.Configure(theme);
                var provider = parent.AddComponent<DeucarianThemeProvider>();
                provider.SetTheme(theme);
                var list = parent.AddComponent<NotificationListView>();
                list.Configure((RectTransform)parent.transform, template);
                template.gameObject.SetActive(true); parent.SetActive(true);
                provider.RefreshThemeGraph();
                Assert.That(text.fontSize, Is.EqualTo(32));

                settings.SetFeatures(false, true);
                provider.RefreshThemeGraph();
                Assert.That(text.fontSize, Is.EqualTo(17));
                Assert.That(text.color, Is.EqualTo(Color.yellow));
                typography.Configure(null, new DeucarianThemeTextStyle(40, FontStyles.Italic),
                    new DeucarianThemeTextStyle(24), new DeucarianThemeTextStyle(16));
                Assert.That(text.fontSize, Is.EqualTo(17));

                using var workspace = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
                using var preview = new NotificationLabRowPreview(workspace);
                preview.Configure(NotificationPresentationSettings.Default, list);
                workspace.SetMessages(new[] { new DeucarianEditorMessageData("warning", "Warning", "Content", DeucarianEditorStatus.Warning, "") },
                    Array.Empty<DeucarianEditorMessageData>(), 0);
                var row = workspace.VisibleRows.Q<DeucarianEditorMessageRow>("warning");
                Assert.That(row.Title.style.color.value, Is.EqualTo(Color.yellow));
                Assert.That(row.style.backgroundColor.value, Is.EqualTo(Color.cyan));
                Assert.That(template.AuthoredAppearance(NotificationSeverity.Warning).Severity, Is.EqualTo(Color.magenta));
                Assert.That(NotificationRowAppearance.Resolve(null, rowStyle, NotificationSeverity.Warning,
                    NotificationRowAppearance.Default(NotificationSeverity.Warning)).Severity, Is.EqualTo(Color.magenta));
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(theme); Object.DestroyImmediate(palette); Object.DestroyImmediate(style);
                Object.DestroyImmediate(typography); Object.DestroyImmediate(rowStyle);
                AssetDatabase.DeleteAsset("Assets/" + folder);
            }
        }
    }
}
