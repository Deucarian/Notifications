using System;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Canonical package prefab references. Defaults remain linked to package updates.</summary>
    public static class NotificationViewDefaults
    {
        public const string ListResourcePath = "Deucarian/Notifications/Defaults/DefaultNotificationList";
        public const string RowResourcePath = "Deucarian/Notifications/Defaults/DefaultNotificationRow";

        public static GameObject LoadListPrefab() => Resources.Load<GameObject>(ListResourcePath)
            ?? throw new InvalidOperationException("The package default notification list prefab is missing.");

        public static NotificationRowView LoadRowPrefab()
        {
            var prefab = Resources.Load<GameObject>(RowResourcePath);
            var row = prefab != null ? prefab.GetComponent<NotificationRowView>() : null;
            if (row == null) throw new InvalidOperationException("The package default notification row prefab is missing.");
            return row;
        }

        public static NotificationRowView ResolveRowPrefab(NotificationViewSettings settings = null)
        {
            settings = settings != null ? settings : NotificationViewSettings.Load();
            return settings != null && settings.CustomPrefab != null ? settings.CustomPrefab : LoadRowPrefab();
        }
    }
}
