using System;
using System.Collections.Generic;

namespace Deucarian.Notifications
{
    /// <summary>Owns notification state and scheduling for one application or independent scope.</summary>
    public sealed class NotificationService : IDisposable
    {
        private readonly NotificationStore store;
        private readonly NotificationEpisodeController episodes;
        private readonly NotificationPresenter presenter;
        private readonly INotificationDefinitions definitions;
        private readonly INotificationResolutionView resolutionView;
        private bool disposed;

        public NotificationService(INotificationClock clock = null,
            INotificationFeedbackSink feedback = null, INotificationListView view = null,
            INotificationDefinitions definitions = null)
        {
            this.definitions = definitions;
            store = new NotificationStore(feedback, definitions);
            episodes = new NotificationEpisodeController(store, clock ?? new StopwatchNotificationClock());
            if (view == null) return;
            presenter = new NotificationPresenter(store, view);
            resolutionView = view as INotificationResolutionView;
            try { resolutionView?.BindResolution(ResolveFromView); presenter.Activate(); }
            catch { Dispose(); throw; }
        }

        public INotificationSource Source => store;
        public NotificationSnapshot Snapshot => store.Snapshot;

        /// <summary>Validates the entire batch before applying keyed conditions and their activation/recovery delays.</summary>
        public void EvaluateBatch(IEnumerable<NotificationCondition> conditions)
        {
            ThrowIfDisposed();
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            var samples = new List<NotificationConditionSample>();
            foreach (var condition in conditions)
                samples.Add(new NotificationConditionSample(
                    NotificationDefinitions.Require(definitions, condition.Key), condition.IsUnhealthy, condition.Timing));
            episodes.EvaluateBatch(samples);
        }

        public void Warn(NotificationKey key, string title, string message)
        {
            ThrowIfDisposed();
            var definition = NotificationDefinitions.Require(definitions, key);
            if (definition.Severity != NotificationSeverity.Warning)
                throw new InvalidOperationException("The selected definition is not a warning. Use Show to preserve its registered severity.");
            Show(key, title, message);
        }

        public void Show(NotificationDefinition definition)
        {
            ThrowIfDisposed();
            episodes.EvaluateBatch(new[] { new NotificationConditionSample(definition, true, default) });
        }

        public void Show(NotificationKey key, string title = null, string message = null) =>
            Show(key, new NotificationContentOverrides(title, message));

        public void Show(NotificationKey key, NotificationContentOverrides overrides)
        {
            ThrowIfDisposed();
            RequireKey(key);
            var definition = NotificationDefinitions.Require(definitions, key);
            Show(new NotificationDefinition(definition.Id, definition.Severity, overrides?.Title ?? definition.Title,
                overrides?.Message ?? definition.Body, definition.Priority, overrides?.FeedbackRoleId ?? definition.FeedbackRoleId,
                definition.Lifetime, definition.AllowManualResolution));
        }

        private void ResolveFromView(NotificationId id)
        {
            if (disposed || !Snapshot.TryGet(id, out var item)) return;
            if (item.Definition.AllowManualResolution && item.Definition.Lifetime.Kind == NotificationLifetimeKind.UntilResolved)
                episodes.Resolve(id);
        }

        public void Resolve(NotificationKey key)
        {
            ThrowIfDisposed();
            episodes.Resolve(new NotificationId(RequireKey(key)));
        }

        public void Tick() { ThrowIfDisposed(); episodes.Tick(); }
        public void Clear() { ThrowIfDisposed(); episodes.Reset(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            resolutionView?.BindResolution(null);
            presenter?.Dispose();
            episodes.Dispose();
            store.Dispose();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NotificationService));
        }

        private static string RequireKey(NotificationKey key) => key != null ? key.Id :
            throw new ArgumentNullException(nameof(key), "Select a notification key in the Inspector or reuse a named definition from your NotificationKeys class.");
    }
}
