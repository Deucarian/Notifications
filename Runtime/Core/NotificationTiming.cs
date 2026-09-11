using System;
using System.Collections.Generic;
using System.Threading;
using Deucarian.Diagnostics;

namespace Deucarian.Notifications
{
    public interface INotificationClock
    {
        double NowSeconds { get; }
    }

    public sealed class StopwatchNotificationClock : INotificationClock
    {
        private readonly System.Diagnostics.Stopwatch stopwatch =
            System.Diagnostics.Stopwatch.StartNew();

        public double NowSeconds => stopwatch.Elapsed.TotalSeconds;
    }

    public readonly struct NotificationTimingPolicy
    {
        public NotificationTimingPolicy(double activationDebounceSeconds, double recoveryDebounceSeconds)
        {
            if (activationDebounceSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(activationDebounceSeconds));
            }

            if (recoveryDebounceSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(recoveryDebounceSeconds));
            }

            ActivationDebounceSeconds = activationDebounceSeconds;
            RecoveryDebounceSeconds = recoveryDebounceSeconds;
        }

        public double ActivationDebounceSeconds { get; }
        public double RecoveryDebounceSeconds { get; }
    }

    public readonly struct NotificationConditionSample
    {
        public NotificationConditionSample(
            NotificationDefinition definition,
            bool isUnhealthy,
            NotificationTimingPolicy timing)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            IsUnhealthy = isUnhealthy;
            Timing = timing;
        }

        public NotificationDefinition Definition { get; }
        public bool IsUnhealthy { get; }
        public NotificationTimingPolicy Timing { get; }
    }

    /// <summary>Converts debounced health conditions into atomic store commands.</summary>
    public sealed class NotificationEpisodeController : IDisposable
    {
        private static long nextRuntimeId;
        private sealed class EpisodeState
        {
            public NotificationDefinition Definition;
            public NotificationTimingPolicy Timing;
            public bool RawUnhealthy;
            public bool IsActive;
            public double RawChangedAt;
            public double ActivatedAt;
            public bool TimedOut;
        }

        private readonly INotificationCommands store;
        private readonly INotificationClock clock;
        private readonly Dictionary<NotificationId, EpisodeState> states =
            new Dictionary<NotificationId, EpisodeState>();
        private readonly DiagnosticProviderRegistration diagnosticsRegistration;
        private int pendingActivationCount;
        private int pendingRecoveryCount;
        private bool disposed;

        public NotificationEpisodeController(NotificationStore store, INotificationClock clock)
            : this((INotificationCommands)store, clock) { }

        public NotificationEpisodeController(INotificationCommands store, INotificationClock clock)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            string runtimeId = Interlocked.Increment(ref nextRuntimeId).ToString();
            diagnosticsRegistration = DiagnosticProviderRegistry.Register(
                new NotificationSchedulerDiagnosticProvider(
                    "notifications.scheduler." + runtimeId,
                    CaptureDiagnostics));
        }

        public void EvaluateBatch(IEnumerable<NotificationConditionSample> samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            ThrowIfDisposed();
            double now = clock.NowSeconds;
            foreach (NotificationConditionSample sample in samples)
            {
                UpdateState(sample, now);
            }

            ApplyDueTransitions(now);
        }

        public void Tick()
        {
            ThrowIfDisposed();
            ApplyDueTransitions(clock.NowSeconds);
        }

        public int PendingCount => pendingActivationCount + pendingRecoveryCount;

        /// <summary>Resolves and forgets an individual episode, including a pending activation.</summary>
        public void Resolve(NotificationId id)
        {
            ThrowIfDisposed();
            if (id.IsEmpty) throw new ArgumentException("A notification ID is required.", nameof(id));
            states.Remove(id);
            store.ApplyBatch(new[] { NotificationCommand.Resolve(id) }, clock.NowSeconds);
            ApplyDueTransitions(clock.NowSeconds);
        }

        public void Reset(bool resolveActive = true)
        {
            ThrowIfDisposed();
            states.Clear();
            pendingActivationCount = 0;
            pendingRecoveryCount = 0;
            if (resolveActive)
            {
                store.ClearAll(clock.NowSeconds);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            states.Clear();
            pendingActivationCount = 0;
            pendingRecoveryCount = 0;
            diagnosticsRegistration?.Dispose();
        }

        internal NotificationSchedulerDiagnosticState CaptureDiagnostics()
        {
            return new NotificationSchedulerDiagnosticState(
                states.Count,
                pendingActivationCount,
                pendingRecoveryCount,
                disposed);
        }

        private void UpdateState(NotificationConditionSample sample, double now)
        {
            if (!states.TryGetValue(sample.Definition.Id, out EpisodeState state))
            {
                state = new EpisodeState
                {
                    Definition = sample.Definition,
                    Timing = sample.Timing,
                    RawUnhealthy = sample.IsUnhealthy,
                    IsActive = false,
                    RawChangedAt = now
                };
                states.Add(sample.Definition.Id, state);
                return;
            }

            state.Definition = sample.Definition;
            state.Timing = sample.Timing;
            // A new explicit sample may restart an expired notice; ticking alone never reactivates it.
            if (state.TimedOut && sample.IsUnhealthy)
            {
                state.TimedOut = false;
                state.RawChangedAt = now;
            }
            if (state.RawUnhealthy != sample.IsUnhealthy)
            {
                state.RawUnhealthy = sample.IsUnhealthy;
                state.RawChangedAt = now;
            }
        }

        private void ApplyDueTransitions(double now)
        {
            List<NotificationCommand> commands = new List<NotificationCommand>();
            int pendingActivations = 0;
            int pendingRecoveries = 0;
            foreach (KeyValuePair<NotificationId, EpisodeState> pair in states)
            {
                EpisodeState state = pair.Value;
                double elapsed = Math.Max(0d, now - state.RawChangedAt);
                if (state.IsActive && state.Definition.Lifetime.Kind == NotificationLifetimeKind.Timed &&
                    now - state.ActivatedAt >= state.Definition.Lifetime.Seconds)
                {
                    state.IsActive = false;
                    state.TimedOut = true;
                    commands.Add(NotificationCommand.Resolve(pair.Key));
                    continue;
                }
                if (state.TimedOut) continue;
                if (state.RawUnhealthy)
                {
                    if (!state.IsActive && elapsed >= state.Timing.ActivationDebounceSeconds)
                    {
                        state.IsActive = true;
                        state.ActivatedAt = now;
                        commands.Add(NotificationCommand.Activate(state.Definition));
                    }
                    else if (!state.IsActive)
                    {
                        pendingActivations++;
                    }
                    else if (state.IsActive)
                    {
                        commands.Add(NotificationCommand.Activate(state.Definition));
                    }
                }
                else if (state.IsActive && elapsed >= state.Timing.RecoveryDebounceSeconds)
                {
                    state.IsActive = false;
                    commands.Add(NotificationCommand.Resolve(pair.Key));
                }
                else if (!state.RawUnhealthy && state.IsActive)
                {
                    pendingRecoveries++;
                }
            }

            pendingActivationCount = pendingActivations;
            pendingRecoveryCount = pendingRecoveries;

            if (commands.Count > 0)
            {
                store.ApplyBatch(commands, now);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(NotificationEpisodeController));
            }
        }
    }
}
