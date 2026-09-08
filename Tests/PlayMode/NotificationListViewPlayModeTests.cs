using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.PlayModeTests
{
    public sealed class NotificationListViewPlayModeTests
    {
        private sealed class FeedbackSink : INotificationFeedbackSink
        {
            public int Count { get; private set; }

            public bool TryRequestFeedback(NotificationFeedbackRequest request)
            {
                Count++;
                return true;
            }
        }

        [UnityTest]
        public IEnumerator BundledViewStacksWithoutGapsAndNeverInterceptsInput()
        {
            GameObject prefab = Resources.Load<GameObject>(
                "Deucarian/Notifications/Defaults/DefaultNotificationList");
            Assert.NotNull(prefab);
            GameObject instance = Object.Instantiate(prefab);
            NotificationListView view = instance.GetComponent<NotificationListView>();
            var presentation = NotificationPresentationSettings.Default;
            presentation.show = presentation.hide = NotificationTransition.None;
            view.ConfigurePresentation(presentation);
            FeedbackSink feedback = new FeedbackSink();
            using (NotificationStore store = new NotificationStore(feedback))
            using (NotificationPresenter presenter = new NotificationPresenter(store, view))
            {
                presenter.Activate();
                store.ApplyBatch(new[]
                {
                    NotificationCommand.Activate(Warning("imu", 80)),
                    NotificationCommand.Activate(Warning("gnss", 100)),
                    NotificationCommand.Activate(Warning("fix", 90))
                }, 1d);
                yield return null;

                Assert.AreEqual(3, view.VisibleCount);
                Assert.AreEqual(1, feedback.Count);
                AssertAllGraphicsIgnoreRaycasts(instance);
                AssertVisibleOrder(instance, "sample.gnss", "sample.fix", "sample.imu");

                store.ApplyBatch(
                    new[] { NotificationCommand.Resolve("sample.fix") },
                    2d);
                yield return null;

                Assert.AreEqual(2, view.VisibleCount);
                Assert.AreEqual(1, feedback.Count);
                AssertVisibleOrder(instance, "sample.gnss", "sample.imu");

                instance.SetActive(false);
                yield return null;
                instance.SetActive(true);
                presenter.Activate();
                yield return null;
                Assert.AreEqual(2, view.VisibleCount);
                Assert.AreEqual(1, feedback.Count, "View restoration must not replay activation audio.");
            }

            Object.Destroy(instance);
            yield return null;
        }

        private static NotificationDefinition Warning(string suffix, int priority)
        {
            return new NotificationDefinition(
                "sample." + suffix,
                NotificationSeverity.Warning,
                suffix,
                "Body",
                priority,
                "deucarian.feedback.audio.warning");
        }

        [UnityTest]
        public IEnumerator BundledLabelsStaySeparateIncludingLongContentAndReusedRows()
        {
            var canvas = new GameObject("Notification layout test", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            GameObject instance = Object.Instantiate(Resources.Load<GameObject>(
                "Deucarian/Notifications/Defaults/DefaultNotificationList"), canvas.transform, false);
            try
            {
                var view = instance.GetComponent<NotificationListView>();
                var settings = NotificationPresentationSettings.Default;
                settings.show = settings.hide = NotificationTransition.None;
                view.ConfigurePresentation(settings);
                using (var store = new NotificationStore())
                using (var presenter = new NotificationPresenter(store, view))
                {
                    presenter.Activate();
                    foreach (string title in new[] { "GNSS signal lost", string.Concat(Enumerable.Repeat("Long warning title ", 12)) })
                    {
                        store.ApplyBatch(new[] { NotificationCommand.Activate(new NotificationDefinition(
                            "layout", NotificationSeverity.Warning, title,
                            "Please walk to an open area. " + string.Concat(Enumerable.Repeat("Extra details. ", 12)))) }, 0);
                        yield return null;
                        Canvas.ForceUpdateCanvases();
                        var row = instance.GetComponentsInChildren<NotificationRowView>().Single();
                        TMP_Text heading = row.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "Title");
                        TMP_Text body = row.GetComponentsInChildren<TMP_Text>().Single(x => x.name == "Body");
                        RectTransform headingRect = heading.rectTransform;
                        RectTransform bodyRect = body.rectTransform;
                        float titleBottom = headingRect.TransformPoint(new Vector3(0, headingRect.rect.yMin)).y;
                        float bodyTop = bodyRect.TransformPoint(new Vector3(0, bodyRect.rect.yMax)).y;
                        Assert.Greater(titleBottom, bodyTop, "Title and instruction need separate, non-overlapping text rectangles.");
                        Assert.Greater(GlyphRange(heading).x, GlyphRange(body).y,
                            "Rendered title and instruction glyphs must not overlap, including ellipsized content.");
                        store.ApplyBatch(new[] { NotificationCommand.Resolve("layout") }, 1);
                        yield return null;
                    }
                }
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        private static Vector2 GlyphRange(TMP_Text label)
        {
            label.ForceMeshUpdate();
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int i = 0; i < label.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = label.textInfo.characterInfo[i];
                if (!character.isVisible) continue;
                min = Mathf.Min(min, label.transform.TransformPoint(character.bottomLeft).y);
                max = Mathf.Max(max, label.transform.TransformPoint(character.topRight).y);
            }
            Assert.IsFalse(float.IsInfinity(min), "The test must measure actual visible glyphs.");
            return new Vector2(min, max);
        }

        private static void AssertAllGraphicsIgnoreRaycasts(GameObject root)
        {
            UnityEngine.UI.Graphic[] graphics =
                root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            Assert.Greater(graphics.Length, 0);
            for (int i = 0; i < graphics.Length; i++)
            {
                Assert.IsFalse(graphics[i].raycastTarget, graphics[i].name);
            }

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            Assert.NotNull(group);
            Assert.IsFalse(group.blocksRaycasts);
            Assert.IsFalse(group.interactable);
        }

        private static void AssertVisibleOrder(GameObject root, params string[] expected)
        {
            NotificationRowView[] all = root.GetComponentsInChildren<NotificationRowView>(true);
            int found = 0;
            int previousSibling = -1;
            for (int i = 0; i < all.Length; i++)
            {
                NotificationRowView row = all[i];
                if (!row.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Assert.Less(found, expected.Length, "Unexpected visible notification row.");
                Assert.AreEqual(expected[found], row.NotificationId.Value);
                Assert.Greater(row.transform.GetSiblingIndex(), previousSibling);
                previousSibling = row.transform.GetSiblingIndex();
                found++;
            }

            Assert.AreEqual(expected.Length, found);
        }
    }
}
