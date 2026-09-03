using System;

namespace Deucarian.Notifications
{
    /// <summary>Stable identity for one independently resolvable notification.</summary>
    public readonly struct NotificationId :
        IEquatable<NotificationId>,
        IComparable<NotificationId>
    {
        public NotificationId(string value)
        {
            string normalized = Normalize(value);
            if (!IsValid(normalized))
            {
                throw new ArgumentException(
                    "Notification IDs must be non-empty lowercase identifiers without whitespace.",
                    nameof(value));
            }

            Value = normalized;
        }

        public string Value { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public int CompareTo(NotificationId other)
        {
            return string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(NotificationId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is NotificationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }

        public static bool operator ==(NotificationId left, NotificationId right) => left.Equals(right);
        public static bool operator !=(NotificationId left, NotificationId right) => !left.Equals(right);

        public static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim();
        }

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsWhiteSpace(character) || char.IsUpper(character))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

