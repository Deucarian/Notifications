using System;

namespace Deucarian.Notifications
{
    public enum NotificationLifetimeKind { UntilResolved, Timed }

    /// <summary>Timed lifetime starts at activation, never at view creation or animation completion.</summary>
    public readonly struct NotificationLifetime : IEquatable<NotificationLifetime>
    {
        private NotificationLifetime(double seconds) { Seconds = seconds; }
        public double Seconds { get; }
        public NotificationLifetimeKind Kind => Seconds > 0 ? NotificationLifetimeKind.Timed : NotificationLifetimeKind.UntilResolved;
        public static NotificationLifetime UntilResolved => default;
        public static NotificationLifetime Timed(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(seconds), "A timed notification needs a finite positive duration.");
            return new NotificationLifetime(seconds);
        }
        public bool Equals(NotificationLifetime other) => Seconds.Equals(other.Seconds);
        public override bool Equals(object other) => other is NotificationLifetime value && Equals(value);
        public override int GetHashCode() => Seconds.GetHashCode();
    }
}
