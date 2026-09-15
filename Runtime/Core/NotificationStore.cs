using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.Diagnostics;

namespace Deucarian.Notifications
{
    /// <summary>Authoritative keyed store for active notification episodes.</summary>
    public sealed class NotificationStore : INotificationSource, INotificationCommands, IDisposable
    {
        private static long nextRuntimeId;

        private readonly object syncRoot = new object();
        private readonly Dictionary<NotificationId, NotificationItem> active =
            new Dictionary<NotificationId, NotificationItem>();
        private readonly INotificationFeedbackSink feedbackSink;
        private readonly INotificationDefinitions definitions;
#if UNITY_EDITOR
        private readonly List<NotificationEditorDefinitionScope> editorScopes = new List<NotificationEditorDefinitionScope>();
#endif
        private readonly DiagnosticProviderRegistration diagnosticsRegistration;

        private NotificationSnapshot snapshot = NotificationSnapshot.Empty;
        private long nextEpisode;
        private long transitionCount;
        private long feedbackRequestCount;
        private long feedbackFailureCount;
        private bool disposed;

        public NotificationStore(INotificationFeedbackSink feedbackSink = null, INotificationDefinitions definitions = null)
        {
            this.feedbackSink = feedbackSink;
            this.definitions = definitions;
            string runtimeId = Interlocked.Increment(ref nextRuntimeId).ToString();
            diagnosticsRegistration = DiagnosticProviderRegistry.Register(
                new NotificationDiagnosticProvider("notifications." + runtimeId, CaptureDiagnostics));
        }

        public event EventHandler<NotificationChangedEventArgs> SnapshotChanged;

        public void ValidateDefinition(NotificationDefinition definition)
        {
            ThrowIfDisposed();
            if (definition == null) throw new ArgumentNullException(nameof(definition));
#if UNITY_EDITOR
            lock (syncRoot)
                foreach (var scope in editorScopes)
                    if (scope.Contains(definition.Id)) return;
#endif
            NotificationDefinitions.Require(definitions, definition.Id);
        }

#if UNITY_EDITOR
        internal NotificationEditorDefinitionScope CreateEditorScope()
        {
            ThrowIfDisposed();
            var scope = new NotificationEditorDefinitionScope(this);
            lock (syncRoot) editorScopes.Add(scope);
            return scope;
        }

        internal void ValidateEditorRegistration(NotificationEditorDefinitionScope owner, NotificationDefinition definition)
        {
            ThrowIfDisposed();
            lock (syncRoot)
            {
                if (definitions != null && definitions.TryGet(new EditorLookupKey(definition.Id.Value), out _))
                    throw new InvalidOperationException("Temporary Lab messages cannot replace a registered application definition.");
                foreach (var scope in editorScopes)
                    if (!ReferenceEquals(scope, owner) && scope.Contains(definition.Id))
                        throw new InvalidOperationException("This temporary definition belongs to another Lab session.");
            }
        }

        private sealed class EditorLookupKey : NotificationKey { internal EditorLookupKey(string id) : base(id) { } }

        internal void RemoveEditorScope(NotificationEditorDefinitionScope scope)
        {
            lock (syncRoot) editorScopes.Remove(scope);
            if (!disposed)
            {
                var commands = new List<NotificationCommand>();
                foreach (var id in scope.Ids) commands.Add(NotificationCommand.Resolve(id));
                ApplyBatch(commands, 0);
            }
        }
#endif

        public NotificationSnapshot Snapshot
        {
            get
            {
                lock (syncRoot)
                {
                    return snapshot;
                }
            }
        }

