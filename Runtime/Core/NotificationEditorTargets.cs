#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Deucarian.Notifications
{
    /// <summary>Editor-only discovery of explicitly composed presenters. Absent from player builds.</summary>
    public static class NotificationEditorTargets
    {
        private static readonly List<WeakReference<NotificationEditorTarget>> targets =
            new List<WeakReference<NotificationEditorTarget>>();

        internal static NotificationEditorTarget Register(NotificationStore store, INotificationListView view)
        {
            var target = new NotificationEditorTarget(store, view);
            lock (targets) targets.Add(new WeakReference<NotificationEditorTarget>(target));
            return target;
        }

        public static NotificationEditorTarget[] Capture()
        {
            var result = new List<NotificationEditorTarget>();
            lock (targets)
            {
                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    if (!targets[i].TryGetTarget(out var target) || !target.IsAvailable)
                        targets.RemoveAt(i);
                    else
                        result.Add(target);
                }
            }
            result.Reverse();
            return result.ToArray();
        }
    }

    /// <summary>A live presenter endpoint; discovery never takes ownership of its store or view.</summary>
    public sealed class NotificationEditorTarget : IDisposable
    {
        private bool disposed;

        internal NotificationEditorTarget(NotificationStore store, INotificationListView view)
        {
            Store = store;
            View = view;
        }

        public NotificationStore Store { get; }
        public INotificationListView View { get; }
        public bool IsAvailable => !disposed && !Store.CaptureDiagnostics().Disposed;
        public void Dispose() => disposed = true;
    }
}
#endif
