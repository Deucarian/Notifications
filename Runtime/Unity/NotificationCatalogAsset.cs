using System;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Explicit serialized build references. No runtime asset or assembly scanning.</summary>
    public sealed class NotificationCatalogAsset : ScriptableObject, INotificationDefinitions
    {
        public const string DefaultResourcePath = "Deucarian/Notifications/ProjectNotificationCatalog";
        [SerializeField] private NotificationDefinitionAsset[] definitions = Array.Empty<NotificationDefinitionAsset>();
        public bool TryGet(NotificationKey key, out NotificationDefinition definition)
        {
            definition = null;
            if (key == null) return false;
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            NotificationDefinitionAsset selected = null;
            foreach (var asset in definitions)
            {
                if (asset == null)
                    throw new InvalidOperationException("The notification catalog contains a missing definition. Repair it in the Notification Lab.");
                if (string.IsNullOrWhiteSpace(asset.Id) || !ids.Add(asset.Id))
                    throw new InvalidOperationException("The notification catalog contains an empty or duplicate ID. Repair it in the Notification Lab.");
                if (asset.Id == key.Id) selected = asset;
            }
            if (selected == null) return false;
            definition = selected.CreateDefinition();
            return true;
        }
    }
}
