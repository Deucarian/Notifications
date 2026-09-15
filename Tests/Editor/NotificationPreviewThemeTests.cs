using Deucarian.Editor;
using System;
using System.Reflection;
using Deucarian.Notifications.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationPreviewThemeTests
    {
        private string theme, family, palette, library, style;
        private DeucarianThemeMode mode;
        private ComposerPreviewScope composerPreview;
        private sealed class Clock : INotificationClock { public double NowSeconds => 0; }

        [SetUp]
        public void RememberSelection()
        {
            theme = DeucarianThemingEditorSettings.ActiveThemeGuid;
            family = DeucarianThemingEditorSettings.ActiveThemeFamilyGuid;
            palette = DeucarianThemingEditorSettings.ActivePaletteGuid;
            library = DeucarianThemingEditorSettings.ActiveRoleLibraryGuid;
            style = DeucarianThemingEditorSettings.ActiveStyleGuid;
            mode = DeucarianThemingEditorSettings.ActiveThemeMode;
            composerPreview = new ComposerPreviewScope();
        }

        [TearDown]
        public void RestoreSelection()
        {
            DeucarianThemingEditorSettings.ActiveThemeGuid = theme;
            DeucarianThemingEditorSettings.ActiveThemeFamilyGuid = family;
            DeucarianThemingEditorSettings.ActivePaletteGuid = palette;
            DeucarianThemingEditorSettings.ActiveRoleLibraryGuid = library;
            DeucarianThemingEditorSettings.ActiveStyleGuid = style;
            DeucarianThemingEditorSettings.ActiveThemeMode = mode;
            composerPreview?.Dispose();
            composerPreview = null;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RuntimeRowsFollowLightAndDarkSelectionWithoutReplacingRowsOrChangingProjectDefault(bool stagedStyle)
        {
            var defaults = DeucarianVisualDefaults.LoadFamily();
            var applied = DeucarianThemeRuntimeResolver.ResolveDefaultTheme();
            using var workspace = new DeucarianEditorLabWorkspace(new VisualElement(), "Test", "Notifications", "", () => { }, _ => { });
            using var session = new NotificationLabSession(new Clock(), null);
            using var preview = new NotificationRuntimePreview(workspace, false);
            var presentation = NotificationPresentationSettings.Default;
            presentation.show = NotificationTransition.None;
            var selectedStyle = stagedStyle ? AlternateStyle(defaults.LightTheme.VisualStyle) : defaults.LightTheme.VisualStyle;
            DeucarianThemingEditorSettings.SetDraftSelection(defaults, DeucarianThemeMode.Light, selectedStyle);
            preview.Configure(presentation, null);
            session.Show(NotificationLabSession.Example(NotificationSeverity.Warning), default);
            preview.Render(session.Store.Snapshot);
            var row = preview.List.GetComponentInChildren<NotificationRowView>();
            Assert.That(row.ThemeOverride.ColorPalette, Is.SameAs(defaults.LightTheme.ColorPalette));
            Assert.That(row.ThemeOverride.VisualStyle, Is.SameAs(selectedStyle));
            Assert.That(row.BodyColor, Is.EqualTo(defaults.LightTheme.ColorPalette.GetColorById(DeucarianBuiltinColorRoleIds.TextSecondary)));
            Assert.That(preview.ThemeDescription, Does.Contain("Light"));

            DeucarianThemingEditorSettings.SetDraftSelection(defaults, DeucarianThemeMode.Dark, selectedStyle);
            preview.Configure(presentation, null);
            preview.Render(session.Store.Snapshot);
            Assert.That(preview.List.GetComponentInChildren<NotificationRowView>(), Is.SameAs(row));
            Assert.That(row.ThemeOverride.ColorPalette, Is.SameAs(defaults.DarkTheme.ColorPalette));
            Assert.That(row.ThemeOverride.VisualStyle, Is.SameAs(selectedStyle));
            Assert.That(row.BodyColor, Is.EqualTo(defaults.DarkTheme.ColorPalette.GetColorById(DeucarianBuiltinColorRoleIds.TextSecondary)));
            Assert.That(DeucarianThemeRuntimeResolver.ResolveDefaultTheme(), Is.SameAs(applied));
        }

        [Test]
        public void StagedStyleIsComposedWithoutEditingThemeAndRuntimeOverrideTakesPrecedence()
        {
            var defaults = DeucarianVisualDefaults.LoadFamily();
            var original = defaults.LightTheme.VisualStyle;
            var alternate = AlternateStyle(original);
            DeucarianThemingEditorSettings.SetDraftSelection(defaults, DeucarianThemeMode.Light, alternate);
            using var resolver = new NotificationPreviewTheme();
            var composed = resolver.Resolve(null, null);
            Assert.That(composed, Is.Not.SameAs(defaults.LightTheme));
            Assert.That(composed.ColorPalette, Is.SameAs(defaults.LightTheme.ColorPalette));
            Assert.That(composed.VisualStyle, Is.SameAs(alternate));
            Assert.That(resolver.Resolve(null, null), Is.SameAs(composed));
            Assert.That(resolver.Description, Does.Contain("draft").And.Contain("Apply to project"));
            Assert.That(defaults.LightTheme.VisualStyle, Is.SameAs(original));
            var target = new GameObject("Preview target", typeof(RectTransform));
            target.SetActive(false);
            try
            {
                var row = target.AddComponent<NotificationRowView>();
                row.ThemeOverride = defaults.DarkTheme;
                Assert.That(resolver.Resolve(row, row), Is.SameAs(defaults.DarkTheme));
                Assert.That(resolver.Description, Does.StartWith("Running list theme"));
            }
            finally { Object.DestroyImmediate(target); }
            resolver.Dispose();
            Assert.That(composed == null, Is.True);
        }

        private static DeucarianThemeStyle AlternateStyle(DeucarianThemeStyle original)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DeucarianThemeStyle"))
            {
                var candidate = AssetDatabase.LoadAssetAtPath<DeucarianThemeStyle>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != original) return candidate;
            }
            Assert.Fail("The bundled styles must include an alternate visual style.");
            return null;
        }

        /// <summary>Exact editor-only test bridge; preserve another package's active user preview without widening its production API.</summary>
        private sealed class ComposerPreviewScope : IDisposable
        {
            private const BindingFlags StaticInternal = BindingFlags.Static | BindingFlags.NonPublic;
            private readonly Type coordinator = typeof(DeucarianEditorThemePreview).Assembly.GetType(
                "Deucarian.Theming.Editor.DeucarianThemePreviewCoordinator", true);
            private readonly object selection;
            private readonly DeucarianThemeStyle source;
            private bool disposed;

            public ComposerPreviewScope()
            {
                selection = coordinator.GetProperty("SelectedPreview", StaticInternal).GetValue(null);
                if ((bool)coordinator.GetProperty("HasComposerPreview", StaticInternal).GetValue(null))
                {
                    var activeStyle = (DeucarianThemeStyle)coordinator.GetProperty("ComposerPreviewStyle", StaticInternal).GetValue(null);
                    source = Object.Instantiate(activeStyle);
                    source.name = activeStyle.name;
                    source.hideFlags = HideFlags.HideAndDontSave;
                }
                coordinator.GetMethod("ClearComposerPreview", StaticInternal).Invoke(null, null);
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    if (source != null)
                    {
                        coordinator.GetMethod("ApplyComposerPreview", StaticInternal).Invoke(null, new object[] {
                            selection, source, source.SurfaceProfile, source.ShapeProfile, source.StrokeProfile,
                            source.Density, source.TypographyProfile });
                        ((DeucarianThemeStyle)coordinator.GetProperty("ComposerPreviewStyle", StaticInternal).GetValue(null)).name = source.name;
                    }
                    else coordinator.GetMethod("ClearComposerPreview", StaticInternal).Invoke(null, null);
                }
                finally { if (source != null) Object.DestroyImmediate(source); }
            }
        }

        [Test]
        public void RecreatedLabRetainsInputsDefinitionDraftAndTabWithoutReinjectingMessages()
        {
            string oldDraft = DeucarianEditorProjectPreferences.GetString("notifications.lab.draft");
            var first = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
            DeucarianNotificationLabWindow second = null;
            try
            {
                first.Inputs = new NotificationLabRecipeData { title = "Draft title", body = "Draft body", sound = false };
                first.SelectedTab = 3;
                first.DefinitionState.Search = "saved";
                first.DefinitionState.CreateName = "New definition draft";
                first.DefinitionState.DetailsScroll = new Vector2(0, 240);
                first.AddCustom();
                string captured = first.CaptureReloadState();
                Object.DestroyImmediate(first); first = null;
                second = ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>();
                second.RestoreReloadState(captured);
                Assert.That(second.CaptureReloadState(), Is.EqualTo(captured));
                Assert.That(second.SelectedTab, Is.EqualTo(3));
                Assert.That(second.Inputs.title, Is.EqualTo("Draft title"));
                Assert.That(second.DefinitionState.CreateName, Is.EqualTo("New definition draft"));
                Assert.That(second.DefinitionState.DetailsScroll.y, Is.EqualTo(240));
                Assert.That(second.Session.Store.Snapshot.Count, Is.Zero);
                Assert.That(second.Connection, Is.Null);
            }
            finally
            {
                if (first != null) Object.DestroyImmediate(first);
                if (second != null) Object.DestroyImmediate(second);
                if (string.IsNullOrEmpty(oldDraft)) DeucarianEditorProjectPreferences.Delete("notifications.lab.draft");
                else DeucarianEditorProjectPreferences.SetString("notifications.lab.draft", oldDraft);
            }
        }
    }
}
