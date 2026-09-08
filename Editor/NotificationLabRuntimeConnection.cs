using System;
using System.Collections.Generic;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Projects only lab-owned messages into an existing application's notification store.</summary>
    internal sealed class NotificationLabRuntimeConnection : IDisposable
    {
        private readonly NotificationStore source;
        private readonly NotificationEditorTarget target;
        private readonly INotificationClock clock;
        private readonly string prefix = "deucarian.lab." + Guid.NewGuid().ToString("N") + ".";
        private readonly HashSet<NotificationId> injected = new HashSet<NotificationId>();
        private bool disposed;
        private readonly INotificationPresentationTarget presentationTarget;
        private readonly NotificationPresentationSettings originalPresentation;

        public NotificationLabRuntimeConnection(NotificationStore source, NotificationEditorTarget target,
            INotificationClock clock)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            this.target = target ?? throw new ArgumentNullException(nameof(target));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            presentationTarget = target.View as INotificationPresentationTarget;
            if (presentationTarget != null) originalPresentation = presentationTarget.Presentation;
            if (ReferenceEquals(source, target.Store)) throw new ArgumentException("The lab cannot target itself.", nameof(target));
            if (!target.IsAvailable) throw new ArgumentException("The runtime target is no longer active.", nameof(target));
            // Connect an empty session: choosing a destination must never replay or auto-ping old examples.
            if (source.Snapshot.Count != 0) throw new ArgumentException("Reset the lab before connecting.", nameof(source));
            source.SnapshotChanged += OnChanged;
        }

        public NotificationEditorTarget Target => target;
        public bool IsAvailable => !disposed && target.IsAvailable;
        public bool SupportsPresentation => presentationTarget != null;
        public NotificationPresentationSettings Presentation => presentationTarget != null
            ? presentationTarget.Presentation : NotificationPresentationSettings.Default;
        public void ConfigurePresentation(NotificationPresentationSettings settings)
        {
            if (IsAvailable) presentationTarget?.ConfigurePresentation(settings);
        }

        private void OnChanged(object sender, NotificationChangedEventArgs change)
        {
            if (!IsAvailable) return;
            var commands = new List<NotificationCommand>();
            var current = new HashSet<NotificationId>();
            foreach (NotificationItem item in change.Current.Items)
            {
                NotificationDefinition definition = item.Definition;
                var id = new NotificationId(prefix + definition.Id.Value);
                current.Add(id);
                commands.Add(NotificationCommand.Activate(new NotificationDefinition(id, definition.Severity,
                    definition.Title, definition.Body, definition.Priority, definition.FeedbackRoleId, definition.Lifetime)));
            }
            foreach (NotificationId id in injected)
                if (!current.Contains(id)) commands.Add(NotificationCommand.Resolve(id));
            injected.Clear();
            foreach (NotificationId id in current) injected.Add(id);
            Apply(commands);
        }

        private void Apply(List<NotificationCommand> commands)
        {
            if (commands.Count == 0) return;
            // A host may dispose its store before tearing down its presenter during a scene change.
            try { target.Store.ApplyBatch(commands, clock.NowSeconds); }
            catch (ObjectDisposedException) { Dispose(); }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            source.SnapshotChanged -= OnChanged;
            var commands = new List<NotificationCommand>();
            foreach (NotificationId id in injected) commands.Add(NotificationCommand.Resolve(id));
            injected.Clear();
            // Also clean up after the presenter is deactivated, if its store is still alive.
            Apply(commands);
            if (!(target.View is UnityEngine.Object obj) || obj != null)
            {
                // A deactivated presenter no longer observes cleanup. Refresh before restoring layout.
                target.View.Render(target.Store.Snapshot);
                presentationTarget?.ConfigurePresentation(originalPresentation);
            }
        }
    }
}
