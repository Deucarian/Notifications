using System;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    [Serializable]
    internal sealed class NotificationLabRecipePanel
    {
        [SerializeField] private NotificationLabRecipe recipe;
        internal void Bind(DeucarianEditorWorkspaceForm form, Func<NotificationLabRecipeData> capture,
            Action<NotificationLabRecipeData> apply)
        {
            form.Asset("lab-recipe", "Recipe", typeof(NotificationLabRecipe), () => recipe, value => recipe = (NotificationLabRecipe)value);
            form.Action("lab-load-recipe", "Load recipe", () => apply(recipe.settings), () => recipe != null);
            form.Action("lab-save-recipe", "Save as recipe…", () => Save(capture()));
            form.Note(() => "Recipes save inputs, not live messages, connections or application state.");
        }

        private void Save(NotificationLabRecipeData inputs)
        {
            string path = EditorUtility.SaveFilePanelInProject("Save notification recipe",
                "Notification Test Recipe", "asset", "Choose where to keep these test inputs.");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                recipe = NotificationLabRecipeStorage.SaveNew(path, inputs);
                DeucarianEditorSelection.SelectAndPing(recipe);
            }
            catch (Exception exception)
            {
                DeucarianEditorActionErrors.Show("Recipe was not saved", exception);
            }
        }
    }
}
