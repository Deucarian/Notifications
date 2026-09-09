using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Interactive editor-only notification lifecycle lab with an isolated store.</summary>
    public sealed partial class DeucarianNotificationLabWindow : EditorWindow, INotificationListView
    {
        private sealed class EditorClock : INotificationClock
        {
            public double NowSeconds => EditorApplication.timeSinceStartup;
        }

        private NotificationLabWorkspaceAdapter workspace;
        private NotificationLabSession session;
        private NotificationPresenter presenter;
        private NotificationLabAudio audio;
        private NotificationLabRuntimeConnection runtimeConnection;
        private NotificationId lastCustomId;
        private int nextCustomId;
        private string runtimeStatus;
        private NotificationSnapshot snapshot = NotificationSnapshot.Empty;
        [SerializeField] private DeucarianAudioPaletteSet paletteSet;
        [SerializeField] private DeucarianAudioExperience experience = DeucarianAudioExperience.XR;
        [SerializeField] private bool soundEnabled = true;
        [SerializeField] private string messageTitle = "Example warning";
        [SerializeField] private string messageBody = "This is a test notification. Resolve it to simulate recovery.";
        [SerializeField] private NotificationSeverity severity = NotificationSeverity.Warning;
        [SerializeField] private float activationDelay;
        [SerializeField] private float recoveryDelay = 1f;
        private double nextRepaint;

        public static void OpenWindow()
        {
            var window = GetWindow<DeucarianNotificationLabWindow>("Notification Lab");
            DeucarianEditorWorkspace.ConfigureWindow(window);
            window.AdoptPaletteSelection();
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            RestoreDraft();
            AdoptPaletteSelection();
            StartSession();
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += StopSession;
        }

        private void OnDisable()
        {
            SaveDraft();
            workspace?.Dispose();
            workspace = null;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= StopSession;
            StopSession();
        }

        private void StartSession()
        {
            StopSession();
            audio = new NotificationLabAudio(new DeucarianAudioPreviewService());
            audio.Configure(paletteSet, experience, soundEnabled);
            session = new NotificationLabSession(new EditorClock(), audio);
            presenter = new NotificationPresenter(session.Store, this);
            presenter.Activate();
        }

        private void StopSession()
        {
            runtimeConnection?.Dispose();
            runtimeConnection = null;
            presenter?.Dispose();
            session?.Dispose();
            audio?.Dispose();
            presenter = null;
            session = null;
            audio = null;
            snapshot = NotificationSnapshot.Empty;
            lastCustomId = default;
            nextCustomId = 0;
            workspace?.Refresh();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Clear pending work before a transition, including when domain reload is disabled.
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                StopSession();
            else
                StartSession();
            workspace?.Refresh();
        }

        private void Tick()
        {
            if (runtimeConnection != null && !IsRuntimeTarget(runtimeConnection.Target))
            {
                DisconnectRuntime();
                session?.Reset();
                runtimeStatus = "The runtime list closed. Test messages were removed; choose a destination again.";
                workspace?.Refresh();
            }
            session?.Tick();
            if (EditorApplication.timeSinceStartup >= nextRepaint)
            {
                nextRepaint = EditorApplication.timeSinceStartup + 0.1d;
                workspace?.Refresh();
            }
        }

        public void Render(NotificationSnapshot value)
        {
            snapshot = value ?? NotificationSnapshot.Empty;
            workspace?.Refresh();
        }

        internal void AdoptPaletteSelection()
        {
            if (Selection.activeObject is DeucarianAudioPaletteSet selected) paletteSet = selected;
            if (paletteSet == null)
            {
                string[] matches = AssetDatabase.FindAssets("t:DeucarianAudioPaletteSet", new[] { "Assets" });
                if (matches.Length == 1)
                    paletteSet = AssetDatabase.LoadAssetAtPath<DeucarianAudioPaletteSet>(AssetDatabase.GUIDToAssetPath(matches[0]));
                if (paletteSet == null) paletteSet = DeucarianAudioDefaults.LoadPaletteSet();
            }
            audio?.Configure(paletteSet, experience, soundEnabled && runtimeConnection == null);
        }

        internal void ShowCustom()
        {
            if (session == null || lastCustomId.IsEmpty || string.IsNullOrWhiteSpace(messageTitle)) return;
            audio.Configure(paletteSet, experience, soundEnabled && runtimeConnection == null);
            session.Show(new NotificationDefinition(lastCustomId, severity, messageTitle, messageBody,
                (int)severity * 10, NotificationLabSession.FeedbackRole(severity), Lifetime()), Timing());
        }

        internal void AddCustom()
        {
            if (session == null || string.IsNullOrWhiteSpace(messageTitle)) return;
            lastCustomId = new NotificationId("lab.custom." + ++nextCustomId);
            ShowCustom();
            workspace?.Refresh();
        }

        internal static bool IsRuntimeTarget(NotificationEditorTarget target)
        {
            return EditorApplication.isPlaying && target != null && target.IsAvailable && target.View is Component component &&
                   component != null && component.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(component);
        }

        internal void SelectRuntimeTarget(NotificationEditorTarget target)
        {
            if (session == null) return;
            DisconnectRuntime();
            session.Reset();
            lastCustomId = default;
            runtimeStatus = null;
            if (target != null)
            {
                audio.Configure(paletteSet, experience, false);
                runtimeConnection = new NotificationLabRuntimeConnection(session.Store, target, new EditorClock());
                presentationSettings = runtimeConnection.Presentation;
            }
            workspace?.Refresh();
        }

        private void DisconnectRuntime()
        {
            runtimeConnection?.Dispose();
            runtimeConnection = null;
            audio?.Configure(paletteSet, experience, soundEnabled);
        }

        private NotificationTimingPolicy Timing() => new NotificationTimingPolicy(
            SanitizeDelay(activationDelay), SanitizeDelay(recoveryDelay));

        private static float SanitizeDelay(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, 0f, 60f);

        public void CreateGUI()
        {
            workspace?.Dispose();
            workspace = new NotificationLabWorkspaceAdapter(rootVisualElement, this);
            workspace.Refresh();
        }

        internal NotificationLabRecipeData Inputs { get => CaptureDraft(); set { ApplyDraft(value); workspace?.Refresh(); } }
        internal NotificationLabSession Session => session;
        internal NotificationSnapshot Snapshot => snapshot;
        internal NotificationLabRuntimeConnection Connection => runtimeConnection;
        internal string RuntimeStatus => runtimeStatus;
        internal string AudioStatus => audio?.Status ?? "The test session is restarting.";
        internal DeucarianAudioPaletteSet Palette { get => paletteSet; set { paletteSet = value; audio?.Configure(value, experience, soundEnabled && runtimeConnection == null); } }
        internal bool HasLast => !lastCustomId.IsEmpty;

        internal void RepeatLast()
        {
            if (lastCustomId.IsEmpty) lastCustomId = new NotificationId("lab.custom." + ++nextCustomId);
            for (int i = 0; i < 10; i++) ShowCustom();
        }

        internal void ResolveLast() { session?.Resolve(lastCustomId); workspace?.Refresh(); }
        internal void ClearMessages() { session?.Reset(); lastCustomId = default; audio?.Stop(); workspace?.Refresh(); }
        internal void StopAudio() => audio?.Stop();
        internal void OpenAudioLab() { if (paletteSet != null) DeucarianAudioPaletteLabWindow.Open(paletteSet); }
        internal void ShowThree() => session?.ShowBatch(new[] {
            NotificationLabSession.Example(NotificationSeverity.Info, Lifetime()),
            NotificationLabSession.Example(NotificationSeverity.Warning, Lifetime()),
            NotificationLabSession.Example(NotificationSeverity.Error, Lifetime()) }, Timing());

        internal NotificationLabSession SessionForTests => session;
        internal void DisableForTests() => OnDisable();
        internal void TransitionForTests(PlayModeStateChange state) => OnPlayModeChanged(state);
        internal void SelectRuntimeTargetForTests(NotificationEditorTarget target) => SelectRuntimeTarget(target);
        internal void AddCustomForTests() => AddCustom();
        internal void UpdateCustomForTests() => ShowCustom();
        internal void TickForTests() => Tick();
        internal static bool CanTargetForTests(NotificationEditorTarget target) => IsRuntimeTarget(target);
    }
}
