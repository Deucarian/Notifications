using System;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabMotionPreviewTests
    {
        [TestCase(NotificationTransition.Fade)]
        [TestCase(NotificationTransition.Scale)]
        [TestCase(NotificationTransition.Slide)]
        public void BothTabsShareLiveMotionAndExitSettings(NotificationTransition transition)
        {
            using var view = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var preview = new NotificationLabRowPreview(view);
            var settings = NotificationPresentationSettings.Default;
            settings.show = transition; settings.hide = transition; settings.showSeconds = settings.hideSeconds = 1;
            preview.Configure(settings, null);
            view.SetMessages(new[] { Item("one") }, Array.Empty<DeucarianEditorMessageData>(), 0);
            var row = view.VisibleRows.Q<DeucarianEditorMessageRow>("one");
            double now = EditorApplication.timeSinceStartup;
            preview.Advance(now);
            preview.Advance(now + .5);
            var reference = new NotificationRowTransition();
            reference.SetVisible(true, settings);
            reference.Advance(.5f);
            Assert.That(Progress(row, transition), Is.EqualTo(reference.Progress).Within(.01));
            view.SelectTab(1);
            Assert.That(view.VisibleRows.Q<DeucarianEditorMessageRow>("one"), Is.SameAs(row));
            Assert.That(Progress(row, transition), Is.EqualTo(reference.Progress).Within(.01));
            settings.showSeconds = .25f;
            preview.Configure(settings, null);
            preview.Advance(now + .75);
            Assert.That(Progress(row, transition), Is.EqualTo(1).Within(.01));
            view.SelectTab(0);
            view.SetMessages(Array.Empty<DeucarianEditorMessageData>(), Array.Empty<DeucarianEditorMessageData>(), 0);
            Assert.That(row.parent, Is.Not.Null);
            preview.Advance(EditorApplication.timeSinceStartup + 2);
            Assert.That(row.parent, Is.Null);
        }

        [Test]
        public void ExitKeepsItsVisibleSlotUntilAnotherMessageCanEnter()
        {
            using var view = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var preview = new NotificationLabRowPreview(view);
            var settings = NotificationPresentationSettings.Default;
            settings.maxVisible = 1; settings.showSeconds = 0; settings.hideSeconds = 1;
            preview.Configure(settings, null);
            view.SetMessages(new[] { Item("one") }, Array.Empty<DeucarianEditorMessageData>(), 0);
            preview.Advance(EditorApplication.timeSinceStartup);
            view.SetMessages(new[] { Item("two") }, Array.Empty<DeucarianEditorMessageData>(), 0);
            var second = view.VisibleRows.Q<DeucarianEditorMessageRow>("two");
            preview.Advance(EditorApplication.timeSinceStartup);
            Assert.That(second.style.opacity.value, Is.Zero);
            Assert.That(second.style.display.value, Is.EqualTo(DisplayStyle.None), "Waiting rows must not consume any layout space.");
            preview.Advance(EditorApplication.timeSinceStartup + 2);
            preview.Advance(EditorApplication.timeSinceStartup + 2);
            Assert.That(second.style.opacity.value, Is.EqualTo(1));
        }

        [Test]
        public void NoneEnterAndExitCompleteWithoutWaitingForConfiguredSeconds()
        {
            using var view = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var preview = new NotificationLabRowPreview(view);
            var settings = NotificationPresentationSettings.Default;
            settings.show = settings.hide = NotificationTransition.None;
            settings.showSeconds = settings.hideSeconds = 2;
            preview.Configure(settings, null);
            view.SetMessages(new[] { Item("one") }, Array.Empty<DeucarianEditorMessageData>(), 0);
            preview.Advance(EditorApplication.timeSinceStartup);
            var row = view.VisibleRows.Q<DeucarianEditorMessageRow>("one");
            Assert.That(row.style.opacity.value, Is.EqualTo(1));
            view.SetMessages(Array.Empty<DeucarianEditorMessageData>(), Array.Empty<DeucarianEditorMessageData>(), 0);
            Assert.That(row.parent, Is.Null);
        }

        [TestCase(NotificationTransition.Fade)]
        [TestCase(NotificationTransition.Scale)]
        [TestCase(NotificationTransition.Slide)]
        public void ResolvingDuringEntranceReversesFromTheCurrentPosition(NotificationTransition transition)
        {
            using var view = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var preview = new NotificationLabRowPreview(view);
            var settings = NotificationPresentationSettings.Default;
            settings.show = settings.hide = transition; settings.showSeconds = settings.hideSeconds = 2;
            preview.Configure(settings, null);
            view.SetMessages(new[] { Item("one") }, Array.Empty<DeucarianEditorMessageData>(), 0);
            var row = view.VisibleRows.Q<DeucarianEditorMessageRow>("one");
            double now = EditorApplication.timeSinceStartup;
            preview.Advance(now); preview.Advance(now + .2);
            float before = Progress(row, transition);
            view.SetMessages(Array.Empty<DeucarianEditorMessageData>(), Array.Empty<DeucarianEditorMessageData>(), 0);
            preview.Advance(now + .2);
            Assert.That(Progress(row, transition), Is.EqualTo(before).Within(.0001));
            preview.Advance(now + .3);
            Assert.That(Progress(row, transition), Is.LessThan(before));
        }

        [Test]
        public void DemotionFinishesInItsSlotAndPromotionCanReverseTheExit()
        {
            using var view = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var preview = new NotificationLabRowPreview(view);
            var settings = NotificationPresentationSettings.Default;
            settings.maxVisible = 1; settings.showSeconds = 0; settings.hideSeconds = 1;
            preview.Configure(settings, null);
            double now = EditorApplication.timeSinceStartup;
            view.SetMessages(new[] { Item("one") }, new[] { Item("two") }, 0);
            preview.Advance(now);
            var first = view.VisibleRows.Q<DeucarianEditorMessageRow>("one");
            view.SetMessages(new[] { Item("two") }, new[] { Item("one") }, 0);
            Assert.That(first.parent, Is.SameAs(view.VisibleRows));
            preview.Advance(now + .2);
            Assert.That(first.style.opacity.value, Is.LessThan(1));
            view.SetMessages(new[] { Item("one") }, new[] { Item("two") }, 0);
            preview.Advance(now + .3);
            Assert.That(first.parent, Is.SameAs(view.VisibleRows));
            Assert.That(first.style.opacity.value, Is.EqualTo(1));
            view.SetMessages(new[] { Item("two") }, new[] { Item("one") }, 0);
            preview.Advance(now + 2); preview.Advance(now + 2);
            Assert.That(first.parent, Is.Not.SameAs(view.VisibleRows));
            Assert.That(view.VisibleRows.Q<DeucarianEditorMessageRow>("two").style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        private static float Progress(DeucarianEditorMessageRow row, NotificationTransition transition) =>
            transition == NotificationTransition.Fade ? row.style.opacity.value :
            transition == NotificationTransition.Scale ? (row.transform.scale.x - .85f) / .15f : 1 + row.transform.position.x / 60;
        private static DeucarianEditorMessageData Item(string id) =>
            new DeucarianEditorMessageData(id, "Warning", "Test content", DeucarianEditorStatus.Warning, "");
    }
}
