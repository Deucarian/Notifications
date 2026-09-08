using System;
using Deucarian.Notifications.Editor;
using NUnit.Framework;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLifetimeTests
    {
        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }

        [Test]
        public void PersistentAndTimedMessagesHaveIndependentLifetimes()
        {
            var clock = new Clock();
            using (var session = new NotificationLabSession(clock, null))
            {
                session.ShowBatch(new[] { Message("persistent"), Message("timed", 5) }, new NotificationTimingPolicy(2, 1));
                clock.NowSeconds = 2;
                session.Tick();
                Assert.AreEqual(2, session.Store.Snapshot.Count);
                clock.NowSeconds = 6.9;
                session.Tick();
                Assert.AreEqual(2, session.Store.Snapshot.Count);
                clock.NowSeconds = 7;
                session.Tick();
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                Assert.AreEqual("persistent", session.Store.Snapshot[0].Id.Value);
                Assert.AreEqual(0, session.PendingCount);
                clock.NowSeconds = 100;
                session.Tick();
                Assert.AreEqual(1, session.Store.Snapshot.Count, "An expired notice must not reactivate on tick.");
                Assert.AreEqual(1, session.PingCount);
                session.Show(Message("timed", 5), new NotificationTimingPolicy(0, 0));
                Assert.AreEqual(2, session.Store.Snapshot.Count, "An explicit new request can restart it.");
                Assert.AreEqual(2, session.PingCount);
            }
        }

        [Test]
        public void UpdateDoesNotRestartTimerAndManualRecoveryCanFinishFirst()
        {
            var clock = new Clock();
            using (var session = new NotificationLabSession(clock, null))
            {
                session.Show(Message("timed", 5), new NotificationTimingPolicy(0, 1));
                clock.NowSeconds = 4;
                session.Show(Message("timed", 5), new NotificationTimingPolicy(0, 1));
                clock.NowSeconds = 5;
                session.Tick();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                Assert.AreEqual(1, session.PingCount);
                session.Show(Message("timed", 10), new NotificationTimingPolicy(0, 1));
                session.Resolve(new NotificationId("timed"));
                clock.NowSeconds = 6;
                session.Tick();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                Assert.AreEqual(0, session.PendingCount);
            }
        }

        [Test]
        public void OverflowIsNotResolutionAndPromotesWithoutAnotherPing()
        {
            using (var session = new NotificationLabSession(new Clock(), null))
            {
                for (int i = 0; i < 8; i++) session.Show(Message("item." + i), new NotificationTimingPolicy(0, 0));
                NotificationSnapshot selected = NotificationVisibility.Select(session.Store.Snapshot, 5);
                Assert.AreEqual(5, selected.Count);
                Assert.AreEqual(8, session.Store.Snapshot.Count);
                Assert.AreEqual("item.0", selected[0].Id.Value);
                session.Resolve(selected[0].Id);
                selected = NotificationVisibility.Select(session.Store.Snapshot, 5);
                Assert.AreEqual("item.5", selected[4].Id.Value);
                Assert.AreEqual(8, session.PingCount);
                session.Show(new NotificationDefinition("critical", NotificationSeverity.Error, "Urgent", "", 100),
                    new NotificationTimingPolicy(0, 0));
                Assert.AreEqual("critical", NotificationVisibility.Select(session.Store.Snapshot, 5)[0].Id.Value);
            }
        }

        [Test]
        public void TimedOverflowExpiresWithoutEverBeingDisplayed()
        {
            var clock = new Clock();
            using (var session = new NotificationLabSession(clock, null))
            {
                session.Show(Message("first"), new NotificationTimingPolicy(0, 0));
                session.Show(Message("overflow", 1), new NotificationTimingPolicy(0, 0));
                Assert.AreEqual("first", NotificationVisibility.Select(session.Store.Snapshot, 1)[0].Id.Value);
                clock.NowSeconds = 1;
                session.Tick();
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                Assert.AreEqual(0, session.PendingCount);
            }
        }

        [Test]
        public void LifetimeValuesAreValidatedAndPartOfDefinitionIdentity()
        {
            foreach (double seconds in new[] { 0, -1, double.NaN, double.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => NotificationLifetime.Timed(seconds));
            Assert.AreEqual(NotificationLifetimeKind.UntilResolved, default(NotificationLifetime).Kind);
            Assert.AreNotEqual(Message("same"), Message("same", 5));
            Assert.AreEqual(Message("same", 5), Message("same", 5));
        }

        private static NotificationDefinition Message(string id, double seconds = 0) => new NotificationDefinition(
            id, NotificationSeverity.Warning, id, "Test", 10, "deucarian.feedback.audio.warning",
            seconds > 0 ? NotificationLifetime.Timed(seconds) : NotificationLifetime.UntilResolved);
    }
}
