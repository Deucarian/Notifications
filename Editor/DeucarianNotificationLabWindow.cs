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

        private static readonly string[] ExperienceLabels = { "Default", "XR", "WebGL", "Desktop", "Mobile" };
        private NotificationLabSession session;
        private NotificationPresenter presenter;
        private NotificationLabAudio audio;
        private NotificationLabRuntimeConnection runtimeConnection;
        private readonly List<NotificationEditorTarget> runtimeTargets = new List<NotificationEditorTarget>();
        private NotificationId lastCustomId;
        private int nextCustomId;
        private string runtimeStatus;
        private NotificationSnapshot snapshot = NotificationSnapshot.Empty;
        private Vector2 scroll;
        [SerializeField] private DeucarianAudioPaletteSet paletteSet;
        [SerializeField] private DeucarianAudioExperience experience = DeucarianAudioExperience.XR;
        [SerializeField] private bool soundEnabled = true;
        [SerializeField] private string messageTitle = "Example warning";
        [SerializeField] private string messageBody = "This is a test notification. Resolve it to simulate recovery.";
        [SerializeField] private NotificationSeverity severity = NotificationSeverity.Warning;
        [SerializeField] private float activationDelay;
        [SerializeField] private float recoveryDelay = 1f;
        [SerializeField] private bool advancedTests;
        [SerializeField] private bool showPresentation;
        [SerializeField] private bool showAudio;
        private double nextRepaint;

        public static void OpenWindow()
        {
            var window = GetWindow<DeucarianNotificationLabWindow>("Notification Lab");
            window.minSize = new Vector2(420f, 400f);
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
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // Clear pending work before a transition, including when domain reload is disabled.
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
                StopSession();
            else
                StartSession();
            Repaint();
        }

        private void Tick()
        {
            if (runtimeConnection != null && !IsRuntimeTarget(runtimeConnection.Target))
            {
                DisconnectRuntime();
                session?.Reset();
                runtimeStatus = "The runtime list closed. Test messages were removed; choose a destination again.";
                Repaint();
            }
            session?.Tick();
            if (session != null && (session.PendingCount > 0 || HasTimedMessages()) && EditorApplication.timeSinceStartup >= nextRepaint)
            {
                nextRepaint = EditorApplication.timeSinceStartup + 0.1d;
                Repaint();
            }
        }

        public void Render(NotificationSnapshot value)
        {
            snapshot = value ?? NotificationSnapshot.Empty;
            Repaint();
        }

        private void OnGUI()
        {
            DeucarianEditorWindowChrome.DrawImGuiWindowBackground(new Rect(Vector2.zero, position.size));
            DeucarianEditorChrome.DrawPackageHeader("notifications", "Notification Lab",
                "Add and resolve test messages in the editor or in your running application's warning list.");
            if (session == null)
            {
                EditorGUILayout.HelpBox("The test session will restart after the Play Mode transition.", MessageType.Info);
                return;
            }
            DrawDestination();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (position.width >= 940f)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.43f))) DrawComposer();
                    using (new EditorGUILayout.VerticalScope()) DrawLivePreview();
                }
            }
            else
            {
                DrawComposer();
                DrawLivePreview();
            }
            DrawRecipeControls();
            showPresentation = EditorGUILayout.Foldout(showPresentation, "Appearance and motion", true);
            if (showPresentation) DrawPresentation();
            showAudio = EditorGUILayout.Foldout(showAudio, "Audio", true);
            if (showAudio) DrawAudio();
            EditorGUILayout.EndScrollView();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Primary("Add message", !string.IsNullOrWhiteSpace(messageTitle))) AddCustom();
                if (DeucarianEditorButtons.Secondary("Resolve all", snapshot.Count > 0 || session.PendingCount > 0)) session.ResolveAll();
            }
            DeucarianEditorStatusPanel.DrawStatusBar(runtimeConnection == null ? "Editor preview only" : "Connected to running app",
                snapshot.Count + " active · " + session.PendingCount + " waiting", experience.ToString());
        }

        private void DrawComposer()
        {
            DeucarianEditorCards.BeginCard("Try a notification", subtitle: "Add separate messages, or update the last one without another ping.");
            messageTitle = EditorGUILayout.TextField("Title", messageTitle);
            EditorGUILayout.LabelField("Message", DeucarianEditorStyles.MutedLabel);
            messageBody = EditorGUILayout.TextArea(messageBody, GUILayout.MinHeight(48f));
            severity = (NotificationSeverity)EditorGUILayout.EnumPopup("Severity", severity);
            lifetimeKind = (NotificationLifetimeKind)EditorGUILayout.EnumPopup("Lifetime", lifetimeKind);
            if (lifetimeKind == NotificationLifetimeKind.Timed)
                lifetimeSeconds = Mathf.Max(0.1f, SanitizeDelay(EditorGUILayout.FloatField("Duration (seconds)", lifetimeSeconds)));
            advancedTests = EditorGUILayout.Foldout(advancedTests, "Advanced tests", true);
            if (advancedTests)
            {
            activationDelay = SanitizeDelay(EditorGUILayout.FloatField("Show delay (seconds)", activationDelay));
            recoveryDelay = SanitizeDelay(EditorGUILayout.FloatField("Recovery delay (seconds)", recoveryDelay));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Update last", !lastCustomId.IsEmpty)) ShowCustom();
                if (DeucarianEditorButtons.Secondary("Resolve last", !lastCustomId.IsEmpty)) session.Resolve(lastCustomId);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Repeat same message 10×"))
                {
                    if (lastCustomId.IsEmpty) lastCustomId = new NotificationId("lab.custom." + ++nextCustomId);
                    for (int i = 0; i < 10; i++) ShowCustom();
                }
                if (DeucarianEditorButtons.Secondary("Show three at once"))
                    session.ShowBatch(new[]
                    {
                        NotificationLabSession.Example(NotificationSeverity.Info, Lifetime()),
                        NotificationLabSession.Example(NotificationSeverity.Warning, Lifetime()),
                        NotificationLabSession.Example(NotificationSeverity.Error, Lifetime())
                    }, Timing());
            }
            if (DeucarianEditorButtons.Secondary("Add 10 mixed messages (overflow test)")) ShowMixed();
            EditorGUILayout.LabelField("Repeated active messages stay in one row. New simultaneous messages share one ping.",
                DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.LabelField(session.PingCount + " ping requests · last batch " + session.LastBatchSize,
                EditorStyles.wordWrappedMiniLabel);
            }
            DeucarianEditorCards.EndCard();
        }

        private void DrawLivePreview()
        {
            DeucarianEditorPreviewLabChrome.Begin("Lifecycle preview",
                runtimeConnection == null ? "Content and timing only. Theme, fade, scale, slide and lazy follow are shown in the running app."
                    : "These test messages also appear in your application's real list. Hardware warnings are not shown here.");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Resolve all", snapshot.Count > 0 || session.PendingCount > 0))
                    session.ResolveAll();
                if (DeucarianEditorButtons.Secondary("Reset session"))
                {
                    audio.Stop();
                    session.Reset();
                    lastCustomId = default;
                }
            }
            if (snapshot.Count == 0)
            {
                GUILayout.Space(22f);
                DeucarianEditorStatusPanel.DrawStatusCard(
                    session.PendingCount > 0 ? "Waiting for the show delay…" : "All clear. Show a message to begin.",
                    session.PendingCount > 0 ? DeucarianEditorStatus.Info : DeucarianEditorStatus.Success);
                GUILayout.Space(22f);
            }
            // Resolving a row may replace the snapshot synchronously. Keep this draw pass stable.
            NotificationSnapshot visible = NotificationVisibility.Select(snapshot, presentationSettings.Sanitized().maxVisible);
            for (int i = 0; i < visible.Count; i++)
            {
                NotificationItem item = visible[i];
                DeucarianEditorCards.DrawInlineCard(() =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DeucarianEditorStatusBadge.Draw(item.Definition.Severity.ToString(), Status(item.Definition.Severity),
                            GUILayout.Width(76f));
                        GUILayout.FlexibleSpace();
                        bool recovering = session.IsRecovering(item.Id);
                        if (DeucarianEditorButtons.Secondary(recovering ? "Recovering…" : "Resolve", !recovering,
                            GUILayout.Width(100f))) session.Resolve(item.Id);
                    }
                    EditorGUILayout.LabelField(item.Definition.Title, DeucarianEditorStyles.SectionTitle);
                    EditorGUILayout.LabelField(item.Definition.Body, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField("Episode " + item.Episode + " · Priority " + item.Definition.Priority,
                        DeucarianEditorStyles.MutedLabel);
                    EditorGUILayout.LabelField(LifetimeLabel(item), DeucarianEditorStyles.MutedLabel);
                });
            }
            if (snapshot.Count > visible.Count) EditorGUILayout.LabelField("+" + (snapshot.Count - visible.Count) + " more · still active",
                DeucarianEditorStyles.SectionTitle);
            DeucarianEditorPreviewLabChrome.End();
        }

        private void DrawAudio()
        {
            if (runtimeConnection != null)
            {
                DeucarianEditorCards.BeginCard("Runtime audio", subtitle: "Uses the running app's configured palette and playback.");
                EditorGUILayout.LabelField("New test messages request one ping through the real warning list. Editor clip audition is disabled to avoid double audio.",
                    EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("Volume, pitch and muting follow the application. Switch to Editor preview only to audition another palette.",
                    EditorStyles.wordWrappedLabel);
                DeucarianEditorCards.EndCard();
                return;
            }
            DeucarianEditorCards.BeginCard("Audio feedback", subtitle: "Choose any product palette, including your XR palette.");
            paletteSet = DeucarianEditorFields.DrawAssetFieldWithSelectButton("Palette Set", paletteSet);
            experience = (DeucarianAudioExperience)DeucarianEditorSegmentedControl.Draw((int)experience, ExperienceLabels);
            soundEnabled = EditorGUILayout.Toggle("Sound on new messages", soundEnabled);
            audio.Configure(paletteSet, experience, soundEnabled);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (DeucarianEditorButtons.Secondary("Use selected palette")) AdoptPaletteSelection();
                if (DeucarianEditorButtons.Secondary("Open Audio Palette Lab", paletteSet != null))
                    DeucarianAudioPaletteLabWindow.Open(paletteSet);
                if (DeucarianEditorButtons.Secondary("Stop sound")) audio.Stop();
            }
            EditorGUILayout.LabelField(audio.Status, DeucarianEditorStyles.MutedLabel);
            EditorGUILayout.LabelField(
                "Editor audition applies palette volume and pitch. Application mixing and spatial audio are tested in the running app.",
                DeucarianEditorStyles.MutedLabel);
            DeucarianEditorCards.EndCard();
        }

        private void AdoptPaletteSelection()
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

        private void ShowCustom()
        {
            audio.Configure(paletteSet, experience, soundEnabled && runtimeConnection == null);
            session.Show(new NotificationDefinition(lastCustomId, severity, messageTitle, messageBody,
                (int)severity * 10, NotificationLabSession.FeedbackRole(severity), Lifetime()), Timing());
        }

        private void AddCustom()
        {
            lastCustomId = new NotificationId("lab.custom." + ++nextCustomId);
            ShowCustom();
        }

        private void DrawDestination()
        {
            DeucarianEditorCards.BeginCard("Destination");
            runtimeTargets.Clear();
            var labels = new List<string> { "Editor preview only" };
            int selected = 0;
            foreach (NotificationEditorTarget target in NotificationEditorTargets.Capture())
            {
                if (!IsRuntimeTarget(target)) continue;
                runtimeTargets.Add(target);
                var component = (Component)target.View;
                labels.Add(component.gameObject.scene.name + " / " + component.name + " (" + component.GetType().Name + ")");
                if (runtimeConnection != null && ReferenceEquals(target, runtimeConnection.Target)) selected = runtimeTargets.Count;
            }
            int choice = EditorGUILayout.Popup("Destination", selected, labels.ToArray());
            if (choice != selected) SelectRuntimeTarget(choice == 0 ? null : runtimeTargets[choice - 1]);
            EditorGUILayout.LabelField(!EditorApplication.isPlaying
                    ? "Editor preview · start Play Mode to choose an application list. No camera is created."
                    : runtimeTargets.Count == 0 ? "Waiting for an active scene list. The app must create and activate a NotificationPresenter."
                    : runtimeConnection == null ? "Editor preview · choose a running list to test its visuals and sound."
                    : "Connected · test messages and visual overrides are removed on disconnect.",
                EditorStyles.wordWrappedLabel);
            if (!string.IsNullOrEmpty(runtimeStatus)) EditorGUILayout.LabelField(runtimeStatus, EditorStyles.wordWrappedLabel);
            DeucarianEditorCards.EndCard();
        }

        private static bool IsRuntimeTarget(NotificationEditorTarget target)
        {
            return EditorApplication.isPlaying && target.IsAvailable && target.View is Component component &&
                   component != null && component.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(component);
        }

        private void SelectRuntimeTarget(NotificationEditorTarget target)
        {
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

        private static DeucarianEditorStatus Status(NotificationSeverity value)
        {
            switch (value)
            {
                case NotificationSeverity.Success: return DeucarianEditorStatus.Success;
                case NotificationSeverity.Warning: return DeucarianEditorStatus.Warning;
                case NotificationSeverity.Error: return DeucarianEditorStatus.Error;
                default: return DeucarianEditorStatus.Info;
            }
        }

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
