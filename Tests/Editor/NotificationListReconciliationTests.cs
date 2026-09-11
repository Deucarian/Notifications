using System;
using System.Linq;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationListReconciliationTests
    {
        [Test]
        public void PriorityReorderRefreshesEveryRetainedRowBeforeSortingTheLayout()
        {
            using (var fixture = new ListFixture())
            {
                fixture.Store.ApplyBatch(new[] { Activate("a", 300), Activate("b", 200), Activate("c", 100) }, 0);
                var original = fixture.Rows.ToDictionary(row => row.NotificationId.Value);
                fixture.Store.ApplyBatch(new[] {
                    Activate("a", 100, "updated a"), Activate("b", 200, "updated b"), Activate("c", 300, "updated c")
                }, 1);

                CollectionAssert.AreEqual(new[] { "c", "b", "a" }, fixture.Rows.Select(row => row.NotificationId.Value));
                foreach (var row in fixture.Rows)
                {
                    string id = row.NotificationId.Value;
                    Assert.AreSame(original[id], row, "A priority change must retain the row instance.");
                    Assert.That(row.transform.Find("Title").GetComponent<TMP_Text>().text, Is.EqualTo("updated " + id));
                }
            }
        }

        [Test]
        public void ResolvingLeadingRowsRetiresAllStaleRowsAndFreesCapacityForTheNextBatch()
        {
            using (var fixture = new ListFixture())
            {
                fixture.Store.ApplyBatch(new[] { Activate("a", 300), Activate("b", 200), Activate("c", 100) }, 0);
                fixture.Store.ApplyBatch(new[] { NotificationCommand.Resolve("a"), NotificationCommand.Resolve("b") }, 1);
                Assert.That(fixture.View.RenderedRowCount, Is.EqualTo(1));
                CollectionAssert.AreEqual(new[] { "c" }, fixture.Rows.Select(row => row.NotificationId.Value));

                fixture.Store.ApplyBatch(new[] { Activate("d", 300), Activate("e", 200) }, 2);
                Assert.That(fixture.View.RenderedRowCount, Is.EqualTo(3));
                CollectionAssert.AreEqual(new[] { "d", "e", "c" }, fixture.Rows.Select(row => row.NotificationId.Value));
                Assert.That(fixture.Rows.All(row => row.gameObject.activeSelf), Is.True);
            }
        }

        private static NotificationCommand Activate(string id, int priority, string title = null) =>
            NotificationCommand.Activate(new NotificationDefinition(id, NotificationSeverity.Warning, title ?? id, "Body", priority));

        private sealed class ListFixture : IDisposable
        {
            private readonly GameObject instance;
            private readonly NotificationPresenter presenter;
            internal readonly NotificationStore Store = new NotificationStore();
            internal readonly NotificationListView View;
            internal NotificationRowView[] Rows => instance.GetComponentsInChildren<NotificationRowView>()
                .OrderBy(row => row.transform.GetSiblingIndex()).ToArray();

            internal ListFixture()
            {
                instance = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"));
                View = instance.GetComponent<NotificationListView>();
                var settings = NotificationPresentationSettings.Default;
                settings.maxVisible = 3;
                settings.show = settings.hide = NotificationTransition.None;
                View.ConfigurePresentation(settings);
                presenter = new NotificationPresenter(Store, View);
                presenter.Activate();
            }
            public void Dispose() { presenter.Dispose(); Store.Dispose(); Object.DestroyImmediate(instance); }
        }
    }
}
