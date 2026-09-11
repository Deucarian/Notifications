using System;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Isolated editor motion specimen. It never drives a scene object or application state.</summary>
    internal sealed class NotificationLabMotionPreview : VisualElement, IDisposable
    {
        private readonly IVisualElementScheduledItem animation;
        private readonly Button play;
        private double startedAt;
        private bool playing;
        private bool disposed;
        public VisualElement Specimen { get; }
        public NotificationTransition Enter { get; set; } = NotificationTransition.Fade;
        public NotificationTransition Exit { get; set; } = NotificationTransition.Fade;
        public float EnterSeconds { get; set; } = 0.25f;
        public float ExitSeconds { get; set; } = 0.25f;
        public bool ReducedMotion { get; set; }

        public NotificationLabMotionPreview()
        {
            AddToClassList("dw-motion-preview");
            var stage = DeucarianEditorWorkspaceControls.Region(null, "dw-motion-stage");
            Specimen = DeucarianEditorWorkspaceControls.Region("motion-specimen", "dw-motion-specimen");
            stage.Add(Specimen); Add(stage);
            play = DeucarianEditorWorkspaceControls.IconButton("Preview motion", DeucarianEditorIconIds.Play,
                Play, DeucarianEditorButtonRole.Primary);
            play.name = "motion-preview-play";
            Add(DeucarianEditorWorkspaceControls.Actions(play));
            animation = schedule.Execute(Tick).Every(16);
            animation.Pause();
            RegisterCallback<DetachFromPanelEvent>(_ => Stop());
        }

        public void Play()
        {
            if (disposed) return;
            foreach (var child in Specimen.Children()) DeucarianEditorWorkspaceControls.Show(child, true);
            startedAt = EditorApplication.timeSinceStartup;
            playing = true;
            animation.Resume();
            Tick();
        }

        private static float Seconds(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Mathf.Clamp(value, 0, 10);
        private void Tick()
        {
            if (!playing || disposed) return;
            float enter = ReducedMotion ? 0 : Seconds(EnterSeconds);
            float exit = ReducedMotion ? 0 : Seconds(ExitSeconds);
            float elapsed = (float)(EditorApplication.timeSinceStartup - startedAt);
            if (elapsed < enter) Apply(Enter, elapsed / enter);
            else if (elapsed < enter + 0.8f) Apply(NotificationTransition.None, 1);
            else if (elapsed < enter + 0.8f + exit) Apply(Exit, 1 - (elapsed - enter - 0.8f) / exit);
            else if (elapsed < enter + 1.05f + exit) Apply(NotificationTransition.Fade, 0);
            else Stop();
        }

        private void Apply(NotificationTransition transition, float progress)
        {
            float value = Mathf.SmoothStep(0, 1, Mathf.Clamp01(progress));
            Specimen.style.opacity = transition == NotificationTransition.Fade ? value : 1;
            Specimen.transform.scale = Vector3.one * (transition == NotificationTransition.Scale ? value : 1);
            Specimen.transform.position = transition == NotificationTransition.Slide ? new Vector3((value - 1) * 120, 0, 0) : Vector3.zero;
        }

        public void Stop()
        {
            playing = false;
            animation.Pause();
            Apply(NotificationTransition.None, 1);
        }

        public void Dispose() { if (disposed) return; Stop(); disposed = true; play.SetEnabled(false); }
    }
}
