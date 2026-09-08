using System;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabRecipeTests
    {
        [Test]
        public void DraftRoundTripHasNoRuntimeConnectionAndDoesNotShareMutableInputs()
        {
            const string key = "notifications.lab.draft";
            string previous = DeucarianEditorProjectPreferences.GetString(key);
            try
            {
                var input = new NotificationLabRecipeData { title = "Timed check", lifetime = NotificationLifetimeKind.Timed, lifetimeSeconds = 3 };
                NotificationLabRecipeStorage.SaveDraft(input);
                input.title = "Changed later";
                var restored = NotificationLabRecipeStorage.LoadDraft();
                Assert.That(restored.title, Is.EqualTo("Timed check"));
                Assert.That(restored.lifetimeSeconds, Is.EqualTo(3));
                string json = JsonUtility.ToJson(restored);
                Assert.That(json, Does.Not.Contain("runtimeConnection").And.Not.Contain("session").And.Not.Contain("instanceID"));
                DeucarianEditorProjectPreferences.SetString(key, "{ broken json");
                Assert.That(NotificationLabRecipeStorage.LoadDraft(), Is.Null);
            }
            finally { DeucarianEditorProjectPreferences.SetString(key, previous); }
        }

        [Test]
        public void SavingARecipeCreatesAnIndependentAssetAndNeverOverwrites()
        {
            string folder = "Assets/NotificationRecipeTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            string path = folder + "/Warning.asset";
            try
            {
                var input = new NotificationLabRecipeData { title = "Original" };
                var recipe = NotificationLabRecipeStorage.SaveNew(path, input);
                input.title = "Changed";
                Assert.That(recipe.settings.title, Is.EqualTo("Original"));
                Assert.Throws<InvalidOperationException>(() => NotificationLabRecipeStorage.SaveNew(path, input));
                Assert.That(AssetDatabase.LoadAssetAtPath<NotificationLabRecipe>(path).settings.title, Is.EqualTo("Original"));
                Assert.Throws<ArgumentException>(() => NotificationLabRecipeStorage.SaveNew("Packages/Bad.asset", input));
            }
            finally { AssetDatabase.DeleteAsset(folder); }
        }
    }
}
