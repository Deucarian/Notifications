using System;
using System.Collections.Generic;
using Deucarian.Diagnostics;
using NUnit.Framework;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationStoreTests
    {
        private readonly List<NotificationStore> stores = new List<NotificationStore>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < stores.Count; i++)
            {
                stores[i]?.Dispose();
            }

            stores.Clear();
        }

        [Test]
        public void NewStoreStartsEmptyAndRegistersSanitizedDiagnostics()
        {
            NotificationStore store = CreateStore();

            Assert.AreEqual(0, store.Snapshot.Count);
            IDiagnosticProvider provider = FindProvider();
            Assert.NotNull(provider);

            DiagnosticReport report = DiagnosticProviderRegistry.BuildReport();
            string json = DiagnosticsJsonExporter.ToJson(report);
            StringAssert.DoesNotContain("Please reconnect", json);
            StringAssert.DoesNotContain("sample.connection.lost", json);
            StringAssert.Contains("active_count", json);
        }

        [Test]
        public void ActivateRepeatedUpdateAndResolveHaveEpisodeSemantics()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);
            NotificationDefinition warning = Warning("one", "Body");

            store.ApplyBatch(new[] { NotificationCommand.Activate(warning) }, 1d);
            long firstEpisode = store.Snapshot[0].Episode;
            store.ApplyBatch(new[] { NotificationCommand.Activate(warning) }, 2d);

            Assert.AreEqual(1, store.Snapshot.Count);
            Assert.AreEqual(firstEpisode, store.Snapshot[0].Episode);
            Assert.AreEqual(1, sink.Requests.Count);

            store.ApplyBatch(new[] { NotificationCommand.Resolve(warning.Id) }, 3d);
            Assert.AreEqual(0, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Requests.Count);

            store.ApplyBatch(new[] { NotificationCommand.Activate(warning) }, 4d);
            Assert.Greater(store.Snapshot[0].Episode, firstEpisode);
            Assert.AreEqual(2, sink.Requests.Count);
        }

        [Test]
        public void OneHundredRepeatedActiveUpdatesRemainOneEpisodeAndOneFeedback()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);
            NotificationDefinition warning = Warning("repeat", "Body");

            for (int i = 0; i < 100; i++)
            {
                store.ApplyBatch(
                    new[] { NotificationCommand.Activate(warning) },
                    i);
            }

            Assert.AreEqual(1, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Requests.Count);
        }

        [Test]
        public void FeedbackSelectionUsesSeverityThenPriorityThenStableId()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);
            NotificationDefinition warning = new NotificationDefinition(
                "sample.z-warning",
                NotificationSeverity.Warning,
                "Warning",
                "Body",
                1000,
                "feedback.warning");
            NotificationDefinition errorZ = new NotificationDefinition(
                "sample.z-error",
                NotificationSeverity.Error,
                "Error Z",
                "Body",
                50,
                "feedback.error.z");
            NotificationDefinition errorA = new NotificationDefinition(
                "sample.a-error",
                NotificationSeverity.Error,
                "Error A",
                "Body",
                50,
                "feedback.error.a");

            store.ApplyBatch(new[]
            {
                NotificationCommand.Activate(warning),
                NotificationCommand.Activate(errorZ),
                NotificationCommand.Activate(errorA)
            }, 1d);

            Assert.AreEqual(1, sink.Requests.Count);
            Assert.AreEqual("feedback.error.a", sink.Requests[0].RoleId);
        }

        [Test]
        public void IdenticalBodiesRemainIndependentAndOrderedByPriority()
        {
            NotificationStore store = CreateStore();
            NotificationDefinition lower = Warning("fix", "Please walk to an open area", 90);
            NotificationDefinition higher = Warning("gnss", "Please walk to an open area", 100);

            store.ApplyBatch(
                new[]
                {
                    NotificationCommand.Activate(lower),
                    NotificationCommand.Activate(higher)
                },
                1d);

            Assert.AreEqual(2, store.Snapshot.Count);
            Assert.AreEqual(higher.Id, store.Snapshot[0].Id);
            Assert.AreEqual(lower.Id, store.Snapshot[1].Id);

            store.ApplyBatch(new[] { NotificationCommand.Resolve(lower.Id) }, 2d);
            Assert.AreEqual(1, store.Snapshot.Count);
            Assert.AreEqual(higher.Id, store.Snapshot[0].Id);
        }

        [Test]
        public void ThreeActivationsInOneBatchRequestOneHighestPriorityFeedback()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);

            store.ApplyBatch(
                new[]
                {
                    NotificationCommand.Activate(Warning("imu", "IMU", 80)),
                    NotificationCommand.Activate(Warning("fix", "Fix", 90)),
                    NotificationCommand.Activate(Warning("gnss", "GNSS", 100))
                },
                1d);

            Assert.AreEqual(3, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Requests.Count);
            Assert.AreEqual(3, sink.Requests[0].ActivatedCount);
            Assert.AreEqual(100, sink.Requests[0].Priority);
        }

        [Test]
        public void LastCommandForIdWinsWithoutTransientActivationFeedback()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);
            NotificationDefinition warning = Warning("one", "Body");

            store.ApplyBatch(
                new[]
                {
                    NotificationCommand.Activate(warning),
                    NotificationCommand.Resolve(warning.Id)
                },
                1d);

            Assert.AreEqual(0, store.Snapshot.Count);
            Assert.AreEqual(0, sink.Requests.Count);
        }

        [Test]
        public void ContentRefreshAndPresenterRecreationDoNotRequestFeedback()
        {
            RecordingFeedbackSink sink = new RecordingFeedbackSink();
            NotificationStore store = CreateStore(sink);
            FakeListView firstView = new FakeListView();
            NotificationPresenter firstPresenter = new NotificationPresenter(store, firstView);
            firstPresenter.Activate();

            NotificationDefinition initial = Warning("one", "Initial");
            store.ApplyBatch(new[] { NotificationCommand.Activate(initial) }, 1d);
            NotificationDefinition updated = Warning("one", "Localized");
            store.ApplyBatch(new[] { NotificationCommand.Activate(updated) }, 2d);
            firstPresenter.Dispose();

            FakeListView secondView = new FakeListView();
            using (NotificationPresenter secondPresenter = new NotificationPresenter(store, secondView))
            {
                secondPresenter.Activate();
            }

            Assert.AreEqual(1, sink.Requests.Count);
            Assert.AreEqual("Localized", secondView.Last[0].Definition.Body);
        }

        [Test]
        public void ThrowingFeedbackSinkCannotBlockVisualState()
        {
            NotificationStore store = CreateStore(new ThrowingFeedbackSink());

            Assert.DoesNotThrow(() => store.ApplyBatch(
                new[] { NotificationCommand.Activate(Warning("one", "Body")) },
                1d));
            Assert.AreEqual(1, store.Snapshot.Count);
        }

        [Test]
        public void DisposeUnregistersProviderAndRejectsFurtherCommands()
        {
            NotificationStore store = CreateStore();
            IDiagnosticProvider provider = FindProvider();
            Assert.NotNull(provider);

            store.Dispose();

            IReadOnlyList<IDiagnosticProvider> remaining =
                DiagnosticProviderRegistry.SnapshotProviders();
            for (int i = 0; i < remaining.Count; i++)
            {
                Assert.AreNotSame(provider, remaining[i]);
            }
            Assert.Throws<ObjectDisposedException>(() => store.ApplyBatch(
                new[] { NotificationCommand.Activate(Warning("one", "Body")) },
                1d));
        }

        private NotificationStore CreateStore(INotificationFeedbackSink sink = null)
        {
            NotificationStore store = new NotificationStore(sink, definitions: new RegisteredTestDefinitions("sample.one", "sample.repeat", "sample.z-warning", "sample.z-error", "sample.a-error", "sample.fix", "sample.gnss", "sample.imu"));
            stores.Add(store);
            return store;
        }

        private static NotificationDefinition Warning(string suffix, string body, int priority = 10)
        {
            return new NotificationDefinition(
                "sample." + suffix,
                NotificationSeverity.Warning,
                suffix,
                body,
                priority,
                "deucarian.feedback.audio.warning");
        }

        private static IDiagnosticProvider FindProvider()
        {
            IReadOnlyList<IDiagnosticProvider> providers = DiagnosticProviderRegistry.SnapshotProviders();
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                if (providers[i].ProviderId.StartsWith("notifications.", StringComparison.Ordinal))
                {
                    return providers[i];
                }
            }

            return null;
        }

        private sealed class RecordingFeedbackSink : INotificationFeedbackSink
        {
            public List<NotificationFeedbackRequest> Requests { get; } =
                new List<NotificationFeedbackRequest>();

            public bool TryRequestFeedback(NotificationFeedbackRequest request)
            {
                Requests.Add(request);
                return true;
            }
        }

        private sealed class ThrowingFeedbackSink : INotificationFeedbackSink
        {
            public bool TryRequestFeedback(NotificationFeedbackRequest request)
            {
                throw new InvalidOperationException("Expected test failure");
            }
        }

        private sealed class FakeListView : INotificationListView
        {
            public NotificationSnapshot Last { get; private set; } = NotificationSnapshot.Empty;

            public void Render(NotificationSnapshot snapshot)
            {
                Last = snapshot;
            }
        }
    }
}
