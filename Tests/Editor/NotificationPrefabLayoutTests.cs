using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationPrefabLayoutTests
    {
        [Test]
        public void RepairGeneratorMatchesTheBundledNonOverlappingLabelLayout()
        {
            var root = new GameObject("Generated notification layout", typeof(RectTransform));
            try
            {
                NotificationRowView generated = DeucarianNotificationPrefabFactory.CreateRowTemplate(
                    (RectTransform)root.transform, null);
                GameObject prefab = Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList");
                Assert.NotNull(prefab);
                NotificationRowView bundled = prefab.GetComponentInChildren<NotificationRowView>(true);
                AssertLayout(generated);
                AssertLayout(bundled);
                foreach (string label in new[] { "Title", "Body" })
                {
                    var expected = (RectTransform)generated.transform.Find(label);
                    var actual = (RectTransform)bundled.transform.Find(label);
                    Assert.AreEqual(expected.anchoredPosition, actual.anchoredPosition, label);
                    Assert.AreEqual(expected.sizeDelta, actual.sizeDelta, label);
                    Assert.AreEqual(expected.anchorMin, actual.anchorMin, label);
                    Assert.AreEqual(expected.anchorMax, actual.anchorMax, label);
                    Assert.AreEqual(expected.pivot, actual.pivot, label);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AssertLayout(NotificationRowView row)
        {
            var title = (RectTransform)row.transform.Find("Title");
            var body = (RectTransform)row.transform.Find("Body");
            var rect = (RectTransform)row.transform;
            Assert.GreaterOrEqual(-title.anchoredPosition.y, 9, "Top padding");
            Assert.GreaterOrEqual(title.anchoredPosition.y - title.rect.height - body.anchoredPosition.y,
                4, "There must be a gap between the title and instruction.");
            Assert.GreaterOrEqual(rect.rect.height + body.anchoredPosition.y - body.rect.height, 9, "Bottom padding");
            Assert.GreaterOrEqual(title.anchoredPosition.x, 18, "The severity stripe needs its own space.");
            Assert.AreEqual(title.anchoredPosition.x, body.anchoredPosition.x);
        }
    }
}
