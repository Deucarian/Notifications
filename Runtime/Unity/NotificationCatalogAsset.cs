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
            foreach (var asset in definitions)
                if (asset != null && asset.Id == key.Id) { definition = asset.CreateDefinition(); return true; }
            return false;
        }
    }
}
