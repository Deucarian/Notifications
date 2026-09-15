using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationCloneBaselineTests
    {
        [UnityTest]
        public IEnumerator ClonesAndReusedRowsRestoreAuthoredAppearanceAfterTheirInactiveTemplateWasThemed()
        {
            yield return new EnterPlayMode();
            Assert.That(DeucarianThemeRuntimeResolver.LoadSettings(), Is.Null,
                "Run this isolated resource test in a consumer without project settings.");
            string folder = "Assets/NotificationCloneBaseline-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            AssetDatabase.CreateFolder(folder, "Resources");
            var settings = ScriptableObject.CreateInstance<DeucarianThemeRuntimeSettings>();
            AssetDatabase.CreateAsset(settings, folder + "/Resources/" + DeucarianThemeRuntimeSettings.ResourceName + ".asset");
            var parent = new GameObject("Clone baseline fixture", typeof(RectTransform), typeof(Canvas));
            parent.SetActive(false);
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var style = ScriptableObject.CreateInstance<DeucarianThemeStyle>();
            var typography = ScriptableObject.CreateInstance<DeucarianThemeTypographyProfile>();
            var roles = new List<DeucarianColorRole>();
            try
            {
                var instance = Object.Instantiate(Resources.Load<GameObject>(
                    "Deucarian/Notifications/Defaults/DefaultNotificationList"), parent.transform, false);
                var view = instance.GetComponent<NotificationListView>();
                var template = Object.Instantiate(view.RowTemplate, parent.transform, false);
                var container = (RectTransform)instance.transform.Find("Rows");
                view.Configure(container, template);
                var authoredFont = Label(template, "Title").font;
                var themedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Packages/com.deucarian.theming/Runtime/Fonts/Montserrat-Regular SDF.asset");
                Assert.That(authoredFont, Is.Not.Null);
                Assert.That(themedFont, Is.Not.Null.And.Not.SameAs(authoredFont));
                AuthorTemplate(template);
                AddColor(palette, roles, DeucarianBuiltinColorRoleIds.SurfaceRaised, Color.white);
                AddColor(palette, roles, DeucarianBuiltinColorRoleIds.TextPrimary, Color.green);
                AddColor(palette, roles, DeucarianBuiltinColorRoleIds.TextSecondary, Color.red);
                typography.Configure(themedFont, new DeucarianThemeTextStyle(72, FontStyles.Bold),
                    new DeucarianThemeTextStyle(48), new DeucarianThemeTextStyle(24));
                style.SetComposition(null, null, null, DeucarianThemeDensity.Standard, typography);
                theme.Configure("clone-baseline", "Clone baseline", palette, style);
                settings.Configure(theme);
                var provider = parent.AddComponent<DeucarianThemeProvider>();
                provider.SetTheme(theme);
                Assert.That(template.gameObject.activeSelf, Is.False);
                Assert.That(Label(template, "Title").fontSize, Is.EqualTo(72),
                    "The regression requires the inactive source template to be themed before cloning.");
                Assert.That(template.PreferredHeight, Is.GreaterThan(104));
                parent.SetActive(true);
                var presentation = NotificationPresentationSettings.Default;
                presentation.show = presentation.hide = NotificationTransition.None;
                view.ConfigurePresentation(presentation);
                using (var store = new NotificationStore(definitions: new RegisteredTestDefinitions("clone")))
                using (var presenter = new NotificationPresenter(store, view))
                {
                    presenter.Activate();
                    NotificationRowView previous = null;
                    for (int cycle = 0; cycle < 2; cycle++)
                    {
                        settings.SetFeatures(true, true);
                        provider.RefreshThemeGraph();
                        store.ApplyBatch(new[] { NotificationCommand.Activate(new NotificationDefinition(
                            "clone", NotificationSeverity.Warning, "Warning", "Instruction")) }, cycle * 2);
                        yield return null;
                        var row = instance.GetComponentsInChildren<NotificationRowView>().Single();
                        if (previous != null) Assert.That(row, Is.SameAs(previous), "Also exercise pooled-row reuse.");
                        previous = row;
                        Assert.That(Label(row, "Title").font, Is.SameAs(themedFont));
                        Assert.That(Label(row, "Title").fontSize, Is.EqualTo(72));
                        Assert.That(Label(row, "Title").color, Is.EqualTo(Color.green));
                        Assert.That(row.PreferredHeight, Is.GreaterThan(104));
                        settings.SetFeatures(false, true);
                        provider.RefreshThemeGraph();
                        yield return null;
                        AssertAuthored(row, authoredFont);
                        store.ApplyBatch(new[] { NotificationCommand.Resolve("clone") }, cycle * 2 + 1);
                        yield return null;
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(parent); Object.DestroyImmediate(theme); Object.DestroyImmediate(palette);
                Object.DestroyImmediate(style); Object.DestroyImmediate(typography);
                foreach (var role in roles) Object.DestroyImmediate(role);
                AssetDatabase.DeleteAsset(folder);
            }
            yield return new ExitPlayMode();
        }

        private static void AuthorTemplate(NotificationRowView row)
        {
            var title = Label(row, "Title"); var body = Label(row, "Body");
            title.fontSize = 17; title.fontStyle = FontStyles.Italic; title.color = Color.yellow;
            title.characterSpacing = 1; title.lineSpacing = 2;
            body.fontSize = 14; body.fontStyle = FontStyles.Normal; body.color = Color.cyan;
            body.characterSpacing = 3; body.lineSpacing = 4;
            row.GetComponent<Image>().color = Color.magenta;
            ((RectTransform)row.transform).sizeDelta = new Vector2(620, 104);
            row.GetComponent<LayoutElement>().preferredHeight = 104;
            title.rectTransform.sizeDelta = new Vector2(590, 34);
            title.rectTransform.anchoredPosition = new Vector2(18, -12);
            body.rectTransform.sizeDelta = new Vector2(590, 36);
            body.rectTransform.anchoredPosition = new Vector2(18, -55);
            ((RectTransform)row.transform.Find("Severity")).sizeDelta = new Vector2(6, 104);
        }

        private static void AssertAuthored(NotificationRowView row, TMP_FontAsset font)
        {
            var title = Label(row, "Title"); var body = Label(row, "Body");
            Assert.That(title.font, Is.SameAs(font)); Assert.That(body.font, Is.SameAs(font));
            Assert.That(title.fontSize, Is.EqualTo(17)); Assert.That(body.fontSize, Is.EqualTo(14));
            Assert.That(title.fontStyle, Is.EqualTo(FontStyles.Italic)); Assert.That(body.fontStyle, Is.EqualTo(FontStyles.Normal));
            Assert.That(title.color, Is.EqualTo(Color.yellow)); Assert.That(body.color, Is.EqualTo(Color.cyan));
            Assert.That(title.characterSpacing, Is.EqualTo(1)); Assert.That(title.lineSpacing, Is.EqualTo(2));
            Assert.That(body.characterSpacing, Is.EqualTo(3)); Assert.That(body.lineSpacing, Is.EqualTo(4));
            Assert.That(row.GetComponent<Image>().color, Is.EqualTo(Color.magenta));
            Assert.That(row.PreferredHeight, Is.EqualTo(104));
            Assert.That(((RectTransform)row.transform).sizeDelta, Is.EqualTo(new Vector2(620, 104)));
            Assert.That(row.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(104));
            Assert.That(title.rectTransform.sizeDelta, Is.EqualTo(new Vector2(590, 34)));
            Assert.That(title.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(18, -12)));
            Assert.That(body.rectTransform.sizeDelta, Is.EqualTo(new Vector2(590, 36)));
            Assert.That(body.rectTransform.anchoredPosition, Is.EqualTo(new Vector2(18, -55)));
            Assert.That(((RectTransform)row.transform.Find("Severity")).sizeDelta, Is.EqualTo(new Vector2(6, 104)));
        }

        private static TMP_Text Label(NotificationRowView row, string name) => row.transform.Find(name).GetComponent<TMP_Text>();

        private static void AddColor(DeucarianColorPalette palette, List<DeucarianColorRole> roles, string id, Color color)
        {
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            roles.Add(role);
            role.Configure(id, id, "Test", "", color, false);
            palette.SetColor(role, color);
        }
    }
}
