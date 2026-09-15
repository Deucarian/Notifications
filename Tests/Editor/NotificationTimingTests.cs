using System.Collections.Generic;
using NUnit.Framework;
using Deucarian.Diagnostics;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationTimingTests
    {
        private NotificationStore store;
        private FakeClock clock;
        private NotificationEpisodeController controller;
        private RecordingFeedbackSink sink;

        [SetUp]
        public void SetUp()
        {
            sink = new RecordingFeedbackSink();
            store = new NotificationStore(sink, definitions: new RegisteredTestDefinitions("sample.warning", "sample.first", "sample.second"));
            clock = new FakeClock();
            controller = new NotificationEpisodeController(store, clock);
        }

        [TearDown]
        public void TearDown()
        {
            controller.Dispose();
            store.Dispose();
        }

        [Test]
        public void ActivationAndRecoveryRequireTheirDebounceDeadlines()
        {
            Evaluate(true);
            clock.NowSeconds = 0.74d;
            controller.Tick();
            Assert.AreEqual(0, store.Snapshot.Count);

            clock.NowSeconds = 0.75d;
            controller.Tick();
            Assert.AreEqual(1, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Count);

            Evaluate(false);
            clock.NowSeconds = 1.74d;
            controller.Tick();
            Assert.AreEqual(1, store.Snapshot.Count);

            clock.NowSeconds = 1.75d;
            controller.Tick();
            Assert.AreEqual(0, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Count);
        }

        [Test]
        public void RelapseDuringPendingRecoveryKeepsSameEpisodeAndDoesNotPing()
        {
            Evaluate(true);
            clock.NowSeconds = 0.75d;
            controller.Tick();
            long episode = store.Snapshot[0].Episode;

            clock.NowSeconds = 1d;
            Evaluate(false);
            clock.NowSeconds = 1.5d;
            Evaluate(true);
            clock.NowSeconds = 3d;
            controller.Tick();

            Assert.AreEqual(1, store.Snapshot.Count);
            Assert.AreEqual(episode, store.Snapshot[0].Episode);
            Assert.AreEqual(1, sink.Count);
        }

        [Test]
        public void MultipleDueConditionsAreAppliedAsOneFeedbackBatch()
        {
            NotificationDefinition first = CreateDefinition("first", 100);
            NotificationDefinition second = CreateDefinition("second", 90);
            NotificationTimingPolicy timing = new NotificationTimingPolicy(0.75d, 1d);

            controller.EvaluateBatch(new[]
            {
                new NotificationConditionSample(first, true, timing),
                new NotificationConditionSample(second, true, timing)
            });
            clock.NowSeconds = 0.75d;
            controller.Tick();

            Assert.AreEqual(2, store.Snapshot.Count);
            Assert.AreEqual(1, sink.Count);
        }

        [Test]
        public void SchedulerDiagnosticsExposeOnlySanitizedPendingCounts()
        {
            Evaluate(true);

            string json = DiagnosticsJsonExporter.ToJson(
                DiagnosticProviderRegistry.BuildReport());
            StringAssert.Contains("pending_activation_count", json);
            StringAssert.Contains("pending_recovery_count", json);
            StringAssert.Contains("scheduler_state", json);
            StringAssert.DoesNotContain("sample.warning", json);
            StringAssert.DoesNotContain("Body", json);
        }

        private void Evaluate(bool unhealthy)
        {
            controller.EvaluateBatch(new[]
            {
                new NotificationConditionSample(
                    CreateDefinition("warning", 100),
                    unhealthy,
                    new NotificationTimingPolicy(0.75d, 1d))
            });
        }

        private static NotificationDefinition CreateDefinition(string suffix, int priority)
        {
            return new NotificationDefinition(
                "sample." + suffix,
                NotificationSeverity.Warning,
                suffix,
                "Body",
                priority,
                "deucarian.feedback.audio.warning");
        }

        private sealed class FakeClock : INotificationClock
        {
            public double NowSeconds { get; set; }
        }

        private sealed class RecordingFeedbackSink : INotificationFeedbackSink
        {
            public int Count { get; private set; }

            public bool TryRequestFeedback(NotificationFeedbackRequest request)
            {
                Count++;
                return true;
            }
        }
    }
}
