using System;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Project prefab choice; a null override deliberately follows the current package default.</summary>
    public sealed class NotificationViewSettings : ScriptableObject
    {
        public const string ResourcePath = "Deucarian/Notifications/NotificationViewSettings";
        [SerializeField] private NotificationRowView customPrefab;
        [SerializeField] private bool hasPresentation;
        [SerializeField] private NotificationPresentationSettings presentation = NotificationPresentationSettings.Default;
        public bool HasPresentation => hasPresentation;
        public NotificationPresentationSettings Presentation => presentation.Sanitized();
        public NotificationRowView CustomPrefab => customPrefab;
        public bool UsesDefault => customPrefab == null;
        public event Action Changed;
        public static NotificationViewSettings Load() => Resources.Load<NotificationViewSettings>(ResourcePath);

        public static NotificationPresentationSettings ResolvePresentation(NotificationPresentationSettings fallback)
        {
            var settings = Load();
            return settings != null && settings.HasPresentation ? settings.Presentation : fallback.Sanitized();
        }

        public void SetPresentation(NotificationPresentationSettings value)
        {
            value = value.Sanitized();
            if (hasPresentation && presentation.Equals(value)) return;
            presentation = value;
            hasPresentation = true;
            Changed?.Invoke();
        }

        public void UseCustomPrefab(NotificationRowView prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (customPrefab == prefab) return;
            customPrefab = prefab;
            Changed?.Invoke();
        }

        public void ChangeToDefault()
        {
            if (customPrefab == null) return;
            customPrefab = null;
            Changed?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Subscribers may replace scene objects; wait until serialization has finished.
            UnityEditor.EditorApplication.delayCall -= NotifyInspectorChange;
            UnityEditor.EditorApplication.delayCall += NotifyInspectorChange;
        }

        private void NotifyInspectorChange()
        {
            if (this != null) Changed?.Invoke();
        }

        private void OnDisable() => UnityEditor.EditorApplication.delayCall -= NotifyInspectorChange;
#endif
    }
}
