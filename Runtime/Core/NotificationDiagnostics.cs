using System;
using Deucarian.Diagnostics;

namespace Deucarian.Notifications
{
    internal readonly struct NotificationDiagnosticState
    {
        public NotificationDiagnosticState(
            int activeCount,
            NotificationSeverity highestSeverity,
            long transitionCount,
            long feedbackRequestCount,
            long feedbackFailureCount,
            bool disposed)
        {
            ActiveCount = activeCount;
            HighestSeverity = highestSeverity;
            TransitionCount = transitionCount;
            FeedbackRequestCount = feedbackRequestCount;
            FeedbackFailureCount = feedbackFailureCount;
            Disposed = disposed;
        }

        public int ActiveCount { get; }
        public NotificationSeverity HighestSeverity { get; }
        public long TransitionCount { get; }
        public long FeedbackRequestCount { get; }
        public long FeedbackFailureCount { get; }
        public bool Disposed { get; }
    }

    internal sealed class NotificationDiagnosticProvider : IDiagnosticProvider
    {
        private readonly string providerId;
        private readonly Func<NotificationDiagnosticState> capture;

        public NotificationDiagnosticProvider(string providerId, Func<NotificationDiagnosticState> capture)
        {
            this.providerId = providerId;
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
        }

        public string ProviderId => providerId;
        public string DisplayName => "Notifications";

        public void Collect(DiagnosticReportBuilder builder)
        {
            NotificationDiagnosticState state = capture();
            DiagnosticSection section = builder.AddSection(providerId, DisplayName);
            section.AddItem("active_count", "Active count", state.ActiveCount.ToString());
            section.AddItem(
                "highest_severity",
                "Highest severity",
                state.HighestSeverity.ToString());
            section.AddItem("transitions", "Transitions", state.TransitionCount.ToString());
            section.AddItem(
                "feedback_requests",
                "Feedback requests",
                state.FeedbackRequestCount.ToString());
            section.AddItem(
                "feedback_failures",
                "Feedback failures",
                state.FeedbackFailureCount.ToString(),
                state.FeedbackFailureCount > 0
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Success);
            section.AddItem(
                "lifecycle",
                "Lifecycle",
                state.Disposed ? "Disposed" : "Active",
                state.Disposed ? DiagnosticSeverity.Info : DiagnosticSeverity.Success);
        }
    }

    internal readonly struct NotificationSchedulerDiagnosticState
    {
        public NotificationSchedulerDiagnosticState(
            int trackedCount,
            int pendingActivationCount,
            int pendingRecoveryCount,
            bool disposed)
        {
            TrackedCount = trackedCount;
            PendingActivationCount = pendingActivationCount;
            PendingRecoveryCount = pendingRecoveryCount;
            Disposed = disposed;
        }

        public int TrackedCount { get; }
        public int PendingActivationCount { get; }
        public int PendingRecoveryCount { get; }
        public bool Disposed { get; }
    }

    internal sealed class NotificationSchedulerDiagnosticProvider : IDiagnosticProvider
    {
        private readonly string providerId;
        private readonly Func<NotificationSchedulerDiagnosticState> capture;

        public NotificationSchedulerDiagnosticProvider(
            string providerId,
            Func<NotificationSchedulerDiagnosticState> capture)
        {
            this.providerId = providerId;
            this.capture = capture ?? throw new ArgumentNullException(nameof(capture));
        }

        public string ProviderId => providerId;
        public string DisplayName => "Notification scheduler";

        public void Collect(DiagnosticReportBuilder builder)
        {
            NotificationSchedulerDiagnosticState state = capture();
            DiagnosticSection section = builder.AddSection(providerId, DisplayName);
            section.AddItem("tracked_count", "Tracked conditions", state.TrackedCount.ToString());
            section.AddItem(
                "pending_activation_count",
                "Pending activations",
                state.PendingActivationCount.ToString());
            section.AddItem(
                "pending_recovery_count",
                "Pending recoveries",
                state.PendingRecoveryCount.ToString());
            section.AddItem(
                "scheduler_state",
                "Scheduler state",
                state.Disposed ? "Disposed" : "Active",
                state.Disposed ? DiagnosticSeverity.Info : DiagnosticSeverity.Success);
        }
    }
}
