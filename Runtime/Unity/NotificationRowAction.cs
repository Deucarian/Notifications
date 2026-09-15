using System;
using Deucarian.Theming;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Routes the default card's Resolve button to its explicitly bound lifecycle owner.</summary>
    [ExecuteAlways]
    public sealed class NotificationRowAction : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private DeucarianSelectableThemeColors colors;
        [SerializeField] private DeucarianGraphicThemeColor textColor;
        private Action<NotificationId> resolve;
        private NotificationId id;
        public Button Button => button;
        internal bool CanResolve => button != null && button.gameObject.activeSelf && resolve != null;

        public void Configure(Button target, DeucarianSelectableThemeColors themeColors, DeucarianGraphicThemeColor labelColor)
        {
            if (button != null) button.onClick.RemoveListener(Resolve);
            button = target; colors = themeColors; textColor = labelColor;
            if (isActiveAndEnabled && button != null) button.onClick.AddListener(Resolve);
        }

        internal void Bind(NotificationId notificationId, bool persistent, Action<NotificationId> handler)
        {
            id = notificationId; resolve = handler;
            if (button == null) return;
            button.gameObject.SetActive(persistent && handler != null);
            button.interactable = true;
            button.targetGraphic.raycastTarget = CanResolve;
        }

        internal void ApplyTheme(DeucarianTheme theme)
        {
            if (theme == null) return;
            if (colors != null) colors.ThemeOverride = theme;
            if (textColor != null) textColor.ThemeOverride = theme;
            colors?.ApplyTheme(theme);
            textColor?.ApplyTheme(theme);
        }

        private void Resolve()
        {
            if (!CanResolve || !button.interactable) return;
            button.interactable = false;
            try { resolve(id); }
            catch { button.interactable = true; throw; }
        }
        private void OnEnable() { if (button != null) button.onClick.AddListener(Resolve); }
        private void OnDisable() { if (button != null) button.onClick.RemoveListener(Resolve); }
    }
}
