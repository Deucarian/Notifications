using System;
using System.Collections.Generic;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Auditions resolved notification clips through Theming's editor preview service.</summary>
    internal sealed class NotificationLabAudio : INotificationFeedbackSink, IDisposable
    {
        private readonly IDeucarianAudioPreviewService preview;
        private readonly Dictionary<string, int> previousVariants = new Dictionary<string, int>();
        private int sequence;
        private bool ownsPreview;
        private bool disposed;

        public NotificationLabAudio(IDeucarianAudioPreviewService preview)
        {
            this.preview = preview ?? throw new ArgumentNullException(nameof(preview));
        }

        public DeucarianAudioPaletteSet PaletteSet { get; private set; }
        public DeucarianAudioExperience Experience { get; private set; }
        public bool Enabled { get; private set; }
        public bool IsAvailable => preview.IsAvailable;
        public string Status { get; private set; } = "No ping requested yet.";

        public void Configure(DeucarianAudioPaletteSet palette, DeucarianAudioExperience experience, bool enabled)
        {
            if (PaletteSet == palette && Experience == experience && Enabled == enabled) return;
            Stop();
            PaletteSet = palette;
            Experience = experience;
            Enabled = enabled;
            previousVariants.Clear();
            Status = enabled ? "Ready for the next activation." : "Sound is muted; ping requests are still counted.";
        }

        public bool TryRequestFeedback(NotificationFeedbackRequest request)
            => TryPlay(request, false);

        internal bool TryPreviewFeedback(NotificationFeedbackRequest request) => TryPlay(request, true);

        private bool TryPlay(NotificationFeedbackRequest request, bool explicitPreview)
        {
            if (disposed) return false;
            if (!DeucarianThemeRuntimeResolver.UseAudio)
            {
                Stop();
                Status = "Audio is off in Project setup. Ping requests are still counted.";
                return false;
            }
            if (!Enabled && !explicitPreview) { Status = "Muted ping request."; return false; }
            if (PaletteSet == null || !PaletteSet.TryResolveById(request.RoleId, Experience, out var resolution))
            {
                Status = "No cue found. Choose a palette set containing the feedback role.";
                return false;
            }
            if (resolution.Cue.IntentionalSilence)
            {
                Status = "This role is intentionally silent.";
                return false;
            }
            int previous = previousVariants.TryGetValue(request.RoleId, out int value) ? value : -1;
            if (!resolution.Cue.TrySelectVariant(++sequence, previous, out AudioClip clip, out int selected))
            {
                Status = "The resolved cue contains no audio clip.";
                return false;
            }
            if (!preview.IsAvailable)
            {
                Status = "Audio audition is unavailable in this editor session (including batch mode).";
                return false;
            }
            var processed = preview as IDeucarianProcessedAudioPreviewService;
            bool played = processed != null
                ? processed.PlayProcessed(clip, Mathf.Clamp01(resolution.Cue.Volume), resolution.Cue.ResolvePitch((sequence * 0.618034f) % 1))
                : preview.Play(clip);
            if (!played) { Status = processed?.LastError ?? "Unity could not start the clip preview."; return false; }
            ownsPreview = true;
            previousVariants[request.RoleId] = selected;
            Status = "Played " + clip.name + " · " + Experience;
            return true;
        }

        public void Stop()
        {
            if (!ownsPreview) return;
            ownsPreview = false;
            preview.Stop();
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            disposed = true;
        }
    }
}
