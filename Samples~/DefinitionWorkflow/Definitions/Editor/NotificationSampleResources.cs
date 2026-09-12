using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Deucarian.Notifications.Samples.DefinitionWorkflow.Editor
{
    /// <summary>Imports Unity's own TMP essentials once when this sample is imported into an empty project.</summary>
    internal static class NotificationSampleResources
    {
        [InitializeOnLoadMethod]
        private static void Schedule() => EditorApplication.delayCall += Ensure;

        private static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Resources.Load<TMP_Settings>("TMP Settings") != null) return;
            const string attempted = "Deucarian.NotificationSample.ImportedTmpEssentials";
            if (SessionState.GetBool(attempted, false)) return;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            string archive = package?.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage";
            if (!File.Exists(archive)) throw new InvalidOperationException("Import TMP Essential Resources from Unity's TextMeshPro menu before playing the notification sample.");
            SessionState.SetBool(attempted, true);
            AssetDatabase.ImportPackage(archive, false);
        }
    }
}
