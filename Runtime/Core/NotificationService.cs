using System;

namespace Deucarian.Notifications
{
    /// <summary>Owns notification state and scheduling for one application or independent scope.</summary>
    public sealed class NotificationService : IDisposable
    {
        private readonly NotificationStore store;
        private readonly NotificationEpisodeController episodes;
        private readonly NotificationPresenter presenter;
        private bool disposed;

        public NotificationService(INotificationClock clock = null,
            INotificationFeedbackSink feedback = null, INotificationListView view = null)
        {
            store = new NotificationStore(feedback);
            episodes = new NotificationEpisodeController(store, clock ?? new StopwatchNotificationClock());
            if (view == null) return;
            presenter = new NotificationPresenter(store, view);
            try { presenter.Activate(); }
            catch { Dispose(); throw; }
        }

        public INotificationSource Source => store;
        public NotificationSnapshot Snapshot => store.Snapshot;

        public void Warn(string id, string title, string message) =>
            Show(new NotificationDefinition(id, NotificationSeverity.Warning, title, message,
                feedbackRoleId: "deucarian.feedback.audio.warning"));

        public void Show(NotificationDefinition definition)
        {
            ThrowIfDisposed();
            episodes.EvaluateBatch(new[] { new NotificationConditionSample(definition, true, default) });
        }

        public void Resolve(string id)
        {
            ThrowIfDisposed();
            episodes.Resolve(new NotificationId(id));
        }

        public void Tick() { ThrowIfDisposed(); episodes.Tick(); }
        public void Clear() { ThrowIfDisposed(); episodes.Reset(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            presenter?.Dispose();
            episodes.Dispose();
            store.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NotificationService));
        }
    }
}
