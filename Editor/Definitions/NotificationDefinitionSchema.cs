using System;
using System.IO;
using System.Linq;
using Deucarian.Editor.Definitions;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor.Definitions
{
    [Serializable]
    public sealed class NotificationDefinitionSpec : DeucarianDefinitionSpec
    {
        [DefinitionField("severity")] public NotificationSeverity Severity = NotificationSeverity.Warning;
        [DefinitionField("title")] public string Title = "Connection lost";
        [DefinitionField("message")] public string Message = "Please reconnect your device.";
        [DefinitionField("priority")] public int Priority;
        [DefinitionField("lifetime")] public NotificationLifetimeKind Lifetime;
        [DefinitionField("durationSeconds")] public float DurationSeconds = 5;
        [DefinitionField("sound")] public NotificationSoundPolicy Sound = NotificationSoundPolicy.SeverityDefault;
        [DefinitionField("customSound")] public DeucarianAudioRole CustomSound;
    }

    public sealed class NotificationDefinitionSchema : DeucarianSerializedDefinitionSchema<NotificationDefinitionAsset, NotificationDefinitionSpec>
    {
        public override string Id => "notifications";
        public override string DisplayName => "Notifications";
        public override bool CanPreview => true;
        public override void Preview(ScriptableObject asset) => NotificationDefinitionPreview.Open((NotificationDefinitionAsset)asset);
        public override void Validate(DeucarianDefinitionSpec value)
        {
            base.Validate(value);
            var spec = (NotificationDefinitionSpec)value;
            if (string.IsNullOrWhiteSpace(spec.Title)) throw new ArgumentException("Enter a title for notification '" + spec.Name + "'.");
            if (!Enum.IsDefined(typeof(NotificationSeverity), spec.Severity) || !Enum.IsDefined(typeof(NotificationLifetimeKind), spec.Lifetime) || !Enum.IsDefined(typeof(NotificationSoundPolicy), spec.Sound)) throw new ArgumentException("Select existing severity, lifetime and sound values.");
            if (spec.Lifetime == NotificationLifetimeKind.Timed && (spec.DurationSeconds <= 0 || float.IsInfinity(spec.DurationSeconds) || float.IsNaN(spec.DurationSeconds))) throw new ArgumentException("Timed notifications need a finite duration greater than zero.");
            if (spec.Sound == NotificationSoundPolicy.Custom && spec.CustomSound == null) throw new ArgumentException("Choose a custom sound role, or use SeverityDefault.");
        }
        public override void RefreshCatalog(bool validateOnly = false)
        {
            const string path = "Assets/DeucarianDefinitions/Resources/Deucarian/Notifications/ProjectNotificationCatalog.asset";
            var definitions = AssetDatabase.FindAssets("t:NotificationDefinitionAsset", new[] { "Assets" })
                .Select(x => AssetDatabase.LoadAssetAtPath<NotificationDefinitionAsset>(AssetDatabase.GUIDToAssetPath(x)))
                .Where(x => x != null).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            if (definitions.GroupBy(x => x.Id).Any(x => x.Count() > 1)) throw new InvalidOperationException("Notification IDs must be unique before refreshing the project catalog.");
            DeucarianDefinitionCatalog.Update<NotificationCatalogAsset>(path, "definitions", definitions, validateOnly);
        }
        [MenuItem("Assets/Create/Deucarian/Notifications/Notification Definition")]
        private static void CreateDefinition() { Selection.activeObject = DeucarianDefinitionSync.Create(new NotificationDefinitionSchema(), "NewNotification"); }
    }
}
