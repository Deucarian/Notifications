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
        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class Feedback : INotificationFeedbackSink
        { public int Count; public bool TryRequestFeedback(NotificationFeedbackRequest request) { Count++; return true; } }
    }
}
