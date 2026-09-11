using System;
using Deucarian.Theming;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Add to a configured notification list prefab once; callers use NotificationManager.</summary>
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent, RequireComponent(typeof(NotificationListView))]
    public sealed class NotificationHost : MonoBehaviour
    {
        [SerializeField] private bool registerAsDefault = true;
        [SerializeField] private DeucarianThemeAudioPlayer audioPlayer;
        private IDisposable registration;
        private NotificationService service;
        public NotificationService Service => service ??
            throw new InvalidOperationException("The notification host must be enabled.");

        private void OnEnable()
        {
            service = new NotificationService(new UnityUnscaledNotificationClock(),
                new Feedback(this), GetComponent<NotificationListView>());
            try { if (registerAsDefault) registration = NotificationManager.Bind(service); }
            catch { service.Dispose(); service = null; throw; }
        }

        private void Update() => service?.Tick();
        private void OnDisable()
        {
            registration?.Dispose();
            registration = null;
            service?.Dispose();
            service = null;
            GetComponent<NotificationListView>().Render(NotificationSnapshot.Empty);
        }

        private sealed class Feedback : INotificationFeedbackSink
        {
            private readonly NotificationHost host;
            public Feedback(NotificationHost host) { this.host = host; }
            public bool TryRequestFeedback(NotificationFeedbackRequest request) =>
                host.audioPlayer != null ? host.audioPlayer.PlayRoleById(request.RoleId) :
                ThemeAudio.IsConfigured && ThemeAudio.Play(request.RoleId);
        }
    }
}
