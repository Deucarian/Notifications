using System;
using NUnit.Framework;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationDefinitionUsageTests
    {
        private sealed class Key : NotificationKey { internal Key(string id) : base(id) { } }
        private sealed class Definitions : INotificationDefinitions
        {
            internal readonly NotificationDefinition Value = new NotificationDefinition("connection", NotificationSeverity.Warning, "Connection lost", "Reconnect", feedbackRoleId: "deucarian.feedback.audio.warning");
            public bool TryGet(NotificationKey key, out NotificationDefinition definition)
            { definition = key.Id == Value.Id.Value ? Value : null; return definition != null; }
        }
        private sealed class Feedback : INotificationFeedbackSink
        { internal int Count; public bool TryRequestFeedback(NotificationFeedbackRequest value) { Count++; return true; } }

        [Test]
        public void DefaultsOverridesAndRepeatedCallsShareOneEpisodeWithoutChangingDefinition()
        {
            var definitions = new Definitions(); var feedback = new Feedback(); var key = new Key("connection");
            using (var service = new NotificationService(feedback: feedback, definitions: definitions))
            using (NotificationManager.Bind(service))
            {
                NotificationManager.Show(key);
                Assert.That(service.Snapshot[0].Definition.Title, Is.EqualTo("Connection lost"));
                Assert.That(feedback.Count, Is.EqualTo(1));
                NotificationManager.Show(key, message: "Reconnect the helmet");
                Assert.That(service.Snapshot.Count, Is.EqualTo(1));
                Assert.That(service.Snapshot[0].Definition.Body, Is.EqualTo("Reconnect the helmet"));
                Assert.That(definitions.Value.Body, Is.EqualTo("Reconnect"));
                Assert.That(feedback.Count, Is.EqualTo(1));
                NotificationManager.Resolve(new Key("connection"));
                Assert.That(service.Snapshot.Count, Is.Zero);
                NotificationManager.Show(key);
                Assert.That(feedback.Count, Is.EqualTo(2));
                Assert.That(service.Snapshot[0].Definition.Body, Is.EqualTo("Reconnect"));
            }
        }

        [Test]
        public void MissingCatalogHasConcreteRepairGuidanceAndDoesNotActivateAnything()
        {
            using (var service = new NotificationService())
            {
                var error = Assert.Throws<InvalidOperationException>(() => service.Show(new Key("missing")));
                Assert.That(error.Message, Does.Contain("Notification Lab"));
                Assert.That(error.Message, Does.Contain("missing"));
                Assert.That(service.Snapshot.Count, Is.Zero);
            }
        }
    }
}
