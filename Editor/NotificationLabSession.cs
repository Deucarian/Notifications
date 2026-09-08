using System;
using System.Collections.Generic;
using Deucarian.Theming;

namespace Deucarian.Notifications.Editor
{
    /// <summary>An isolated editor session composed from the production lifecycle.</summary>
    internal sealed class NotificationLabSession : IDisposable, INotificationFeedbackSink
    {
        private readonly NotificationEpisodeController controller;
        private readonly INotificationFeedbackSink feedback;
        private readonly Dictionary<NotificationId, NotificationConditionSample> conditions =
            new Dictionary<NotificationId, NotificationConditionSample>();
        private bool disposed;

        public NotificationLabSession(INotificationClock clock, INotificationFeedbackSink feedback)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            this.feedback = feedback;
            Store = new NotificationStore(this);
            controller = new NotificationEpisodeController(Store, clock);
        }

        public NotificationStore Store { get; }
        public int PingCount { get; private set; }
        public int LastBatchSize { get; private set; }
        public int PendingCount => controller.PendingCount;

        public void Show(NotificationDefinition definition, NotificationTimingPolicy timing)
        {
            ShowBatch(new[] { definition }, timing);
        }

        public void ShowBatch(IEnumerable<NotificationDefinition> definitions, NotificationTimingPolicy timing)
        {
            ThrowIfDisposed();
            var batch = new List<NotificationConditionSample>();
            foreach (NotificationDefinition definition in definitions)
            {
                var sample = new NotificationConditionSample(definition, true, timing);
                conditions[definition.Id] = sample;
                batch.Add(sample);
            }
            controller.EvaluateBatch(batch);
        }

        public bool IsRecovering(NotificationId id)
        {
            return conditions.TryGetValue(id, out NotificationConditionSample sample) && !sample.IsUnhealthy;
        }

        public void Resolve(NotificationId id)
        {
            ThrowIfDisposed();
            if (!conditions.TryGetValue(id, out NotificationConditionSample previous)) return;
            var sample = new NotificationConditionSample(previous.Definition, false, previous.Timing);
            conditions[id] = sample;
            controller.EvaluateBatch(new[] { sample });
        }

        public void ResolveAll()
        {
            ThrowIfDisposed();
            var batch = new List<NotificationConditionSample>();
            foreach (NotificationConditionSample previous in conditions.Values)
                batch.Add(new NotificationConditionSample(previous.Definition, false, previous.Timing));
            foreach (NotificationConditionSample sample in batch) conditions[sample.Definition.Id] = sample;
            controller.EvaluateBatch(batch);
        }

        public void Tick()
        {
            ThrowIfDisposed();
            controller.Tick();
        }

        public void Reset()
        {
            ThrowIfDisposed();
            controller.Reset();
            conditions.Clear();
            PingCount = 0;
            LastBatchSize = 0;
        }

        public bool TryRequestFeedback(NotificationFeedbackRequest request)
        {
            PingCount++;
            LastBatchSize = request.ActivatedCount;
            return feedback != null && feedback.TryRequestFeedback(request);
        }

        public static NotificationDefinition Example(NotificationSeverity severity, NotificationLifetime lifetime = default)
        {
            return new NotificationDefinition(
                "lab.example." + severity.ToString().ToLowerInvariant(), severity,
                "Example " + severity.ToString().ToLowerInvariant(),
                "This is a test message. Resolve it to simulate recovery.",
                (int)severity * 10, FeedbackRole(severity), lifetime);
        }

        public static string FeedbackRole(NotificationSeverity severity)
        {
            switch (severity)
            {
                case NotificationSeverity.Success: return DeucarianBuiltinAudioRoleIds.Success;
                case NotificationSeverity.Warning: return DeucarianBuiltinAudioRoleIds.Warning;
                case NotificationSeverity.Error: return DeucarianBuiltinAudioRoleIds.Error;
                default: return DeucarianBuiltinAudioRoleIds.Info;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            controller.Dispose();
            Store.Dispose();
            conditions.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NotificationLabSession));
        }
    }
}
