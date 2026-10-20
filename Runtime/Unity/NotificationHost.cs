using System;
using Deucarian.Common;
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
        [SerializeField] private NotificationCatalogAsset catalog;
        private IDisposable registration;
        private NotificationService service;
        public NotificationService Service => service ??
            throw new InvalidOperationException("The notification host must be enabled.");
        public NotificationListView View => GetComponent<NotificationListView>();

        /// <summary>Mounts the current package list and owns its service, scheduling and default registration.</summary>
        public static NotificationHost Create(RectTransform parent, DeucarianThemeAudioPlayer audioPlayer = null,
            NotificationCatalogAsset catalog = null, bool registerAsDefault = true)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (registerAsDefault && NotificationManager.IsConfigured)
                throw new InvalidOperationException("A default notification host is already configured.");
            if (TMPro.TMP_Settings.instance == null)
                throw new InvalidOperationException("Import TMP Essential Resources before creating a notification host.");
            var instance = Instantiate(NotificationViewDefaults.LoadListPrefab(), parent, false);
            instance.SetActive(false);
            try
            {
                foreach (var child in instance.GetComponentsInChildren<Transform>(true))
                    child.gameObject.layer = parent.gameObject.layer;
                var rect = (RectTransform)instance.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var group = instance.GetComponent<CanvasGroup>() ?? instance.AddComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                var host = instance.GetComponent<NotificationHost>() ?? instance.AddComponent<NotificationHost>();
                host.audioPlayer = audioPlayer;
                host.catalog = catalog;
                host.registerAsDefault = registerAsDefault;
                host.View.ConfigurePresentation(NotificationViewSettings.ResolvePresentation(NotificationPresentationSettings.Default));
                instance.SetActive(true);
                return host;
            }
            catch
            {
                UnityObjectUtility.DestroySafely(instance);
                throw;
            }
        }

        private void OnEnable()
        {
            if (TMPro.TMP_Settings.instance == null) throw new InvalidOperationException("NotificationHost '" + name + "' needs TextMeshPro settings. Import TMP Essential Resources from Unity's TextMeshPro menu, then enable this host again.");
            service = new NotificationService(new UnityUnscaledNotificationClock(),
                new Feedback(this), GetComponent<NotificationListView>(),
                catalog != null ? catalog : Resources.Load<NotificationCatalogAsset>(NotificationCatalogAsset.DefaultResourcePath));
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
                ThemeAudio.IsConfigured && ThemeAudio.Player.PlayRoleById(request.RoleId);
        }
    }
}
