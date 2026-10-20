using System;
using NUnit.Framework;
using Deucarian.Notifications.Unity;
using UnityEngine;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationManualResolutionTests
    {
        [Test]
        public void ViewCannotDismissAConditionButRecoveryAndProgrammaticResolutionStillWork()
        {
            var clock = new Clock();
            var view = new View();
            var definitions = new Definitions(false);
            using var service = new NotificationService(clock, view: view, definitions: definitions);
            var timing = new NotificationTimingPolicy(0, 1);
            service.EvaluateBatch(new[] { new NotificationCondition(definitions.Key, true, timing) });
            long episode = service.Snapshot[0].Episode;
            view.Resolve(definitions.Value.Id);
            Assert.That(service.Snapshot[0].Episode, Is.EqualTo(episode));
            service.EvaluateBatch(new[] { new NotificationCondition(definitions.Key, false, timing) });
            clock.NowSeconds = .5;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
            clock.NowSeconds = 1;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.Zero);
            service.Show(definitions.Key);
            service.Resolve(definitions.Key);
            Assert.That(service.Snapshot.Count, Is.Zero);
        }

        [Test]
        public void ContentOverridesPreserveManualResolutionPolicy()
        {
            var view = new View();
            var definitions = new Definitions(false);
            using var service = new NotificationService(view: view, definitions: definitions);
            service.Show(definitions.Key, "New title", "New body");
            Assert.That(service.Snapshot[0].Definition.AllowManualResolution, Is.False);
            view.Resolve(definitions.Value.Id);
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
        }

        [Test]
        public void StaleViewCallbackChecksCurrentPolicyAndBecomesHarmlessAfterDisposal()
        {
            var view = new View();
            var definitions = new Definitions(true);
            var service = new NotificationService(view: view, definitions: definitions);
            try
            {
                service.Show(definitions.Key);
                var callback = view.Resolve;
                var manual = service.Snapshot[0].Definition;
                service.Show(new NotificationDefinition(manual.Id, manual.Severity, manual.Title, manual.Body,
                    allowManualResolution: false));
                Assert.That(service.Snapshot[0].Definition, Is.Not.EqualTo(manual));
                callback(manual.Id);
                Assert.That(service.Snapshot.Count, Is.EqualTo(1));
                service.Dispose();
                Assert.That(view.Resolve, Is.Null);
                Assert.DoesNotThrow(() => callback(manual.Id));
            }
            finally { service.Dispose(); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TimedNotificationsExpireWithoutAViewDismissal(bool manual)
        {
            var clock = new Clock();
            var view = new View();
            var definitions = new Definitions(manual, NotificationLifetime.Timed(2));
            using var service = new NotificationService(clock, view: view, definitions: definitions);
            service.Show(definitions.Key);
            view.Resolve(definitions.Value.Id);
            Assert.That(service.Snapshot.Count, Is.EqualTo(1));
            clock.NowSeconds = 2;
            service.Tick();
            Assert.That(service.Snapshot.Count, Is.Zero);
        }

        [Test]
        public void CardUpdatesItsButtonAndRaycastsWhenManualResolutionChanges()
        {
            var instance = UnityEngine.Object.Instantiate(NotificationViewDefaults.LoadListPrefab());
            try
            {
                var view = instance.GetComponent<NotificationListView>();
                var definitions = new Definitions(false);
                using var service = new NotificationService(view: view, definitions: definitions);
                service.Show(definitions.Key);
                var action = view.GetComponentInChildren<NotificationRowAction>();
                Assert.That(action.Button.gameObject.activeSelf, Is.False);
                Assert.That(action.Button.targetGraphic.raycastTarget, Is.False);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                action.Button.onClick.Invoke();
                Assert.That(service.Snapshot.Count, Is.EqualTo(1));
                service.Show(new NotificationDefinition(definitions.Value.Id, NotificationSeverity.Warning, "Warning", "Body"));
                Assert.That(action.Button.gameObject.activeSelf, Is.True);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                service.Show(definitions.Key);
                Assert.That(action.Button.gameObject.activeSelf, Is.False);
                Assert.That(view.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class View : INotificationListView, INotificationResolutionView
        {
            public Action<NotificationId> Resolve;
            public void BindResolution(Action<NotificationId> resolve) => Resolve = resolve;
            public void Render(NotificationSnapshot snapshot) { }
        }
        private sealed class Key : NotificationKey { public Key() : base("manual-policy") { } }
        private sealed class Definitions : INotificationDefinitions
        {
            public readonly NotificationKey Key = new Key();
            public readonly NotificationDefinition Value;
            public Definitions(bool manual, NotificationLifetime lifetime = default) => Value =
                new NotificationDefinition(Key.Id, NotificationSeverity.Warning, "Warning", "Body",
                    lifetime: lifetime, allowManualResolution: manual);
            public bool TryGet(NotificationKey key, out NotificationDefinition definition)
            { definition = key.Id == Key.Id ? Value : null; return definition != null; }
        }
    }
}
