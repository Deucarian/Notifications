using System;

namespace Deucarian.Notifications
{
    public interface INotificationListView
    {
        void Render(NotificationSnapshot snapshot);
    }

    /// <summary>Connects one notification store to a replaceable list view.</summary>
    public sealed class NotificationPresenter : IDisposable
    {
        private readonly NotificationStore store;
        private readonly INotificationListView view;
        private bool active;
        private bool disposed;
#if UNITY_EDITOR
        private NotificationEditorTarget editorTarget;
#endif

        public NotificationPresenter(NotificationStore store, INotificationListView view)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Activate()
        {
            ThrowIfDisposed();
            if (active)
            {
                return;
            }

            active = true;
            store.SnapshotChanged += HandleSnapshotChanged;
            view.Render(store.Snapshot);
#if UNITY_EDITOR
            editorTarget = NotificationEditorTargets.Register(store, view);
#endif
        }

        public void Deactivate()
        {
            if (!active)
            {
                return;
            }

            active = false;
            store.SnapshotChanged -= HandleSnapshotChanged;
#if UNITY_EDITOR
            editorTarget?.Dispose();
            editorTarget = null;
#endif
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            Deactivate();
            disposed = true;
        }

        private void HandleSnapshotChanged(object sender, NotificationChangedEventArgs change)
        {
            if (active)
            {
                view.Render(change.Current);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NotificationPresenter));
            }
        }
    }
}
