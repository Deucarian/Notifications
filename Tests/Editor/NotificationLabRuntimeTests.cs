using System;
using System.Collections;
using System.Linq;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabRuntimeTests
    {
        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class View : INotificationListView, INotificationPresentationTarget
        {
            public NotificationSnapshot Snapshot = NotificationSnapshot.Empty;
            public void Render(NotificationSnapshot snapshot) => Snapshot = snapshot;
            public NotificationPresentationSettings Presentation { get; private set; } = NotificationPresentationSettings.Default;
            public void ConfigurePresentation(NotificationPresentationSettings settings) => Presentation = settings.Sanitized();
        }
        private sealed class Feedback : INotificationFeedbackSink
        {
            public int Count;
            public int LastBatch;
            public bool TryRequestFeedback(NotificationFeedbackRequest request)
            {
                Count++;
                LastBatch = request.ActivatedCount;
                return true;
            }
        }

        [Test]
        public void DiscoveryFollowsPresenterLifecycleAndStoreDisposal()
        {
            using (var store = new NotificationStore())
            using (var presenter = new NotificationPresenter(store, new View()))
            {
                Assert.IsFalse(NotificationEditorTargets.Capture().Any(x => x.Store == store));
                presenter.Activate();
                presenter.Activate();
                Assert.AreEqual(1, NotificationEditorTargets.Capture().Count(x => x.Store == store));
                var target = Target(store);
                presenter.Deactivate();
                Assert.IsFalse(target.IsAvailable);
                Assert.IsFalse(NotificationEditorTargets.Capture().Any(x => x.Store == store));
                presenter.Activate();
                target = Target(store);
                store.Dispose();
                Assert.IsFalse(target.IsAvailable);
                Assert.IsFalse(NotificationEditorTargets.Capture().Any(x => x.Store == store));
            }
        }

        [Test]
        public void RuntimeUsesExistingViewAndAudioWithoutReplacingRealMessages()
        {
            var feedback = new Feedback();
            var view = new View();
            using (var host = new NotificationStore(feedback))
            using (var presenter = new NotificationPresenter(host, view))
            using (var session = new NotificationLabSession(new Clock(), null))
            {
                presenter.Activate();
                host.ApplyBatch(new[] { NotificationCommand.Activate(Message("lab.custom", "Real warning")) }, 0);
                using (var connection = new NotificationLabRuntimeConnection(session.Store, Target(host), new Clock()))
                {
                    Assert.AreEqual(1, feedback.Count, "Selecting a destination does not ping.");
                    session.ShowBatch(new[] { Message("lab.custom", "Test one"), Message("another", "Test two") }, Immediate());
                    Assert.AreEqual(3, view.Snapshot.Count);
                    Assert.AreEqual("Real warning", host.Snapshot.Items.Single(x => x.Id.Value == "lab.custom").Definition.Title);
                    Assert.AreEqual(2, feedback.Count);
                    Assert.AreEqual(2, feedback.LastBatch);
                    for (int i = 0; i < 10; i++) session.Show(Message("lab.custom", "Updated test"), Immediate());
                    Assert.AreEqual(3, view.Snapshot.Count);
                    Assert.AreEqual(2, feedback.Count, "Updating a test episode must not ping again.");
                    session.ResolveAll();
                    Assert.AreEqual(1, view.Snapshot.Count);
                    Assert.AreEqual("Real warning", view.Snapshot[0].Definition.Title);
                }
                Assert.AreEqual(1, host.Snapshot.Count);
            }
        }

        [Test]
        public void DelaysAndResetAffectOnlyInjectedMessages()
        {
            var clock = new Clock();
            using (var host = new NotificationStore())
            using (var presenter = new NotificationPresenter(host, new View()))
            using (var session = new NotificationLabSession(clock, null))
            {
                presenter.Activate();
                host.ApplyBatch(new[] { NotificationCommand.Activate(Message("real", "Real")) }, 0);
                using (var connection = new NotificationLabRuntimeConnection(session.Store, Target(host), clock))
                {
                    session.Show(Message("test", "Delayed"), new NotificationTimingPolicy(2, 1));
                    Assert.AreEqual(1, host.Snapshot.Count);
                    clock.NowSeconds = 2;
                    session.Tick();
                    Assert.AreEqual(2, host.Snapshot.Count);
                    session.Resolve(new NotificationId("test"));
                    clock.NowSeconds = 2.9;
                    session.Tick();
                    Assert.AreEqual(2, host.Snapshot.Count);
                    clock.NowSeconds = 3;
                    session.Tick();
                    Assert.AreEqual(1, host.Snapshot.Count);
                    session.Show(Message("test", "Immediate"), Immediate());
                    session.Reset();
                    Assert.AreEqual(1, host.Snapshot.Count);
                    Assert.AreEqual("real", host.Snapshot[0].Id.Value);
                }
            }
        }

        [Test]
        public void IndependentLabsCannotResolveEachOthersMessages()
        {
            using (var host = new NotificationStore())
            using (var presenter = new NotificationPresenter(host, new View()))
            using (var first = new NotificationLabSession(new Clock(), null))
            using (var second = new NotificationLabSession(new Clock(), null))
            {
                presenter.Activate();
                using (var a = new NotificationLabRuntimeConnection(first.Store, Target(host), new Clock()))
                using (var b = new NotificationLabRuntimeConnection(second.Store, Target(host), new Clock()))
                {
                    first.Show(Message("same", "First lab"), Immediate());
                    second.Show(Message("same", "Second lab"), Immediate());
                    Assert.AreEqual(2, host.Snapshot.Count);
                    a.Dispose();
                    a.Dispose();
                    Assert.AreEqual(1, host.Snapshot.Count);
                    Assert.AreEqual("Second lab", host.Snapshot[0].Definition.Title);
                    first.Show(Message("same", "Detached"), Immediate());
                    Assert.AreEqual(1, host.Snapshot.Count);
                }
                Assert.AreEqual(0, host.Snapshot.Count);
            }
        }

        [Test]
        public void CleanupSurvivesPresenterOrStoreBeingDestroyedFirst()
        {
            using (var host = new NotificationStore())
            using (var presenter = new NotificationPresenter(host, new View()))
            using (var session = new NotificationLabSession(new Clock(), null))
            {
                presenter.Activate();
                var connection = new NotificationLabRuntimeConnection(session.Store, Target(host), new Clock());
                session.Show(Message("test", "Test"), Immediate());
                presenter.Deactivate();
                Assert.IsFalse(connection.IsAvailable);
                connection.Dispose();
                Assert.AreEqual(0, host.Snapshot.Count);
                presenter.Activate();
                session.Reset();
                connection = new NotificationLabRuntimeConnection(session.Store, Target(host), new Clock());
                session.Show(Message("test", "Test"), Immediate());
                host.Dispose();
                Assert.DoesNotThrow(() => session.Reset());
                Assert.DoesNotThrow(() => connection.Dispose());
            }
        }

        [Test]
        public void TimedRuntimeMessagesExpireAndPresentationOverridesRestoreOnDisconnect()
        {
            var clock = new Clock();
            var view = new View();
            var original = view.Presentation;
            using (var host = new NotificationStore())
            using (var presenter = new NotificationPresenter(host, view))
            using (var session = new NotificationLabSession(clock, null))
            {
                presenter.Activate();
                host.ApplyBatch(new[] { NotificationCommand.Activate(Message("real", "Real")) }, 0);
                using (var connection = new NotificationLabRuntimeConnection(session.Store, Target(host), clock))
                {
                    var settings = original;
                    settings.maxVisible = 2;
                    settings.show = NotificationTransition.Slide;
                    connection.ConfigurePresentation(settings);
                    session.Show(new NotificationDefinition("timed", NotificationSeverity.Info, "Timed", "Test",
                        lifetime: NotificationLifetime.Timed(3)), Immediate());
                    Assert.AreEqual(2, host.Snapshot.Count);
                    Assert.AreEqual(NotificationLifetimeKind.Timed,
                        host.Snapshot.Items.Single(x => x.Definition.Title == "Timed").Definition.Lifetime.Kind);
                    Assert.AreEqual(2, view.Presentation.maxVisible, "Changes to messages must not restore the presentation early.");
                    clock.NowSeconds = 3;
                    session.Tick();
                    Assert.AreEqual(1, host.Snapshot.Count);
                    session.Show(Message("remaining", "Remaining test"), Immediate());
                    presenter.Deactivate();
                }
                Assert.AreEqual(1, view.Snapshot.Count, "Cleanup refreshes even a deactivated presenter view.");
                Assert.AreEqual("real", view.Snapshot[0].Id.Value);
                Assert.AreEqual(original.maxVisible, view.Presentation.maxVisible);
                Assert.AreEqual(original.show, view.Presentation.show);
            }
        }

        [Test]
        public void NonemptySessionCannotAutoplayWhenConnecting()
        {
            var feedback = new Feedback();
            using (var host = new NotificationStore(feedback))
            using (var presenter = new NotificationPresenter(host, new View()))
            using (var session = new NotificationLabSession(new Clock(), null))
            {
                presenter.Activate();
                session.Show(Message("test", "Test"), Immediate());
                Assert.Throws<ArgumentException>(() => new NotificationLabRuntimeConnection(session.Store, Target(host), new Clock()));
                Assert.AreEqual(0, host.Snapshot.Count);
                Assert.AreEqual(0, feedback.Count);
            }
        }

        [TestCase(PlayModeStateChange.ExitingPlayMode)]
        [TestCase(PlayModeStateChange.ExitingEditMode)]
        public void WindowTransitionsRemoveOnlyLabMessages(PlayModeStateChange transition)
        {
            using (var host = new NotificationStore())
            using (var presenter = new NotificationPresenter(host, new View()))
            {
                presenter.Activate();
                host.ApplyBatch(new[] { NotificationCommand.Activate(Message("real", "Real")) }, 0);
                var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
                try
                {
                    window.SelectRuntimeTargetForTests(Target(host));
                    window.AddCustomForTests();
                    window.AddCustomForTests();
                    window.UpdateCustomForTests();
                    Assert.AreEqual(3, host.Snapshot.Count);
                    Assert.AreEqual(2, window.SessionForTests.PingCount);
                    window.TransitionForTests(transition);
                    Assert.AreEqual(1, host.Snapshot.Count);
                    Assert.AreEqual("real", host.Snapshot[0].Id.Value);
                }
                finally { UnityEngine.Object.DestroyImmediate(window); }
            }
        }

        [Test]
        public void WindowChangingTargetsAndClosingCleanUpWithoutAutoplay()
        {
            var feedback = new Feedback();
            using (var first = new NotificationStore(feedback))
            using (var second = new NotificationStore(feedback))
            using (var a = new NotificationPresenter(first, new View()))
            using (var b = new NotificationPresenter(second, new View()))
            {
                a.Activate();
                b.Activate();
                var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
                try
                {
                    window.SelectRuntimeTargetForTests(Target(first));
                    window.AddCustomForTests();
                    window.SelectRuntimeTargetForTests(Target(second));
                    Assert.AreEqual(0, first.Snapshot.Count);
                    Assert.AreEqual(0, second.Snapshot.Count);
                    Assert.AreEqual(1, feedback.Count);
                    window.AddCustomForTests();
                    window.DisableForTests();
                    Assert.AreEqual(0, second.Snapshot.Count);
                    Assert.AreEqual(2, feedback.Count);
                }
                finally { UnityEngine.Object.DestroyImmediate(window); }
            }
        }

        private static NotificationEditorTarget Target(NotificationStore store) =>
            NotificationEditorTargets.Capture().Single(x => ReferenceEquals(x.Store, store));
        private static NotificationTimingPolicy Immediate() => new NotificationTimingPolicy(0, 0);
        private static NotificationDefinition Message(string id, string title) =>
            new NotificationDefinition(id, NotificationSeverity.Warning, title, "Test body", 20,
                "deucarian.feedback.audio.warning");

        [UnityTest]
        public IEnumerator RunningWindowAddsStacksAndRemovesMessagesInTheRealSceneView()
        {
            yield return new EnterPlayMode();
            GameObject prefab = Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList");
            Assert.NotNull(prefab);
            GameObject root = UnityEngine.Object.Instantiate(prefab);
            var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
            var feedback = new Feedback();
            var view = root.GetComponent<NotificationListView>();
            try
            {
                using (var host = new NotificationStore(feedback))
                using (var presenter = new NotificationPresenter(host, view))
                {
                    presenter.Activate();
                    var target = Target(host);
                    Assert.IsTrue(DeucarianNotificationLabWindow.CanTargetForTests(target));
                    window.SelectRuntimeTargetForTests(target);
                    window.AddCustomForTests();
                    window.AddCustomForTests();
                    window.TickForTests();
                    yield return null;
                    Assert.AreEqual(2, view.VisibleCount);
                    Assert.AreEqual(2, feedback.Count);
                    window.UpdateCustomForTests();
                    yield return null;
                    Assert.AreEqual(2, view.VisibleCount);
                    Assert.AreEqual(2, feedback.Count);
                    window.SessionForTests.Reset();
                    yield return null;
                    Assert.AreEqual(0, view.VisibleCount);
                    window.AddCustomForTests();
                    presenter.Deactivate();
                    window.TickForTests();
                    Assert.AreEqual(0, host.Snapshot.Count, "Losing the runtime destination removes its injected messages.");
                    Assert.AreEqual(0, window.SessionForTests.Store.Snapshot.Count);
                    Assert.AreEqual(0, window.SessionForTests.PendingCount);
                    Assert.AreEqual(0, view.RenderedRowCount, "No injected rows survive disconnect during an exit animation.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(root);
            }
            yield return new ExitPlayMode();
        }
    }
}
