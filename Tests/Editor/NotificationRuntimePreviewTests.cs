using System;
using System.Collections;
using System.Linq;
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
    public sealed class NotificationRuntimePreviewTests
    {
        private sealed class Clock : INotificationClock { public double NowSeconds => 0; }
        private sealed class PreviewLayoutWindow : EditorWindow { }

        [UnityTest]
        public IEnumerator RuntimePreviewUsesPaneWidthAtWideAndNarrowWindowSizes()
        {
            var window = ScriptableObject.CreateInstance<PreviewLayoutWindow>();
            try
            {
                window.position = new Rect(90, 90, 1460, 900);
                window.Show();
                using var workspace = new DeucarianEditorLabWorkspace(window.rootVisualElement, "Test", "Notifications", "", () => { }, _ => { });
                using var session = new NotificationLabSession(new Clock(), null);
                using var preview = new NotificationRuntimePreview(workspace, autoAdvance: false);
                var settings = NotificationPresentationSettings.Default;
                settings.show = NotificationTransition.None;
                preview.Configure(settings, null);
                session.Show(NotificationLabSession.Example(NotificationSeverity.Warning), default);
                preview.Render(session.Store.Snapshot);
                preview.RenderFrame();
                var image = workspace.PreviewRoot.Q<UnityEngine.UIElements.Image>("notification-runtime-image");
                yield return WaitForPreviewLayout(image);
                AssertPreviewAspect(image);
                float wideWidth = image.contentRect.width;

                window.position = new Rect(90, 90, 860, 900);
                yield return WaitForPreviewLayout(image, wideWidth);
                AssertPreviewAspect(image);
                Assert.That(image.contentRect.width, Is.Not.EqualTo(wideWidth).Within(1));
            }
            finally { window.Close(); }
        }

        private static IEnumerator WaitForPreviewLayout(UnityEngine.UIElements.Image image, float previousWidth = -1)
        {
            double deadline = EditorApplication.timeSinceStartup + 3;
            while ((image.contentRect.width <= 0 || Mathf.Approximately(image.contentRect.width, previousWidth))
                && EditorApplication.timeSinceStartup < deadline) yield return null;
            yield return null;
            yield return null;
        }

        private static void AssertPreviewAspect(UnityEngine.UIElements.Image image)
        {
            Assert.That(image.image, Is.Not.Null);
            Assert.That(image.worldBound.width, Is.GreaterThan(0));
            float aspect = (float)image.image.height / image.image.width;
            Assert.That(image.worldBound.height / image.worldBound.width, Is.EqualTo(aspect).Within(.01f),
                "Runtime pixels must fill the pane with their aspect intact after workspace scaling.");
            Assert.That(image.resolvedStyle.flexShrink, Is.Zero);
            Assert.That(image.worldBound.width, Is.EqualTo(image.parent.worldBound.width).Within(2));
        }

        [Test]
        public void RuntimePreviewDrawsVisibleNotificationPixels()
        {
            using var workspace = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var session = new NotificationLabSession(new Clock(), null);
            using var preview = new NotificationRuntimePreview(workspace, autoAdvance: false);
            var settings = NotificationPresentationSettings.Default;
            settings.show = NotificationTransition.None;
            preview.Configure(settings, null);
            session.Show(NotificationLabSession.Example(NotificationSeverity.Warning), default);
            preview.Render(session.Store.Snapshot);
            preview.List.AdvancePreview(2);
            preview.RenderFrame();
            var texture = (RenderTexture)workspace.PreviewRoot.Q<UnityEngine.UIElements.Image>("notification-runtime-image").image;
            var previous = RenderTexture.active;
            var pixels = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture;
                pixels.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                pixels.Apply();
                Assert.That(pixels.GetPixels32().Count(pixel => pixel.a > 32), Is.GreaterThan(1000),
                    "The real row must produce visible pixels, not merely a non-null preview texture.");
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

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
