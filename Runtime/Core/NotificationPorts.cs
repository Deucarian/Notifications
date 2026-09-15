using System;
using System.Collections.Generic;

namespace Deucarian.Notifications
{
    /// <summary>Observation-only access for presenters. It cannot inject or resolve notifications.</summary>
    public interface INotificationSource
    {
        NotificationSnapshot Snapshot { get; }
        event EventHandler<NotificationChangedEventArgs> SnapshotChanged;
    }

    /// <summary>Explicit mutation access for condition adapters and test injection.</summary>
    public interface INotificationCommands
    {
        void ValidateDefinition(NotificationDefinition definition);
        NotificationChangedEventArgs ApplyBatch(IEnumerable<NotificationCommand> commands, double nowSeconds);
        NotificationChangedEventArgs ClearAll(double nowSeconds = 0d);
    }
}
