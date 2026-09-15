using System.Collections;
using System.Linq;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Notifications.PlayModeTests
{
    public sealed class NotificationListReflowTests
    {
        [UnityTest] public IEnumerator InsertionsReordersAndRemovalsMoveSurvivorsWithoutChangingTheScreenAnchor()
        {
            var canvas = new GameObject("Reflow canvas", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var root = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"), canvas.transform, false);
            try
            {
                var view = root.GetComponent<NotificationListView>();
                var settings = NotificationPresentationSettings.Default;
                settings.show = settings.hide = NotificationTransition.None; settings.reflowSeconds = .4f;
                view.ConfigurePresentation(settings);
                using var store = new NotificationStore(definitions: new RegisteredTestDefinitions("first", "last", "top", "instant", "next")); using var presenter = new NotificationPresenter(store, view);
                presenter.Activate();
                store.ApplyBatch(new[] { NotificationCommand.Activate(Message("first", 20)) }, 0);
                yield return null; Canvas.ForceUpdateCanvases();
                var first = Row(root, "first"); var firstRect = (RectTransform)first.transform;
                var container = (RectTransform)first.transform.parent;
                Assert.That(container.pivot.y, Is.EqualTo(1));
                Vector3 start = firstRect.position;
                store.ApplyBatch(new[] { NotificationCommand.Activate(Message("last", 10)) }, 1);
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector3.Distance(firstRect.position, start), Is.LessThan(.01f), "Appending must not recenter the list.");
                store.ApplyBatch(new[] { NotificationCommand.Activate(Message("top", 30)) }, 2);
                Canvas.ForceUpdateCanvases();
                Assert.That(Vector3.Distance(firstRect.position, start), Is.LessThan(.01f), "Insertion begins at the existing painted position.");
                yield return new WaitForSecondsRealtime(.1f);
                float midway = firstRect.anchoredPosition.y;
                float target = -Row(root, "top").PreferredHeight - 10;
                Assert.That(midway, Is.InRange(target + .1f, -.1f));
                store.ApplyBatch(new[] { NotificationCommand.Resolve("top") }, 3);
                Assert.That(firstRect.anchoredPosition.y, Is.EqualTo(midway).Within(.01f), "Removal during motion must not jump to a stale target.");
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(firstRect.anchoredPosition.y, Is.Zero.Within(.01f));
                Assert.That(first, Is.SameAs(Row(root, "first")), "Reflow keeps item identity.");
                settings.instantLayout = true; view.ConfigurePresentation(settings);
                store.ApplyBatch(new[] { NotificationCommand.Activate(Message("instant", 40)) }, 4);
                Assert.That(firstRect.anchoredPosition.y, Is.EqualTo(-Row(root, "instant").PreferredHeight - 10).Within(.01f));
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        [UnityTest] public IEnumerator ExitingRowKeepsItsSlotThenTheGapClosesSmoothly()
        {
            var root = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"));
            try
            {
                var view = root.GetComponent<NotificationListView>();
                var settings = NotificationPresentationSettings.Default;
                settings.showSeconds = 0; settings.hideSeconds = .2f; settings.reflowSeconds = .4f;
                view.ConfigurePresentation(settings);
                using var store = new NotificationStore(definitions: new RegisteredTestDefinitions("first", "last", "top", "instant", "next")); using var presenter = new NotificationPresenter(store, view);
                presenter.Activate(); store.ApplyBatch(new[] { NotificationCommand.Activate(Message("top", 20)), NotificationCommand.Activate(Message("next", 10)) }, 0);
                yield return null;
                var next = (RectTransform)Row(root, "next").transform; float start = next.anchoredPosition.y;
                store.ApplyBatch(new[] { NotificationCommand.Resolve("top") }, 1);
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(next.anchoredPosition.y, Is.EqualTo(start).Within(.01f));
                Assert.That(Row(root, "top").transform.GetSiblingIndex(), Is.LessThan(next.GetSiblingIndex()));
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(next.anchoredPosition.y, Is.Zero.Within(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static NotificationRowView Row(GameObject root, string id) => root.GetComponentsInChildren<NotificationRowView>().Single(row => row.NotificationId.Value == id);
        private static NotificationDefinition Message(string id, int priority) => new NotificationDefinition(id, NotificationSeverity.Warning, id, "List movement test", priority);
    }
}
