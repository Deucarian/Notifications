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
        [SerializeField] private bool expanded;

        internal void Draw(Func<NotificationLabRecipeData> capture,
            Action<NotificationLabRecipeData> apply, Action<NotificationLifetimeKind> add,
            Action overflow)
        {
            expanded = EditorGUILayout.Foldout(expanded, "Test recipes", true);
            if (!expanded) return;
            recipe = (NotificationLabRecipe)EditorGUILayout.ObjectField("Recipe", recipe, typeof(NotificationLabRecipe), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Load recipe", recipe != null)) apply(recipe.settings);
                if (DeucarianEditorButtons.Secondary("Save as recipe…")) Save(capture());
            }
            if (DeucarianEditorButtons.Secondary("Timed notice · 5 seconds")) add(NotificationLifetimeKind.Timed);
            if (DeucarianEditorButtons.Secondary("Persistent warning · resolve manually")) add(NotificationLifetimeKind.UntilResolved);
            if (DeucarianEditorButtons.Secondary("Overflow · 10 mixed messages")) overflow();
            EditorGUILayout.LabelField("Recipes save inputs, not live messages, connections or application state.", EditorStyles.wordWrappedMiniLabel);
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
