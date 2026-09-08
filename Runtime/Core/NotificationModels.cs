using System;
using System.Collections.Generic;

namespace Deucarian.Notifications
{
    public enum NotificationSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>Immutable content and ordering policy for a keyed notification.</summary>
    public sealed class NotificationDefinition : IEquatable<NotificationDefinition>
    {
        public NotificationDefinition(
            string id,
            NotificationSeverity severity,
            string title,
            string body,
            int priority = 0,
            string feedbackRoleId = null,
            NotificationLifetime lifetime = default)
            : this(new NotificationId(id), severity, title, body, priority, feedbackRoleId, lifetime)
        {
        }

        public NotificationDefinition(
            NotificationId id,
            NotificationSeverity severity,
            string title,
            string body,
            int priority = 0,
            string feedbackRoleId = null,
            NotificationLifetime lifetime = default)
        {
            if (id.IsEmpty)
            {
                throw new ArgumentException("A notification ID is required.", nameof(id));
            }

            Id = id;
            Severity = severity;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Priority = priority;
            Lifetime = lifetime;
            FeedbackRoleId = string.IsNullOrWhiteSpace(feedbackRoleId)
                ? string.Empty
                : feedbackRoleId.Trim();
        }

        public NotificationId Id { get; }
        public NotificationSeverity Severity { get; }
        public string Title { get; }
        public string Body { get; }
        public int Priority { get; }
        public string FeedbackRoleId { get; }
        public NotificationLifetime Lifetime { get; }

        public bool Equals(NotificationDefinition other)
        {
            return other != null &&
                   Id == other.Id &&
                   Severity == other.Severity &&
                   Priority == other.Priority &&
                   Lifetime.Equals(other.Lifetime) &&
                   string.Equals(Title, other.Title, StringComparison.Ordinal) &&
                   string.Equals(Body, other.Body, StringComparison.Ordinal) &&
                   string.Equals(FeedbackRoleId, other.FeedbackRoleId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as NotificationDefinition);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Id.GetHashCode();
                hash = (hash * 397) ^ (int)Severity;
                hash = (hash * 397) ^ Priority;
                hash = (hash * 397) ^ Lifetime.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Title);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Body);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(FeedbackRoleId);
                return hash;
            }
        }
    }

    /// <summary>One active notification episode.</summary>
    public readonly struct NotificationItem
    {
        public NotificationItem(NotificationDefinition definition, long episode, double activatedAtSeconds)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Episode = episode;
            ActivatedAtSeconds = activatedAtSeconds;
        }

        public NotificationDefinition Definition { get; }
        public NotificationId Id => Definition.Id;
        public long Episode { get; }
        public double ActivatedAtSeconds { get; }
    }

    /// <summary>Immutable ordered view of all active notifications.</summary>
    public sealed class NotificationSnapshot
    {
        private readonly NotificationItem[] items;
        private readonly IReadOnlyList<NotificationItem> readonlyItems;

        internal NotificationSnapshot(long version, NotificationItem[] source)
        {
            Version = version;
            items = source != null
                ? (NotificationItem[])source.Clone()
                : Array.Empty<NotificationItem>();
            readonlyItems = Array.AsReadOnly(items);
        }

        public static NotificationSnapshot Empty { get; } =
            new NotificationSnapshot(0, Array.Empty<NotificationItem>());

        public long Version { get; }
        public int Count => items.Length;
        public IReadOnlyList<NotificationItem> Items => readonlyItems;
        public NotificationItem this[int index] => items[index];

        public bool TryGet(NotificationId id, out NotificationItem item)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].Id == id)
                {
                    item = items[i];
                    return true;
                }
            }

            item = default;
            return false;
        }
    }
}
