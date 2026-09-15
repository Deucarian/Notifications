using System;
using System.IO;
using System.Linq;
using Deucarian.Editor;
using Deucarian.Editor.Definitions;
using Deucarian.Notifications.Editor.Definitions;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEditor;

namespace Deucarian.Notifications.Tests.Editor
{
    public sealed class NotificationDefinitionSynchronizationTests
    {
        private const string DirectoryPath = "Assets/DefinitionSynchronizationTest";
        private const string SourcePath = DirectoryPath + "/Editor/Connection.definition.txt";
        private readonly NotificationDefinitionSchema schema = new NotificationDefinitionSchema();
        private bool previousDisabled;

        [SetUp]
        public void SetUp()
        {
            previousDisabled = SessionState.GetBool(DeucarianKeyGeneration.AutomaticRefreshDisabledSessionKey, false);
            SessionState.SetBool(DeucarianKeyGeneration.AutomaticRefreshDisabledSessionKey, true);
            Directory.CreateDirectory(DirectoryPath + "/Editor");
        }

        [TearDown]
        public void TearDown()
        {
            var record = DeucarianDefinitionSync.Records.FirstOrDefault(x => AssetDatabase.GUIDToAssetPath(x.sourceGuid) == SourcePath);
            if (record != null) DeucarianDefinitionSync.Delete(schema, record);
            AssetDatabase.DeleteAsset(DirectoryPath);
            SessionState.SetBool(DeucarianKeyGeneration.AutomaticRefreshDisabledSessionKey, previousDisabled);
        }

