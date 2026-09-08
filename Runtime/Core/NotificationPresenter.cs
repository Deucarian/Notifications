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
        private readonly INotificationSource source;
        private readonly INotificationListView view;
        private bool active;
        private bool disposed;
#if UNITY_EDITOR
        private NotificationEditorTarget editorTarget;
        private readonly Func<NotificationEditorTarget> registerEditorTarget;
#endif

        public NotificationPresenter(NotificationStore store, INotificationListView view)
            : this((INotificationSource)store, view)
        {
#if UNITY_EDITOR
            registerEditorTarget = () => NotificationEditorTargets.Register(store, view);
#endif
        }

        public NotificationPresenter(INotificationSource source, INotificationListView view)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
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
            source.SnapshotChanged += HandleSnapshotChanged;
            try
            {
                view.Render(source.Snapshot);
#if UNITY_EDITOR
                editorTarget = registerEditorTarget?.Invoke();
#endif
            }
            catch
            {
                Deactivate();
                throw;
            }
        }

        public void Deactivate()
        {
            if (!active)
            {
                return;
            }

            active = false;
            source.SnapshotChanged -= HandleSnapshotChanged;
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
