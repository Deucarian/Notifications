using System;
using NUnit.Framework;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationServiceTests
    {
        [Test]
        public void OneLineCallsUpdateResolveAndStartNewEpisodes()
        {
            var feedback = new Feedback();
            using (var service = new NotificationService(feedback: feedback, definitions: new RegisteredTestDefinitions("connection.lost")))
            using (NotificationManager.Bind(service))
            {
                NotificationManager.Warn(new NotificationServiceTestsKey("connection.lost"), "Lost", "Reconnect");
                long episode = service.Snapshot[0].Episode;
                NotificationManager.Warn(new NotificationServiceTestsKey("connection.lost"), "Lost", "Try again");
                Assert.That(service.Snapshot.Count, Is.EqualTo(1));
                Assert.That(service.Snapshot[0].Definition.Body, Is.EqualTo("Try again"));
                Assert.That(feedback.Count, Is.EqualTo(1));
                NotificationManager.Resolve(new NotificationServiceTestsKey("connection.lost"));
                service.Tick();
                Assert.That(service.Snapshot.Count, Is.Zero);
                NotificationManager.Warn(new NotificationServiceTestsKey("connection.lost"), "Lost", "Reconnect");
                Assert.That(service.Snapshot[0].Episode, Is.GreaterThan(episode));
                Assert.That(feedback.Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void ExpiryAndRegistrationCleanupDoNotDisposeBorrowedServices()
        {
            var clock = new Clock();
            using (var service = new NotificationService(clock, definitions: new RegisteredTestDefinitions("timed", "next")))
            {
                var registration = NotificationManager.Bind(service);
                Assert.Throws<InvalidOperationException>(() => NotificationManager.Bind(service));
                service.Show(new NotificationDefinition("timed", NotificationSeverity.Info, "Hello", "World", lifetime: NotificationLifetime.Timed(2)));
                clock.NowSeconds = 2;
                service.Tick();
                Assert.That(service.Snapshot.Count, Is.Zero);
                registration.Dispose();
                using (NotificationManager.Bind(service))
                {
                    registration.Dispose();
                    NotificationManager.Warn(new NotificationServiceTestsKey("next"), "Hello", "World");
                }
                service.Resolve(new NotificationServiceTestsKey("next"));
                Assert.That(NotificationManager.IsConfigured, Is.False);
            }
        }
        [Test]
        public void RepeatedKeyedConditionsKeepOneEpisodeAndOneFeedbackRequest()
        {
            var clock = new Clock();
            var feedback = new Feedback();
            var key = new NotificationServiceTestsKey("imu.lost");
            var timing = new NotificationTimingPolicy(0.75, 1);
            using var service = new NotificationService(clock, feedback, definitions: new RegisteredTestDefinitions(key.Id));
            using var registration = NotificationManager.Bind(service);
            for (int i = 0; i < 10; i++)
                NotificationManager.EvaluateBatch(new[] { new NotificationCondition(key, true, timing) });
            clock.NowSeconds = 0.75;
            service.Tick();
            long episode = service.Snapshot[0].Episode;
            for (int i = 0; i < 10; i++)
                NotificationManager.EvaluateBatch(new[] { new NotificationCondition(key, true, timing) });
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
            Assert.That(service.Snapshot[0].Episode, Is.EqualTo(episode));
            Assert.That(feedback.Count, Is.EqualTo(1));
            service.EvaluateBatch(new[] { new NotificationCondition(key, false, timing) });
            clock.NowSeconds = 1.5;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
            clock.NowSeconds = 1.75;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.Zero);
            service.EvaluateBatch(new[] { new NotificationCondition(key, true, timing) });
            clock.NowSeconds = 2.5;
            service.Tick();
            Assert.That(service.Snapshot[0].Episode, Is.GreaterThan(episode));
            Assert.That(feedback.Count, Is.EqualTo(2));
        }

        [Test]
        public void CombinedConditionsPublishAtomicallyAndUnknownKeysDoNotPartiallyApply()
        {
            var fix = new NotificationServiceTestsKey("fix.lost");
            var imu = new NotificationServiceTestsKey("imu.lost");
            var unknown = new NotificationServiceTestsKey("unknown");
            var feedback = new Feedback();
            using var service = new NotificationService(feedback: feedback, definitions: new RegisteredTestDefinitions(fix.Id, imu.Id));
            int changes = 0;
            service.Source.SnapshotChanged += (_, __) => changes++;
            Assert.Throws<InvalidOperationException>(() => service.EvaluateBatch(new[]
            { new NotificationCondition(fix, true), new NotificationCondition(unknown, true) }));
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.Zero);
            Assert.That(changes, Is.Zero);
            Assert.That(feedback.Count, Is.Zero);
            service.EvaluateBatch(new[] { new NotificationCondition(fix, true), new NotificationCondition(imu, true) });
            Assert.That(service.Snapshot.Count, Is.EqualTo(2));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(feedback.Count, Is.EqualTo(1));
        }

        [Test]
        public void ResolvingSuppressedConditionsCancelsPendingActivationAndPreservesOtherMessages()
        {
            var clock = new Clock();
            var imu = new NotificationServiceTestsKey("imu.lost");
            var other = new NotificationServiceTestsKey("other");
            using var service = new NotificationService(clock, definitions: new RegisteredTestDefinitions(imu.Id, other.Id));
            service.Show(other);
            service.EvaluateBatch(new[] { new NotificationCondition(imu, true, new NotificationTimingPolicy(1, 1)) });
            service.Resolve(imu);
            clock.NowSeconds = 10;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
            Assert.That(service.Snapshot[0].Definition.Id.Value, Is.EqualTo(other.Id));
        }

        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class Feedback : INotificationFeedbackSink
        { public int Count; public bool TryRequestFeedback(NotificationFeedbackRequest request) { Count++; return true; } }
    }
}