        [Test]
        public void CodeAndInspectorEditsPreserveAssetIdentityAndUpdateEachOther()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("SynchronizationTest");
            Write(spec);
            var asset = (NotificationDefinitionAsset)DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            spec.Message = "Edited in C#";
            Write(spec);
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            Assert.That(asset.CreateDefinition().Body, Is.EqualTo(spec.Message));
            var fromAsset = (NotificationDefinitionSpec)schema.Read(asset);
            fromAsset.Title = "Edited in Inspector";
            schema.Apply(asset, fromAsset);
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            var fromCode = (NotificationDefinitionSpec)DeucarianDefinitionSource.Read(schema, File.ReadAllText(SourcePath));
            Assert.That(fromCode.Title, Is.EqualTo(fromAsset.Title));
            Assert.That(fromCode.Id, Is.EqualTo(spec.Id));
            Assert.That(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)), Is.EqualTo(guid));
            string source = File.ReadAllText(SourcePath);
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            Assert.That(File.ReadAllText(SourcePath), Is.EqualTo(source), "An unchanged synchronization must converge.");
        }

        [Test]
        public void RiderImportsAndRegionsKeepCodeAndAssetSynchronizedWithoutReformatting()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("RiderFormattingTest");
            Write(spec);
            var asset = (NotificationDefinitionAsset)DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            string source = File.ReadAllText(SourcePath)
                .Replace("namespace Deucarian.ProjectDefinitions", "using Deucarian.Notifications;\nusing Deucarian.Notifications.Editor.Definitions;\nusing Deucarian.Notifications.Unity;\nnamespace Deucarian.ProjectDefinitions")
                .Replace("global::Deucarian.Notifications.Editor.Definitions.", "")
                .Replace("global::Deucarian.Notifications.Unity.", "")
                .Replace("global::Deucarian.Notifications.", "")
                .Replace("        public static", "        #region Public Properties\n        public static")
                .Replace("        // end-definition-value", "        #endregion\n        // end-definition-value")
                .Replace("Please reconnect your device.", "Edited after IDE cleanup");
            File.WriteAllText(SourcePath, source);
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            Assert.That(asset.CreateDefinition().Body, Is.EqualTo("Edited after IDE cleanup"));
            Assert.That(File.ReadAllText(SourcePath), Is.EqualTo(source));
            var fromAsset = (NotificationDefinitionSpec)schema.Read(asset);
            fromAsset.Message = "Edited from the asset";
            schema.Apply(asset, fromAsset);
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            Assert.That(((NotificationDefinitionSpec)DeucarianDefinitionSource.Read(schema, File.ReadAllText(SourcePath))).Message,
                Is.EqualTo(fromAsset.Message));
            Assert.That(schema.Read(asset).Id, Is.EqualTo(spec.Id));
        }

        [Test]
        public void ConcurrentEditsRequireExplicitResolutionWithoutDiscardingEitherSide()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("ConflictTest");
            Write(spec);
            var asset = DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            var fromAsset = (NotificationDefinitionSpec)schema.Read(asset);
            fromAsset.Message = "Asset edit";
            schema.Apply(asset, fromAsset);
            spec.Message = "Code edit";
            Write(spec);
            Assert.That(() => DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath), Throws.InvalidOperationException.With.Message.Contains("Both code and asset changed"));
            Assert.That(((NotificationDefinitionSpec)schema.Read(asset)).Message, Is.EqualTo("Asset edit"));
            Assert.That(File.ReadAllText(SourcePath), Does.Contain("Code edit"));
            DeucarianDefinitionSync.Resolve(schema, DeucarianDefinitionSync.FindAsset(asset), true);
            Assert.That(((NotificationDefinitionSpec)schema.Read(asset)).Message, Is.EqualTo("Code edit"));
        }

        [Test]
        public void MissingAssetsAndIdentityEditsAreNeverSilentlyRecreated()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("IdentityTest");
            Write(spec);
            var asset = DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            string originalId = spec.Id;
            spec.Id = Guid.NewGuid().ToString("N");
            Write(spec);
            Assert.That(() => DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath), Throws.InvalidOperationException.With.Message.Contains("stable"));
            spec.Id = originalId;
            Write(spec);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(asset));
            Assert.That(() => DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath), Throws.InvalidOperationException.With.Message.Contains("was removed"));
        }

        private void Write(NotificationDefinitionSpec spec)
        {
            File.WriteAllText(SourcePath, DeucarianDefinitionSource.Write(schema, spec));
            AssetDatabase.ImportAsset(SourcePath);
        }

        [Test]
        public void ImportedAssetAndSourceRelinkWithoutAnExistingSynchronizationIndex()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("ImportedPairTest");
            var asset = UnityEngine.ScriptableObject.CreateInstance<NotificationDefinitionAsset>();
            AssetDatabase.CreateAsset(asset, DirectoryPath + "/Imported.asset");
            schema.Apply(asset, spec);
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            Write(spec);
            Assert.That(DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath), Is.SameAs(asset));
            Assert.That(DeucarianDefinitionSync.FindAsset(asset).assetGuid, Is.EqualTo(guid));
        }

        [Test]
        public void RenameUpdatesTheCodeSymbolWithoutChangingIdentity()
        {
            var spec = (NotificationDefinitionSpec)schema.Create("BeforeRenameTest");
            Write(spec);
            var asset = DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            string guid = DeucarianDefinitionSync.FindAsset(asset).assetGuid;
            File.WriteAllText(SourcePath, File.ReadAllText(SourcePath).Replace("Name = \"BeforeRenameTest\"", "Name = \"AfterRenameTest\""));
            DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath);
            Assert.That(File.ReadAllText(SourcePath), Does.Contain("Definition_AfterRenameTest"));
            Assert.That(DeucarianDefinitionSync.FindAsset(asset).assetGuid, Is.EqualTo(guid));
            Assert.That(schema.Read(asset).Id, Is.EqualTo(spec.Id));
        }

        [Test]
        public void TwoNamesThatGenerateTheSameSymbolAreRejected()
        {
            var existing = (NotificationDefinitionSpec)schema.Create("Same Symbol Test");
            var asset = UnityEngine.ScriptableObject.CreateInstance<NotificationDefinitionAsset>();
            AssetDatabase.CreateAsset(asset, DirectoryPath + "/Existing.asset");
            schema.Apply(asset, existing);
            var other = (NotificationDefinitionSpec)schema.Create("SameSymbolTest");
            Write(other);
            Assert.That(() => DeucarianDefinitionSync.SynchronizeSource(schema, SourcePath), Throws.InvalidOperationException.With.Message.Contains("distinct name"));
            Assert.That(DeucarianDefinitionSync.FindAsset(asset), Is.Null);
        }
    }
}
