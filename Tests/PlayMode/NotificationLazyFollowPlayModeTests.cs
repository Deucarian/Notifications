using System.Collections;
using Deucarian.Notifications.Unity;
using Deucarian.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Notifications.PlayModeTests
{
    public sealed class NotificationLazyFollowPlayModeTests
    {
        [UnityTest]
        public IEnumerator FollowOffAndDisableRestorePlacementAndScreenSpaceNeverDrifts()
        {
            var anchor = new GameObject("Moving anchor", typeof(RectTransform), typeof(Canvas));
            var canvas = anchor.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var root = Object.Instantiate(Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList"), anchor.transform);
            var view = root.GetComponent<NotificationListView>();
            try
            {
                view.ConfigureAnchor(.05f, .5f, false);
                Vector3 local = root.transform.localPosition;
                Quaternion rotation = root.transform.localRotation;
                var settings = NotificationPresentationSettings.Default;
                settings.lazyFollow = true;
                settings.follow = new DeucarianLazyFollowSettings { positionDeadZone = 10, rotationDeadZone = 180, smoothingSeconds = 2 };
                view.ConfigurePresentation(settings);
                yield return null;
                Vector3 held = root.transform.position;
                anchor.transform.position += Vector3.right;
                yield return null;
                Assert.That(Vector3.Distance(root.transform.position, held), Is.LessThan(.001f));
                settings.lazyFollow = false;
                view.ConfigurePresentation(settings);
                Assert.That(Vector3.Distance(root.transform.localPosition, local), Is.LessThan(.001f));
                Assert.That(Quaternion.Angle(root.transform.localRotation, rotation), Is.LessThan(.001f));

                settings.lazyFollow = true;
                view.ConfigurePresentation(settings);
                yield return null;
                anchor.transform.position += Vector3.up;
                yield return null;
                view.enabled = false;
                Assert.That(Vector3.Distance(root.transform.localPosition, local), Is.LessThan(.001f));
                view.enabled = true;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                yield return null;
                Assert.That(view.SupportsLazyFollow, Is.False);
                Assert.That(Vector3.Distance(root.transform.localPosition, local), Is.LessThan(.001f));
            }
            finally { Object.DestroyImmediate(anchor); }
        }
    }
}