        public NotificationChangedEventArgs ApplyBatch(
            IEnumerable<NotificationCommand> commands,
            double nowSeconds)
        {
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            ThrowIfDisposed();
            List<NotificationCommand> finalCommands =
                CoalesceCommands(commands);
            foreach (NotificationCommand command in finalCommands)
                if (command.IsActive) ValidateDefinition(command.Definition);
            List<NotificationId> activated = new List<NotificationId>();
            List<NotificationId> resolved = new List<NotificationId>();
            List<NotificationDefinition> activatedDefinitions =
                new List<NotificationDefinition>();
            NotificationChangedEventArgs change = null;

            lock (syncRoot)
            {
                NotificationSnapshot previous = snapshot;
                bool changed = false;

                for (int commandIndex = 0; commandIndex < finalCommands.Count; commandIndex++)
                {
                    NotificationCommand command = finalCommands[commandIndex];
                    if (command.Id.IsEmpty)
                    {
                        throw new ArgumentException("A batch contains an empty notification ID.", nameof(commands));
                    }

                    if (command.IsActive)
                    {
                        changed |= ApplyActive(
                            command.Definition,
                            nowSeconds,
                            activated,
                            activatedDefinitions);
                    }
                    else if (active.Remove(command.Id))
                    {
                        resolved.Add(command.Id);
                        transitionCount++;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return null;
                }

                snapshot = BuildSnapshot(previous.Version + 1);
                change = new NotificationChangedEventArgs(
                    previous,
                    snapshot,
                    activated.ToArray(),
                    resolved.ToArray());
            }

            RequestFeedback(activatedDefinitions);
            SnapshotChanged?.Invoke(this, change);
            return change;
        }

        private static List<NotificationCommand> CoalesceCommands(
            IEnumerable<NotificationCommand> commands)
        {
            Dictionary<NotificationId, NotificationCommand> byId =
                new Dictionary<NotificationId, NotificationCommand>();
            foreach (NotificationCommand command in commands)
            {
                if (command.Id.IsEmpty)
                {
                    throw new ArgumentException("A batch contains an empty notification ID.", nameof(commands));
                }

                byId[command.Id] = command;
            }

            List<NotificationCommand> finalCommands =
                new List<NotificationCommand>(byId.Values);
            finalCommands.Sort((left, right) => left.Id.CompareTo(right.Id));
            return finalCommands;
        }

        public NotificationChangedEventArgs ClearAll(double nowSeconds = 0d)
        {
            ThrowIfDisposed();
            NotificationId[] ids;
            lock (syncRoot)
            {
                ids = new NotificationId[active.Count];
                active.Keys.CopyTo(ids, 0);
            }

            if (ids.Length == 0)
            {
                return null;
            }

            NotificationCommand[] commands = new NotificationCommand[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                commands[i] = NotificationCommand.Resolve(ids[i]);
            }

            return ApplyBatch(commands, nowSeconds);
        }

        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                active.Clear();
#if UNITY_EDITOR
                editorScopes.Clear();
#endif
                snapshot = NotificationSnapshot.Empty;
                SnapshotChanged = null;
            }

            diagnosticsRegistration?.Dispose();
        }

        internal NotificationDiagnosticState CaptureDiagnostics()
        {
            lock (syncRoot)
            {
                NotificationSeverity highest = NotificationSeverity.Info;
                for (int i = 0; i < snapshot.Count; i++)
                {
                    if (snapshot[i].Definition.Severity > highest)
                    {
                        highest = snapshot[i].Definition.Severity;
                    }
                }

                return new NotificationDiagnosticState(
                    snapshot.Count,
                    highest,
                    transitionCount,
                    feedbackRequestCount,
                    feedbackFailureCount,
                    disposed);
            }
        }

        private bool ApplyActive(
            NotificationDefinition definition,
            double nowSeconds,
            List<NotificationId> activated,
            List<NotificationDefinition> activatedDefinitions)
        {
            if (definition == null)
            {
                throw new ArgumentException("Active commands require a definition.");
            }

            if (active.TryGetValue(definition.Id, out NotificationItem current))
            {
                if (current.Definition.Equals(definition))
                {
                    return false;
                }

                active[definition.Id] = new NotificationItem(
                    definition,
                    current.Episode,
                    current.ActivatedAtSeconds);
                return true;
            }

            NotificationItem item = new NotificationItem(
                definition,
                ++nextEpisode,
                nowSeconds);
            active.Add(definition.Id, item);
            activated.Add(definition.Id);
            activatedDefinitions.Add(definition);
            transitionCount++;
            return true;
        }

        private NotificationSnapshot BuildSnapshot(long version)
        {
            NotificationItem[] items = new NotificationItem[active.Count];
            active.Values.CopyTo(items, 0);
            Array.Sort(items, CompareItems);
            return new NotificationSnapshot(version, items);
        }

        private void RequestFeedback(List<NotificationDefinition> activatedDefinitions)
        {
            if (feedbackSink == null || activatedDefinitions.Count == 0)
            {
                return;
            }

            activatedDefinitions.Sort(CompareFeedbackCandidates);
            NotificationDefinition selected = activatedDefinitions[0];
            if (string.IsNullOrEmpty(selected.FeedbackRoleId))
            {
                return;
            }

            bool accepted = false;
            try
            {
                accepted = feedbackSink.TryRequestFeedback(
                    new NotificationFeedbackRequest(
                        selected.FeedbackRoleId,
                        selected.Severity,
                        selected.Priority,
                        activatedDefinitions.Count));
            }
            catch
            {
                accepted = false;
            }

            lock (syncRoot)
            {
                feedbackRequestCount++;
                if (!accepted)
                {
                    feedbackFailureCount++;
                }
            }
        }

        private static int CompareItems(NotificationItem left, NotificationItem right)
        {
            int priority = right.Definition.Priority.CompareTo(left.Definition.Priority);
            if (priority != 0)
            {
                return priority;
            }

            int severity = right.Definition.Severity.CompareTo(left.Definition.Severity);
            return severity != 0 ? severity : left.Id.CompareTo(right.Id);
        }

        private static int CompareFeedbackCandidates(
            NotificationDefinition left,
            NotificationDefinition right)
        {
            int severity = right.Severity.CompareTo(left.Severity);
            if (severity != 0)
            {
                return severity;
            }

            int priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : left.Id.CompareTo(right.Id);
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NotificationStore));
            }
        }
    }
}
