using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationCombinedTransitionTests
    {
        [TestCase(NotificationTransition.Fade, true, false, false)]
        [TestCase(NotificationTransition.Scale, false, true, false)]
        [TestCase(NotificationTransition.Slide, false, false, true)]
        [TestCase(NotificationTransition.FadeAndScale, true, true, false)]
        [TestCase(NotificationTransition.FadeAndSlide, true, false, true)]
        [TestCase(NotificationTransition.ScaleAndSlide, false, true, true)]
        [TestCase(NotificationTransition.FadeScaleAndSlide, true, true, true)]
        public void ChannelsComposeAtTheSameProgressAndSettleOnBothEnds(NotificationTransition mode,
            bool fade, bool scale, bool slide)
        {
            var settings = NotificationPresentationSettings.Default;
            settings.show = settings.hide = mode;
            settings.showSeconds = settings.hideSeconds = 1;
            var row = new NotificationRowTransition();
            row.SetVisible(true, settings);
            row.Advance(.2f);
            Assert.That(row.Progress, Is.InRange(.001f, .999f));
            AssertChannels(row, fade, scale, slide);
            row.Complete();
            Assert.That(row.Alpha, Is.EqualTo(1));
            Assert.That(row.Scale, Is.EqualTo(1));
            Assert.That(row.Offset, Is.EqualTo(Vector2.zero));
            row.SetVisible(false, settings);
            row.Advance(.2f);
            AssertChannels(row, fade, scale, slide);
            row.Complete();
            Assert.That(row.IsHidden, Is.True);
            Assert.That(row.Alpha, Is.Zero);
        }

        private static void AssertChannels(NotificationRowTransition row, bool fade, bool scale, bool slide)
        {
            Assert.That(row.Alpha, Is.EqualTo(fade ? row.Progress : 1));
            Assert.That(row.Scale, Is.EqualTo(scale ? Mathf.Lerp(.85f, 1, row.Progress) : 1));
            Assert.That(row.Offset, Is.EqualTo(slide ? new Vector2(-60 * (1 - row.Progress), 0) : Vector2.zero));
        }

        [Test]
        public void ExistingSerializedValuesKeepTheirMeaningAndUnknownValuesAreSafe()
        {
            Assert.That((int)NotificationTransition.None, Is.Zero);
            Assert.That((int)NotificationTransition.Fade, Is.EqualTo(1));
            Assert.That((int)NotificationTransition.Scale, Is.EqualTo(2));
            Assert.That((int)NotificationTransition.Slide, Is.EqualTo(3));
            var settings = NotificationPresentationSettings.Default;
            settings.show = (NotificationTransition)999;
            Assert.That(settings.Sanitized().show, Is.EqualTo(NotificationTransition.None));
        }
    }
}
