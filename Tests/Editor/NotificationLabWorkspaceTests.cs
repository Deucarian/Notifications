using System.Collections;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabWorkspaceTests
    {
        [UnityTest]
        public IEnumerator EditorControlsDriveTheProductionSessionAndPresentationSettings()
        {
            var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
            var original = window.Inputs;
            try
            {
                window.Inputs = new NotificationLabRecipeData { sound = false, recoveryDelay = 0, lifetimeSeconds = 5 };
                window.Show();
                yield return null;
                yield return null;
                var root = window.rootVisualElement;
                Assert.That(root.Q(className: "deucarian-workspace"), Is.Not.Null);
                root.Q<TextField>("lab-title").value = "Connected UI test";
                Assert.That(window.Inputs.title, Is.EqualTo("Connected UI test"));
                yield return Click(root.Q<Button>("lab-add"));
                Assert.That(window.SessionForTests.Store.Snapshot.Count, Is.EqualTo(1));
                Assert.That(window.SessionForTests.Store.Snapshot[0].Definition.Title, Is.EqualTo("Connected UI test"));
                Assert.That(root.Q("lab-visible-rows").childCount, Is.EqualTo(1));
                root.Q<DeucarianEditorStepper>("lab-maximum").value = 2;
                window.ShowMixed();
                window.TickForTests();
                Assert.That(window.Inputs.presentation.maxVisible, Is.EqualTo(2));
                Assert.That(root.Q("lab-visible-rows").childCount, Is.EqualTo(2));
                Assert.That(root.Q<Foldout>("lab-overflow").contentContainer.childCount, Is.EqualTo(9));
                yield return Click(root.Q<Button>("lab-clear"));
                Assert.That(window.SessionForTests.Store.Snapshot.Count, Is.Zero);
                Assert.That(root.Q("lab-visible-rows").childCount, Is.Zero);
                Assert.That(root.Q<Foldout>("lab-overflow").contentContainer.childCount, Is.Zero);
                root.Q<TextField>("lab-title").value = " ";
                Assert.That(root.Q<Button>("lab-add").enabledInHierarchy, Is.False);
                Assert.That(window.SessionForTests.PingCount, Is.Zero);
                yield return Click(root.Q("workspace-tabs").Q<Button>("choice-1"));
                yield return null;
                var maximum = root.Q<DeucarianEditorStepper>("lab-maximum");
                Assert.That(maximum.parent.Q<Label>().worldBound.xMax, Is.LessThanOrEqualTo(maximum.worldBound.xMin + 1));
            }
            finally { window.Inputs = original; window.Close(); }
        }

        [UnityTest]
        public IEnumerator TimedMessageAddedThroughTheUiExpiresWithoutBeingResolved()
        {
            var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
            var original = window.Inputs;
            try
            {
                window.Inputs = new NotificationLabRecipeData { sound = false, recoveryDelay = 0, lifetime = NotificationLifetimeKind.Timed, lifetimeSeconds = 0.3f };
                window.Show();
                yield return null;
                yield return Click(window.rootVisualElement.Q<Button>("lab-add"));
                Assert.That(window.SessionForTests.Store.Snapshot.Count, Is.EqualTo(1));
                double deadline = EditorApplication.timeSinceStartup + 3;
                while (window.SessionForTests.Store.Snapshot.Count > 0 && EditorApplication.timeSinceStartup < deadline)
                { window.TickForTests(); yield return null; }
                Assert.That(window.SessionForTests.Store.Snapshot.Count, Is.Zero);
                Assert.That(window.rootVisualElement.Q("lab-visible-rows").childCount, Is.Zero);
            }
            finally { window.Inputs = original; window.Close(); }
        }

        [UnityTest]
        public IEnumerator SidebarKeepsMessagesAndDraftWhenReturningToNotifications()
        {
            var window = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
            var original = window.Inputs;
            try
            {
                window.Inputs = new NotificationLabRecipeData { sound = false, recoveryDelay = 0,
                    title = "Keep this warning", lifetime = NotificationLifetimeKind.UntilResolved };
                window.Show();
                yield return null;
                yield return Click(window.rootVisualElement.Q<Button>("lab-add"));
                var session = window.SessionForTests;
                var title = window.rootVisualElement.Q<TextField>("lab-title");
                var bounds = window.position;
                yield return Click(window.rootVisualElement.Q<Button>("workspace-nav-audio"));
                Assert.That(window.rootVisualElement.Q("lab-title"), Is.Null);
                yield return Click(window.rootVisualElement.Q<Button>("workspace-nav-deucarian.notifications.lab"));
                Assert.That(window.rootVisualElement.Q<TextField>("lab-title"), Is.SameAs(title));
                Assert.That(window.SessionForTests, Is.SameAs(session));
                Assert.That(session.Store.Snapshot.Count, Is.EqualTo(1));
                Assert.That(window.position, Is.EqualTo(bounds));
            }
            finally { window.Inputs = original; window.Close(); }
        }

        private static IEnumerator Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            button.Focus();
            yield return null;
            using (var evt = NavigationSubmitEvent.GetPooled())
            { evt.target = button; button.SendEvent(evt); }
        }
    }
}
