using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    public sealed partial class DeucarianNotificationLabWindow
    {
        [SerializeField] private NotificationLabRecipePanel recipePanel = new NotificationLabRecipePanel();
        internal NotificationLabRecipePanel Recipes => recipePanel;

        private NotificationLabRecipeData CaptureDraft() => new NotificationLabRecipeData
        {
            title = messageTitle, body = messageBody, severity = severity, lifetime = lifetimeKind,
            lifetimeSeconds = lifetimeSeconds, activationDelay = activationDelay, recoveryDelay = recoveryDelay,
            presentation = presentationSettings, experience = experience, sound = soundEnabled,
            paletteGuid = paletteSet == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(paletteSet))
        };

        private void ApplyDraft(NotificationLabRecipeData value)
        {
            if (value == null) return;
            var nextPresentation = value.presentation.Sanitized();
            bool presentationChanged = !presentationSettings.Equals(nextPresentation);
            messageTitle = value.title ?? "Example warning";
            messageBody = value.body ?? "";
            severity = value.severity;
            lifetimeKind = value.lifetime;
            lifetimeSeconds = Mathf.Max(0.1f, SanitizeDelay(value.lifetimeSeconds));
            activationDelay = SanitizeDelay(value.activationDelay);
            recoveryDelay = SanitizeDelay(value.recoveryDelay);
            presentationSettings = nextPresentation;
            experience = value.experience;
            soundEnabled = value.sound;
            paletteSet = string.IsNullOrEmpty(value.paletteGuid) ? null :
                AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(AssetDatabase.GUIDToAssetPath(value.paletteGuid));
            audio?.Configure(paletteSet, experience, soundEnabled && runtimeConnection == null);
            if (presentationChanged) runtimeConnection?.ConfigurePresentation(presentationSettings);
        }

        private void SaveDraft() => NotificationLabRecipeStorage.SaveDraft(CaptureDraft());

        private void RestoreDraft()
        {
            ApplyDraft(NotificationLabRecipeStorage.LoadDraft());
        }


    }
}
