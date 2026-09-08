using System.Collections;
using System.Linq;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Deucarian.Notifications.PlayModeTests
{
    public sealed class NotificationPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EveryMotionModeShowsAndHidesUsingUnscaledTime()
        {
            float oldScale = Time.timeScale;
            GameObject root = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"));
            try
            {
                Time.timeScale = 0;
                var view = root.GetComponent<NotificationListView>();
                using (var store = new NotificationStore())
                using (var presenter = new NotificationPresenter(store, view))
                {
                    presenter.Activate();
                    foreach (NotificationTransition mode in new[] { NotificationTransition.None, NotificationTransition.Fade, NotificationTransition.Scale, NotificationTransition.Slide })
                    {
                        var settings = NotificationPresentationSettings.Default;
                        settings.show = settings.hide = mode;
                        settings.showSeconds = settings.hideSeconds = 0.1f;
                        view.ConfigurePresentation(settings);
                        store.ApplyBatch(new[] { NotificationCommand.Activate(Message("test")) }, 0);
                        var row = root.GetComponentsInChildren<NotificationRowView>().Single(x => x.NotificationId.Value == "test");
                        if (mode == NotificationTransition.Fade) Assert.AreEqual(0, row.GetComponent<CanvasGroup>().alpha, 0.01);
                        if (mode == NotificationTransition.Scale) Assert.Less(row.transform.localScale.x, 1);
                        yield return new WaitForSecondsRealtime(0.2f);
                        Assert.AreEqual(1, row.GetComponent<CanvasGroup>().alpha, 0.01);
                        Assert.AreEqual(Vector3.one, row.transform.localScale);
                        store.ApplyBatch(new[] { NotificationCommand.Resolve("test") }, 1);
                        if (mode != NotificationTransition.None) Assert.IsTrue(row.gameObject.activeSelf, "Exit needs to be visible before recycling.");
                        yield return new WaitForSecondsRealtime(0.2f);
                        Assert.AreEqual(0, view.RenderedRowCount);
                        Assert.IsFalse(row.gameObject.activeSelf);
                    }
                }
            }
            finally { Time.timeScale = oldScale; Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator OverflowWaitsForAnExitSlotAndReappearingKeyReversesWithoutDuplicates()
        {
            GameObject root = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"));
            try
            {
                var view = root.GetComponent<NotificationListView>();
                using (var store = new NotificationStore())
                using (var presenter = new NotificationPresenter(store, view))
                {
                    presenter.Activate();
                    store.ApplyBatch(Enumerable.Range(0, 8).Select(i => NotificationCommand.Activate(Message("item." + i))), 0);
                    Assert.AreEqual(8, store.Snapshot.Count);
                    Assert.AreEqual(5, view.RenderedRowCount);
                    Assert.AreEqual(3, view.OverflowCount);
                    Assert.AreEqual("+3 more", root.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "Notification Overflow").text);
                    yield return new WaitForSecondsRealtime(0.25f);
                    store.ApplyBatch(new[] { NotificationCommand.Resolve("item.0") }, 1);
                    Assert.AreEqual(5, view.RenderedRowCount, "Exiting rows still occupy one of the five slots.");
                    store.ApplyBatch(new[] { NotificationCommand.Activate(Message("item.0", 100)) }, 1.01);
                    yield return new WaitForSecondsRealtime(0.25f);
                    Assert.AreEqual(1, root.GetComponentsInChildren<NotificationRowView>().Count(x => x.NotificationId.Value == "item.0"));
                    Assert.AreEqual(5, view.RenderedRowCount);
                    store.ApplyBatch(new[] { NotificationCommand.Resolve("item.0") }, 2);
                    yield return new WaitForSecondsRealtime(0.4f);
                    Assert.AreEqual(5, view.RenderedRowCount);
                    Assert.IsTrue(root.GetComponentsInChildren<NotificationRowView>().Any(x => x.NotificationId.Value == "item.5"));
                    Assert.IsTrue(root.GetComponentsInChildren<Graphic>(true).All(x => !x.raycastTarget));
                    var settings = view.Presentation;
                    settings.maxVisible = 2;
                    view.ConfigurePresentation(settings);
                    Assert.LessOrEqual(view.RenderedRowCount, 2);
                    Assert.AreEqual(5, view.OverflowCount);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator ThemeRolesApplyAndLiveThemeChangesDoNotReplaceTheEpisode()
        {
            var root = new GameObject("Theme host");
            var role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            var secondaryRole = ScriptableObject.CreateInstance<DeucarianColorRole>();
            var palette = ScriptableObject.CreateInstance<DeucarianColorPalette>();
            var theme = ScriptableObject.CreateInstance<DeucarianTheme>();
            try
            {
                role.Configure(DeucarianBuiltinColorRoleIds.TextPrimary, "Text", "Text", "", Color.white, false);
                secondaryRole.Configure(DeucarianBuiltinColorRoleIds.TextSecondary, "Body", "Text", "", Color.white, false);
                palette.Configure("test.palette", "Test", null);
                palette.SetColor(role, Color.cyan);
                palette.SetColor(secondaryRole, Color.magenta);
                theme.Configure("test.theme", "Test", palette);
                var provider = root.AddComponent<DeucarianThemeProvider>();
                provider.SetTheme(theme);
                var listRoot = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"), root.transform);
                var view = listRoot.GetComponent<NotificationListView>();
                var settings = view.Presentation;
                settings.maxVisible = 1;
                view.ConfigurePresentation(settings);
                using (var store = new NotificationStore())
                using (var presenter = new NotificationPresenter(store, view))
                {
                    presenter.Activate();
                    store.ApplyBatch(new[] { NotificationCommand.Activate(Message("theme")) }, 0);
                    store.ApplyBatch(new[] { NotificationCommand.Activate(Message("overflow")) }, 1);
                    yield return null;
                    var row = listRoot.GetComponentsInChildren<NotificationRowView>().Single();
                    var title = row.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "Title");
                    Assert.AreEqual(Color.cyan, title.color);
                    var overflow = listRoot.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "Notification Overflow");
                    Assert.AreEqual(Color.magenta, overflow.color);
                    long episode = store.Snapshot[0].Episode;
                    palette.SetColor(role, Color.yellow);
                    palette.SetColor(secondaryRole, Color.green);
                    provider.SetTheme(theme);
                    yield return null;
                    Assert.AreEqual(Color.yellow, title.color);
                    Assert.AreEqual(Color.green, overflow.color);
                    Assert.AreEqual(episode, store.Snapshot[0].Episode);
                }
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(theme);
                Object.DestroyImmediate(palette); Object.DestroyImmediate(role); Object.DestroyImmediate(secondaryRole);
            }
        }

        private static NotificationDefinition Message(string id, int priority = 10) =>
            new NotificationDefinition(id, NotificationSeverity.Warning, id, "Example body", priority);
    }
}
