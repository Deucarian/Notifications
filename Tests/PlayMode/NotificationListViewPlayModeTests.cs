using System.Collections;
using NUnit.Framework;
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
