using System;
using Deucarian.Common;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    public static class NotificationLabRecipeStorage
    {
        private const string DraftKey = "notifications.lab.draft";

        public static NotificationLabRecipeData Copy(NotificationLabRecipeData value)
            => value == null ? new NotificationLabRecipeData() : JsonUtility.FromJson<NotificationLabRecipeData>(JsonUtility.ToJson(value));

        public static void SaveDraft(NotificationLabRecipeData value)
            => DeucarianEditorProjectPreferences.SetString(DraftKey, JsonUtility.ToJson(Copy(value)));

        public static NotificationLabRecipeData LoadDraft()
        {
            string json = DeucarianEditorProjectPreferences.GetString(DraftKey);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonUtility.FromJson<NotificationLabRecipeData>(json); }
            catch (ArgumentException) { DeucarianEditorProjectPreferences.Delete(DraftKey); return null; }
        }

        public static NotificationLabRecipe SaveNew(string path, NotificationLabRecipeData inputs)
        {
            path = (path ?? "").Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains("/../") ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Choose an .asset file inside the project's Assets folder.", nameof(path));
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || System.IO.File.Exists(path))
                throw new InvalidOperationException("That file already exists. Choose a new name to preserve it.");

            var recipe = ScriptableObject.CreateInstance<NotificationLabRecipe>();
            try
            {
                recipe.settings = Copy(inputs);
                AssetDatabase.CreateAsset(recipe, path);
                EditorUtility.SetDirty(recipe);
                AssetDatabase.SaveAssetIfDirty(recipe);
                return recipe;
            }
            catch
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == recipe) AssetDatabase.DeleteAsset(path);
                else if (!AssetDatabase.Contains(recipe)) UnityObjectUtility.DestroySafely(recipe);
                throw;
            }
        }
    }
}
