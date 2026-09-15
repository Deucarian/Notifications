using System;
using System.Collections.Generic;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationRegistrationTests
    {
        private sealed class Key : NotificationKey { internal Key(string id) : base(id) { } }
        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class Feedback : INotificationFeedbackSink
        {
            internal int Count;
            public bool TryRequestFeedback(NotificationFeedbackRequest request) { Count++; return true; }
        }
        private static NotificationDefinition Message(string id) => new NotificationDefinition(id,
            NotificationSeverity.Warning, "Registered warning", "Body", feedbackRoleId: "warning");

        [Test]
        public void BasicSampleDeclarationsCanBeImportedByTheStrictAuthoringParser()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NotificationStore).Assembly);
            string directory = System.IO.Path.Combine(package.resolvedPath, "Samples~/Notification List Basics/Definitions/Editor");
            var paths = System.IO.Directory.GetFiles(directory, "*.definition.cs");
            Assert.That(paths.Length, Is.EqualTo(10));
            var schema = new Deucarian.Notifications.Editor.Definitions.NotificationDefinitionSchema();
            foreach (var path in paths)
                Assert.DoesNotThrow(() => Deucarian.Editor.Definitions.DeucarianDefinitionSource.Read(schema, System.IO.File.ReadAllText(path)), path);
        }

        [Test]
        public void MissingCatalogRejectsEveryPublicActivationPath()
        {
            using var store = new NotificationStore();
            using var service = new NotificationService();
            var message = Message("unknown");
            Assert.Throws<InvalidOperationException>(() => store.ApplyBatch(new[] { NotificationCommand.Activate(message) }, 0));
            Assert.Throws<InvalidOperationException>(() => service.Show(message));
            Assert.Throws<InvalidOperationException>(() => service.Show(new Key("unknown")));
            Assert.Throws<InvalidOperationException>(() => service.Warn(new Key("unknown"), "Title", "Body"));
            Assert.That(store.Snapshot.Count, Is.Zero);
            Assert.That(service.Snapshot.Count, Is.Zero);
        }

        [Test]
        public void UnknownItemRejectsTheEntireBatchBeforeStateEventsOrFeedback()
        {
            var feedback = new Feedback();
            using var store = new NotificationStore(feedback, new RegisteredTestDefinitions("a.known"));
            int events = 0; store.SnapshotChanged += (_, __) => events++;
            Assert.Throws<InvalidOperationException>(() => store.ApplyBatch(new[] {
                NotificationCommand.Activate(Message("a.known")), NotificationCommand.Activate(Message("z.unknown")) }, 0));
            Assert.That(store.Snapshot.Version, Is.Zero);
            Assert.That(store.Snapshot.Count, Is.Zero);
            Assert.That(feedback.Count, Is.Zero);
            Assert.That(events, Is.Zero);
        }

        [Test]
        public void UnknownDelayedConditionCannotLeaveKnownConditionsScheduled()
        {
            var clock = new Clock();
            using var store = new NotificationStore(definitions: new RegisteredTestDefinitions("known"));
            using var controller = new NotificationEpisodeController(store, clock);
            Assert.Throws<InvalidOperationException>(() => controller.EvaluateBatch(new[] {
                new NotificationConditionSample(Message("known"), true, new NotificationTimingPolicy(1, 0)),
                new NotificationConditionSample(Message("unknown"), true, new NotificationTimingPolicy(1, 0)) }));
            clock.NowSeconds = 2; controller.Tick();
            Assert.That(store.Snapshot.Count, Is.Zero);
        }

        [Test]
        public void LabRegistrationsExpireAndCannotReplaceApplicationDefinitions()
        {
            using var store = new NotificationStore(definitions: new RegisteredTestDefinitions("app"));
            store.ApplyBatch(new[] { NotificationCommand.Activate(Message("app")) }, 0);
            using (var scope = store.CreateEditorScope())
            {
                Assert.Throws<InvalidOperationException>(() => scope.Register(Message("app")));
                scope.Register(Message("lab"));
                store.ApplyBatch(new[] { NotificationCommand.Activate(Message("lab")) }, 0);
                Assert.That(store.Snapshot.Count, Is.EqualTo(2));
            }
            Assert.That(store.Snapshot.Count, Is.EqualTo(1));
            Assert.That(store.Snapshot[0].Id.Value, Is.EqualTo("app"));
            Assert.Throws<InvalidOperationException>(() => store.ApplyBatch(new[] { NotificationCommand.Activate(Message("lab")) }, 0));
        }

        [Test]
        public void CatalogRejectsDuplicateAndMissingAssetsInsteadOfChoosingTheFirst()
        {
            var catalog = ScriptableObject.CreateInstance<NotificationCatalogAsset>();
            var asset = ScriptableObject.CreateInstance<NotificationDefinitionAsset>();
            try
            {
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("id").stringValue = "duplicate";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var data = new SerializedObject(catalog); var entries = data.FindProperty("definitions");
                entries.arraySize = 2;
                entries.GetArrayElementAtIndex(0).objectReferenceValue = asset;
                entries.GetArrayElementAtIndex(1).objectReferenceValue = asset;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => catalog.TryGet(new Key("duplicate"), out _));
                entries.GetArrayElementAtIndex(1).objectReferenceValue = null;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.Throws<InvalidOperationException>(() => catalog.TryGet(new Key("duplicate"), out _));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(asset); }
        }
    }
}
