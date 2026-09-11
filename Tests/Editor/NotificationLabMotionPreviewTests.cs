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
    public sealed class NotificationLabMotionPreviewTests
    {
        [TestCase(NotificationTransition.Fade)]
        [TestCase(NotificationTransition.Scale)]
        [TestCase(NotificationTransition.Slide)]
        public void StopAndDisposeRestoreTheSpecimenWithoutAffectingAnotherPreview(NotificationTransition transition)
        {
            using var first = new NotificationLabMotionPreview { Enter = transition, EnterSeconds = 10 };
            using var second = new NotificationLabMotionPreview { Enter = NotificationTransition.Fade, EnterSeconds = 10 };
            first.Play(); second.Play(); first.Stop();
            Assert.That(first.Specimen.style.opacity.value, Is.EqualTo(1));
            Assert.That(first.Specimen.transform.scale, Is.EqualTo(Vector3.one));
            Assert.That(first.Specimen.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(second.Specimen.style.opacity.value, Is.LessThan(0.01f));
            first.Dispose(); first.Play();
            Assert.That(first.Specimen.style.opacity.value, Is.EqualTo(1));
            Assert.That(first.Q<Button>("motion-preview-play").enabledSelf, Is.False);
        }

        [Test]
        public void PreviewRestoresDismissedExamples()
        {
            using var preview = new NotificationLabMotionPreview();
            var example = new Label("Dismissed example"); preview.Specimen.Add(example);
            example.style.display = DisplayStyle.None;
            preview.Play();
            Assert.That(example.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator LeavingAppearanceStopsThePageOwnedPreview()
        {
            var window = ScriptableObject.CreateInstance<MotionPreviewTestWindow>(); window.Show();
            try
            {
                using var session = new DeucarianEditorPageSession(window, "fixture", _ => { });
                session.Navigate("deucarian.notifications.lab");
                for (int i = 0; i < 6; i++) yield return null;
                var tabs = window.rootVisualElement.Q("workspace-tabs").Q<DeucarianEditorChoiceBar>();
                tabs.Q<Button>("choice-1").Focus();
                using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = tabs.Q<Button>("choice-1"); evt.target.SendEvent(evt); }
                var motion = window.rootVisualElement.Q<NotificationLabMotionPreview>();
                motion.Enter = NotificationTransition.Fade; motion.EnterSeconds = 10; motion.Play();
                Assert.That(motion.Specimen.style.opacity.value, Is.LessThan(0.01f));
                tabs.Q<Button>("choice-0").Focus();
                using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = tabs.Q<Button>("choice-0"); evt.target.SendEvent(evt); }
                Assert.That(motion.Specimen.style.opacity.value, Is.EqualTo(1));
            }
            finally { window.Close(); }
        }
    }

    internal sealed class MotionPreviewTestWindow : EditorWindow { }
}
