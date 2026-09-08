using System;
using Deucarian.Diagnostics;
using NUnit.Framework;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationPresenterPortTests
    {
        [Test]
        public void DiagnosticObserverOnlyNeedsAnImmutableSnapshotFunction()
        {
            int captures = 0;
            var provider = new NotificationDiagnosticProvider("notification.read-only-test", () =>
            {
                captures++;
                return new NotificationDiagnosticState(2, NotificationSeverity.Warning, 3, 1, 0, false);
            });
            using (DiagnosticProviderRegistry.Register(provider))
            {
                DiagnosticProviderRegistry.BuildReport();
                Assert.That(captures, Is.EqualTo(1));
            }
        }

        [Test]
        public void PresenterAcceptsAReadOnlySourceWithoutMutationOrDiagnosticsDependencies()
        {
            var source = new ReadOnlySource();
            var view = new View();
            using (var presenter = new NotificationPresenter(source, view))
            {
                presenter.Activate();
                Assert.That(view.RenderCount, Is.EqualTo(1));
                Assert.That(source.Subscribers, Is.EqualTo(1));
            }
            Assert.That(source.Subscribers, Is.Zero);
        }

        [Test]
        public void FailedInitialRenderReleasesSubscriptionAndAllowsRetry()
        {
            var source = new ReadOnlySource();
            var view = new View { Fail = true };
            using (var presenter = new NotificationPresenter(source, view))
            {
                Assert.Throws<InvalidOperationException>(presenter.Activate);
                Assert.That(source.Subscribers, Is.Zero);
                view.Fail = false;
                presenter.Activate();
                Assert.That(source.Subscribers, Is.EqualTo(1));
            }
        }

        private sealed class ReadOnlySource : INotificationSource
        {
            public NotificationSnapshot Snapshot => NotificationSnapshot.Empty;
            public int Subscribers;
            public event EventHandler<NotificationChangedEventArgs> SnapshotChanged
            {
                add => Subscribers++;
                remove => Subscribers--;
            }
        }

        private sealed class View : INotificationListView
        {
            public bool Fail;
            public int RenderCount;
            public void Render(NotificationSnapshot snapshot)
            {
                if (Fail) throw new InvalidOperationException("Test render failed");
                RenderCount++;
            }
        }
    }
}
