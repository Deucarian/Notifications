using System;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Renders the package's real uGUI list in an isolated preview scene, without a second row implementation.</summary>
    internal sealed class NotificationRuntimePreview : IDisposable
    {
        private const float Width = 720;
        private readonly VisualElement root;
        private readonly bool autoAdvance;
        private double lastRenderTime;
        private readonly UnityEngine.UIElements.Image image;
        private readonly Label status;
        private PreviewRenderUtility renderer;
        private GameObject canvasObject;
        private RectTransform canvasRect;
        private NotificationListView list;
        private NotificationRowView template;
        private NotificationRowView sourceTemplate;
        private NotificationSnapshot snapshot = NotificationSnapshot.Empty;
        private NotificationPresentationSettings settings;
        private double lastTime;
        private bool disposed;
        private bool dirty = true;

        internal NotificationRuntimePreview(DeucarianEditorLabWorkspace workspace, bool autoAdvance = true)
        {
            this.autoAdvance = autoAdvance;
            root = DeucarianEditorWorkspaceControls.Region("notification-runtime-preview");
            image = new UnityEngine.UIElements.Image { name = "notification-runtime-image", scaleMode = ScaleMode.ScaleToFit };
            image.style.height = 360;
            image.style.flexGrow = 1;
            root.Add(image);
            status = DeucarianEditorWorkspaceControls.Label("Runtime notification prefab", "dw-muted");
            root.Add(status);
            workspace.SetRuntimePreview(root);
            if (autoAdvance) EditorApplication.update += Tick;
            lastTime = EditorApplication.timeSinceStartup;
        }

        internal NotificationListView List => list;
        internal bool HasRenderedFrame { get; private set; }

        internal void Configure(NotificationPresentationSettings value, Component runtimeView)
        {
            var runtimeList = runtimeView as NotificationListView;
            if (runtimeList == null && runtimeView != null) runtimeList = runtimeView.GetComponentInChildren<NotificationListView>(true);
            var source = runtimeList != null ? runtimeList.RowTemplate : null;
            if (renderer == null || source != sourceTemplate) Create(source);
            if (list == null) return;
            var next = value.Sanitized();
            if (!settings.Equals(next)) { settings = next; list.ConfigurePresentation(settings); dirty = true; }
            DeucarianTheme theme = source != null ? source.ThemeOverride : null;
            var provider = runtimeView != null ? DeucarianThemeRuntimeResolver.FindProvider(runtimeView) : null;
            if (theme == null && provider != null) theme = provider.CurrentTheme;
            if (theme == null) theme = DeucarianThemeRuntimeResolver.ResolveDefaultTheme();
            if (template.ThemeOverride != theme)
            {
                template.ThemeOverride = theme;
                foreach (var row in list.GetComponentsInChildren<NotificationRowView>(true)) row.ThemeOverride = theme;
                dirty = true;
            }
        }

        internal void Render(NotificationSnapshot value)
        {
            value = value ?? NotificationSnapshot.Empty;
            if (ReferenceEquals(snapshot, value) && !dirty) return;
            snapshot = value;
            if (list == null) return;
            list.Render(value);
            dirty = false;
        }

        internal void Replay()
        {
            if (list == null) return;
            var instant = settings; instant.hideSeconds = 0; instant.hide = NotificationTransition.None;
            list.ConfigurePresentation(instant);
            list.Render(NotificationSnapshot.Empty);
            list.ConfigurePresentation(settings);
            list.Render(snapshot);
        }

        private void Create(NotificationRowView source)
        {
            ReleaseRenderer();
            sourceTemplate = source;
            var prefab = Resources.Load<GameObject>("Deucarian/Notifications/Defaults/DefaultNotificationList");
            if (prefab == null) { status.text = "The runtime notification prefab is missing. Repair the package view assets."; return; }
            if (TMPro.TMP_Settings.instance == null) { status.text = "Import TMP Essential Resources to preview the runtime notification."; return; }
            renderer = new PreviewRenderUtility();
            renderer.camera.orthographic = true;
            renderer.camera.nearClipPlane = .01f;
            renderer.camera.farClipPlane = 100;
            renderer.camera.clearFlags = CameraClearFlags.SolidColor;
            renderer.camera.backgroundColor = Color.clear;
            renderer.camera.transform.position = new Vector3(0, 0, -10);
            canvasObject = new GameObject("Notification preview", typeof(RectTransform), typeof(Canvas));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            renderer.AddSingleGO(canvasObject);
            canvasRect = (RectTransform)canvasObject.transform;
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = renderer.camera;
            var instance = UnityEngine.Object.Instantiate(prefab, canvasRect, false);
            list = instance.GetComponent<NotificationListView>();
            var row = source != null ? source : list.RowTemplate;
            template = UnityEngine.Object.Instantiate(row, canvasRect, false);
            template.gameObject.SetActive(false);
            template.CopyAuthoredBaselineFrom(row);
            var container = instance.GetComponent<RectTransform>();
            // Frame the list near the top of the preview; row geometry remains prefab-owned.
            list.Configure(container, template, .03f, .95f, false);
            list.EditorPreview = true;
            list.ConfigurePresentation(settings);
            dirty = true;
        }

        internal void Tick()
        {
            if (disposed || renderer == null || list == null) return;
            double now = EditorApplication.timeSinceStartup;
            float delta = Mathf.Clamp((float)(now - lastTime), 0, .1f);
            lastTime = now;
            list.AdvancePreview(delta);
            if (root.panel == null || !root.visible || now - lastRenderTime < 1d / 30d) return;
            for (VisualElement ancestor = root; ancestor != null; ancestor = ancestor.parent)
                if (ancestor.resolvedStyle.display == DisplayStyle.None) return;
            lastRenderTime = now;
            RenderFrame();
        }

        internal void RenderFrame()
        {
            if (renderer == null || list == null) return;
            float height = Mathf.Clamp((Mathf.Min(snapshot.Count, settings.maxVisible) + 1) * (template.PreferredHeight + 10) + 30, 240, 900);
            canvasRect.sizeDelta = new Vector2(Width, height);
            renderer.camera.orthographicSize = height * .5f;
            renderer.camera.aspect = Width / height;
            Canvas.ForceUpdateCanvases();
            renderer.BeginPreview(new Rect(0, 0, Width, height), GUIStyle.none);
            renderer.Render(true);
            image.image = renderer.EndPreview();
            HasRenderedFrame = true;
            image.style.height = Mathf.Min(height, 520);
            image.MarkDirtyRepaint();
        }

        private void ReleaseRenderer()
        {
            image.image = null;
            renderer?.Cleanup();
            renderer = null; canvasObject = null; canvasRect = null; list = null; template = null;
            HasRenderedFrame = false;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            EditorApplication.update -= Tick;
            ReleaseRenderer();
        }
    }
}
