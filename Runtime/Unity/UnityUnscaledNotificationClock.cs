using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Unity clock unaffected by <see cref="Time.timeScale"/>.</summary>
    public sealed class UnityUnscaledNotificationClock : INotificationClock
    {
        public double NowSeconds => Time.realtimeSinceStartupAsDouble;
    }
}

