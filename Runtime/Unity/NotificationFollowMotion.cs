using Deucarian.UI;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    internal sealed class NotificationFollowMotion
    {
        private readonly DeucarianLazyFollowState motion = new DeucarianLazyFollowState();
        private Transform parent;
        private Pose fixedLocalPose;
        private bool following;

        public void Advance(Transform view, bool enabled, DeucarianLazyFollowSettings settings, float deltaSeconds)
        {
            if (!enabled || view.parent == null) { Restore(view); return; }
            if (following && parent != view.parent) Restore(view);
            if (!following)
            {
                parent = view.parent;
                fixedLocalPose = new Pose(view.localPosition, view.localRotation);
                motion.Reset(new Pose(view.position, view.rotation));
                following = true;
            }
            var desired = new Pose(parent.TransformPoint(fixedLocalPose.position), parent.rotation * fixedLocalPose.rotation);
            var pose = motion.Advance(desired, settings, deltaSeconds);
            view.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public void Restore(Transform view)
        {
            if (!following) return;
            view.localPosition = fixedLocalPose.position;
            view.localRotation = fixedLocalPose.rotation;
            following = false;
            parent = null;
        }
    }
}
