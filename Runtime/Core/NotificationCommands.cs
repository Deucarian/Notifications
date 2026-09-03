using System;

namespace Deucarian.Notifications
{
    public readonly struct NotificationCommand
    {
        private NotificationCommand(
            NotificationId id,
            NotificationDefinition definition,
            bool active)
        {
            Id = id;
            Definition = definition;
            IsActive = active;
        }

        public NotificationId Id { get; }
        public NotificationDefinition Definition { get; }
        public bool IsActive { get; }

        public static NotificationCommand Activate(NotificationDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new NotificationCommand(definition.Id, definition, true);
        }

        public static NotificationCommand Resolve(NotificationId id)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("A notification ID is required.", nameof(id));
            }

            return new NotificationCommand(id, null, false);
        }

        public static NotificationCommand Resolve(string id) => Resolve(new NotificationId(id));
    }

    public readonly struct NotificationFeedbackRequest
    {
        public NotificationFeedbackRequest(
            string roleId,
            NotificationSeverity severity,
            int priority,
            int activatedCount)
        {
            RoleId = roleId ?? string.Empty;
            Severity = severity;
            Priority = priority;
            ActivatedCount = activatedCount;
        }

        public string RoleId { get; }
        public NotificationSeverity Severity { get; }
        public int Priority { get; }
        public int ActivatedCount { get; }
    }

    public interface INotificationFeedbackSink
    {
        bool TryRequestFeedback(NotificationFeedbackRequest request);
    }

    public sealed class NotificationChangedEventArgs : EventArgs
    {
        internal NotificationChangedEventArgs(
            NotificationSnapshot previous,
            NotificationSnapshot current,
            NotificationId[] activated,
            NotificationId[] resolved)
        {
            Previous = previous;
            Current = current;
            Activated = activated ?? Array.Empty<NotificationId>();
            Resolved = resolved ?? Array.Empty<NotificationId>();
        }

        public NotificationSnapshot Previous { get; }
        public NotificationSnapshot Current { get; }
        public System.Collections.Generic.IReadOnlyList<NotificationId> Activated { get; }
        public System.Collections.Generic.IReadOnlyList<NotificationId> Resolved { get; }
    }
}

