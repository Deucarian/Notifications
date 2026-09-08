using System;

namespace Deucarian.Notifications
{
    /// <summary>Limits presentation only. Overflow stays in the authoritative store.</summary>
    public static class NotificationVisibility
    {
        public static NotificationSnapshot Select(NotificationSnapshot source, int maxVisible)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxVisible < 1) throw new ArgumentOutOfRangeException(nameof(maxVisible));
            var ordered = new NotificationItem[source.Count];
            for (int i = 0; i < ordered.Length; i++) ordered[i] = source[i];
            Array.Sort(ordered, Compare);
            int count = Math.Min(maxVisible, ordered.Length);
            var visible = new NotificationItem[count];
            Array.Copy(ordered, visible, count);
            return new NotificationSnapshot(source.Version, visible);
        }

        private static int Compare(NotificationItem a, NotificationItem b)
        {
            int value = b.Definition.Priority.CompareTo(a.Definition.Priority);
            if (value == 0) value = b.Definition.Severity.CompareTo(a.Definition.Severity);
            if (value == 0) value = a.ActivatedAtSeconds.CompareTo(b.ActivatedAtSeconds);
            if (value == 0) value = a.Episode.CompareTo(b.Episode);
            return value == 0 ? a.Id.CompareTo(b.Id) : value;
        }
    }
}
