using System;
using System.Collections;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabReflowTests
    {
        private sealed class Host : EditorWindow { }

        [UnityTest] public IEnumerator RealLabGeometryReflowsOnInsertAndClosesTheExitGapInBothTabs()
        {
            var host = ScriptableObject.CreateInstance<Host>(); host.position = new Rect(30, 30, 1500, 850); host.Show();
            try
            {
                using var view = new DeucarianEditorLabWorkspace(host.rootVisualElement, "Test", "Notifications", "", () => { }, _ => { });
                using var preview = new NotificationLabRowPreview(view, autoAdvance: false);
                var settings = NotificationPresentationSettings.Default;
                settings.show = NotificationTransition.None; settings.hide = NotificationTransition.Fade;
                settings.hideSeconds = .2f; settings.reflowSeconds = 1;
                preview.Configure(settings, null);
                view.SetMessages(new[] { Item("old") }, Array.Empty<DeucarianEditorMessageData>(), 0);
                preview.Advance(0);
                for (int i = 0; i < 8; i++) yield return null;
                var old = view.VisibleRows[0]; float initial = old.layout.y;
                view.SetMessages(new[] { Item("new"), Item("old") }, Array.Empty<DeucarianEditorMessageData>(), 0);
                preview.Advance(0);
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(old.layout.y, Is.GreaterThan(initial + 10));
                Assert.That(old.layout.y + old.transform.position.y, Is.EqualTo(initial).Within(.1f));
                preview.Advance(.25);
                Assert.That(old.transform.position.y, Is.InRange(-old.layout.y + .1f, -.1f));
                preview.Advance(2);
                Assert.That(old.transform.position.y, Is.Zero.Within(.1f));
                view.SelectTab(1);
                for (int i = 0; i < 4; i++) yield return null;
                preview.Advance(4);
                float beforeExit = old.layout.y;
                view.SetMessages(new[] { Item("old") }, Array.Empty<DeucarianEditorMessageData>(), 0);
                preview.Advance(4.1);
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(view.VisibleRows.IndexOf(old), Is.EqualTo(1), "The exiting row keeps its slot.");
                Assert.That(old.layout.y, Is.EqualTo(beforeExit).Within(.1f));
                preview.Advance(4.3);
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(view.VisibleRows.IndexOf(old), Is.Zero);
                Assert.That(old.transform.position.y, Is.GreaterThan(1), "The gap closes by motion, not a position jump.");
                preview.Advance(6);
                Assert.That(old.transform.position.y, Is.Zero.Within(.1f));
                settings.instantLayout = true; preview.Configure(settings, null);
                view.SetMessages(new[] { Item("instant"), Item("old") }, Array.Empty<DeucarianEditorMessageData>(), 0);
                preview.Advance(6);
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(old.transform.position.y, Is.Zero.Within(.1f));
                Assert.That(view.VisibleRows[1], Is.SameAs(old));
            }
            finally { host.Close(); }
        }

        [UnityTest] public IEnumerator QueuedRowsStayReadableAfterGeometryChangesAndDemotion()
        {
            var host = ScriptableObject.CreateInstance<Host>(); host.position = new Rect(30, 30, 1500, 850); host.Show();
            try
            {
                using var view = new DeucarianEditorLabWorkspace(host.rootVisualElement, "Test", "Notifications", "", () => { }, _ => { });
                using var preview = new NotificationLabRowPreview(view, autoAdvance: false);
                var settings = NotificationPresentationSettings.Default;
                settings.maxVisible = 1; settings.show = NotificationTransition.None; settings.hide = NotificationTransition.Fade;
                settings.hideSeconds = .2f;
                preview.Configure(settings, null);
                view.SetMessages(new[] { Item("visible") }, new[] { Item("queued") }, 0);
                preview.Advance(0);
                var overflow = view.PreviewRoot.Q<Foldout>("lab-overflow");
                overflow.value = true;
                for (int i = 0; i < 8; i++) yield return null;
                var queued = overflow.Q<DeucarianEditorMessageRow>("queued");
                Assert.That(queued.layout.height, Is.GreaterThan(1));
                Assert.That(queued.resolvedStyle.opacity, Is.EqualTo(1));
                var demoted = view.VisibleRows[0];
                view.SetMessages(new[] { Item("queued") }, new[] { Item("visible") }, 0);
                preview.Advance(.3);
                preview.Advance(.4);
                for (int i = 0; i < 8; i++) yield return null;
                Assert.That(demoted.parent, Is.SameAs(overflow.contentContainer));
                Assert.That(demoted.resolvedStyle.opacity, Is.EqualTo(1));
                Assert.That(demoted.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(view.VisibleRows[0], Is.SameAs(queued));
                Assert.That(queued.resolvedStyle.opacity, Is.EqualTo(1));
            }
            finally { host.Close(); }
        }

        [Test] public void OlderRecipesEnableSmoothLayoutWithoutChangingVisibilityChoices()
        {
            var settings = new NotificationPresentationSettings { maxVisible = 5, show = NotificationTransition.None };
            Assert.That(settings.Sanitized().ReflowDuration, Is.EqualTo(.18f));
            Assert.That(settings.Sanitized().show, Is.EqualTo(NotificationTransition.None));
            settings.instantLayout = true;
            Assert.That(settings.Sanitized().ReflowDuration, Is.Zero);
        }

        private static DeucarianEditorMessageData Item(string id) => new DeucarianEditorMessageData(id, "Warning", "A test message", DeucarianEditorStatus.Warning, "");
    }
}
