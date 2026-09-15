using System;
using Deucarian.Editor.Definitions;
using Deucarian.Theming.Editor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    public sealed partial class DeucarianNotificationLabWindow
    {
        [SerializeField] private DeucarianDefinitionPanelState definitionState = new DeucarianDefinitionPanelState();
        internal DeucarianDefinitionPanelState DefinitionState => definitionState;

        /// <summary>Only authoring inputs survive reload; runtime connections and injected messages are always released.</summary>
        public string CaptureReloadState() => JsonUtility.ToJson(new ReloadState
        {
            inputs = CaptureDraft(), tab = selectedTab, definitions = definitionState,
            recipe = DeucarianThemingEditorSettings.GetAssetGuid(recipePanel.Selection)
        });

        public void RestoreReloadState(string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            var value = JsonUtility.FromJson<ReloadState>(state);
            if (value == null) return;
            ApplyDraft(value.inputs);
            SelectedTab = value.tab;
            definitionState = value.definitions ?? new DeucarianDefinitionPanelState();
            recipePanel.Selection = DeucarianThemingEditorSettings.LoadAssetByGuid<NotificationLabRecipe>(value.recipe);
            draftInitialized = true;
        }

        [Serializable]
        private sealed class ReloadState
        {
            public NotificationLabRecipeData inputs;
            public int tab;
            public string recipe;
            public DeucarianDefinitionPanelState definitions;
        }
    }
}
