using System;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    [Serializable]
    public sealed class NotificationLabRecipeData
    {
        public string title = "Example warning";
        public string body = "This is a test notification. Resolve it to simulate recovery.";
        public NotificationSeverity severity = NotificationSeverity.Warning;
        public NotificationLifetimeKind lifetime;
        public float lifetimeSeconds = 5;
        public float activationDelay;
        public float recoveryDelay = 1;
        public NotificationPresentationSettings presentation = NotificationPresentationSettings.Default;
        public DeucarianAudioExperience experience = DeucarianAudioExperience.XR;
        public bool sound = true;
        public string paletteGuid;
    }

    /// <summary>Reusable test inputs only: never stores a live connection or injected notification instances.</summary>
    [CreateAssetMenu(menuName = "Deucarian/Notifications/Test Recipe", fileName = "Notification Test Recipe")]
    public sealed class NotificationLabRecipe : ScriptableObject
    {
        public NotificationLabRecipeData settings = new NotificationLabRecipeData();
    }
}
