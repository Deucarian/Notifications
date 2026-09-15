using System;
using System.Linq;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationRuntimePreviewTests
    {
        private sealed class Clock : INotificationClock { public double NowSeconds => 0; }

        [TestCase(NotificationTransition.None)]
        [TestCase(NotificationTransition.Fade)]
        [TestCase(NotificationTransition.Scale)]
        [TestCase(NotificationTransition.Slide)]
        [TestCase(NotificationTransition.FadeAndScale)]
        [TestCase(NotificationTransition.FadeAndSlide)]
        [TestCase(NotificationTransition.ScaleAndSlide)]
        [TestCase(NotificationTransition.FadeScaleAndSlide)]
        public void LabUsesRuntimePrefabMotionAndReleasesItsPreviewScene(NotificationTransition transition)
        {
            using var workspace = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var session = new NotificationLabSession(new Clock(), null);
            var preview = new NotificationRuntimePreview(workspace, autoAdvance: false);
            NotificationListView list = null;
            try
            {
                var settings = NotificationPresentationSettings.Default;
                settings.show = settings.hide = transition;
                settings.showSeconds = settings.hideSeconds = 1;
                preview.Configure(settings, null);
                list = preview.List;
                Assert.That(list, Is.Not.Null);
                Assert.That(list.gameObject.scene.name, Is.Not.EqualTo(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name));
                session.Show(NotificationLabSession.Example(NotificationSeverity.Warning), default);
                preview.Render(session.Store.Snapshot);
                list.AdvancePreview(.25f);
                var row = list.GetComponentInChildren<NotificationRowView>();
                var group = row.GetComponent<CanvasGroup>();
                if (transition == NotificationTransition.Fade) Assert.That(group.alpha, Is.InRange(.01f, .99f));
                list.AdvancePreview(2);
                Assert.That(group.alpha, Is.EqualTo(1));
                Assert.That(row.transform.localScale, Is.EqualTo(Vector3.one));
                preview.RenderFrame();
                Assert.That(preview.HasRenderedFrame, Is.True);
                var texture = workspace.PreviewRoot.Q<UnityEngine.UIElements.Image>("notification-runtime-image").image;
                Assert.That(texture, Is.Not.Null);
                workspace.SelectTab(1);
                Assert.That(preview.List, Is.SameAs(list));
                session.Reset(); preview.Render(session.Store.Snapshot);
                list.AdvancePreview(2);
                Assert.That(list.RenderedRowCount, Is.Zero);
            }
            finally { preview.Dispose(); }
            Assert.That(list == null, Is.True, "Closing the Lab must destroy its isolated runtime objects.");
        }

        [Test]
        public void RuntimePreviewRetainsRowsDuringReflowAndAllowsInterruptedExits()
        {
            using var workspace = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var session = new NotificationLabSession(new Clock(), null);
            using var preview = new NotificationRuntimePreview(workspace, autoAdvance: false);
            var settings = NotificationPresentationSettings.Default;
            settings.show = NotificationTransition.None; settings.hide = NotificationTransition.Fade;
            settings.hideSeconds = .5f; settings.reflowSeconds = 1;
            preview.Configure(settings, null);
            var lower = new NotificationDefinition("lower", NotificationSeverity.Info, "Lower", "Body", 1);
            var upper = new NotificationDefinition("upper", NotificationSeverity.Warning, "Upper", "Body", 2);
            session.Show(lower, default); preview.Render(session.Store.Snapshot);
            var retained = preview.List.GetComponentInChildren<NotificationRowView>();
            session.Show(upper, default); preview.Render(session.Store.Snapshot);
            preview.List.AdvancePreview(.25f);
            float middle = ((RectTransform)retained.transform).anchoredPosition.y;
            Assert.That(middle, Is.LessThan(0));
            preview.List.AdvancePreview(2);
            Assert.That(((RectTransform)retained.transform).anchoredPosition.y, Is.LessThan(middle));
            session.Resolve(upper.Id); preview.Render(session.Store.Snapshot);
            preview.List.AdvancePreview(.1f);
            Assert.That(preview.List.RenderedRowCount, Is.EqualTo(2));
            session.Show(upper, default); preview.Render(session.Store.Snapshot);
            preview.List.AdvancePreview(2);
            Assert.That(preview.List.RenderedRowCount, Is.EqualTo(2));
            Assert.That(preview.List.GetComponentsInChildren<NotificationRowView>().Any(row => row == retained), Is.True);
        }
    }
}
