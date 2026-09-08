using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Bounded, keyed presentation. Exiting rows keep their slot until the transition finishes.</summary>
    public sealed class NotificationListView : MonoBehaviour, INotificationListView, INotificationPresentationTarget
    {
        private sealed class Slot
        {
            public NotificationRowView Row;
            public NotificationRowMotion Motion;
        }

        [SerializeField] private RectTransform container;
        [SerializeField] private NotificationRowView rowPrefab;
        [SerializeField, Range(0f, 1f)] private float normalizedX = 0.05f;
        [SerializeField, Range(0f, 1f)] private float normalizedY = 0.5f;
        [SerializeField] private bool relativeToSafeArea = true;
        [SerializeField] private NotificationPresentationSettings presentation = NotificationPresentationSettings.Default;
        [SerializeField, Min(0)] private float spacing = 10f;
        private readonly List<Slot> slots = new List<Slot>();
        private readonly Stack<NotificationRowView> pool = new Stack<NotificationRowView>();
        private NotificationSnapshot snapshot = NotificationSnapshot.Empty;
        private NotificationSnapshot selected = NotificationSnapshot.Empty;
        private TMP_Text overflow;
        private readonly NotificationFollowMotion followMotion = new NotificationFollowMotion();

        public int VisibleCount => selected.Count;
        public int OverflowCount => Math.Max(0, snapshot.Count - selected.Count);
        public int RenderedRowCount => slots.Count;
        public NotificationPresentationSettings Presentation => presentation.Sanitized();
        public bool SupportsLazyFollow
        {
            get
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return false;
                Canvas rootCanvas = canvas.rootCanvas;
                return rootCanvas.renderMode == RenderMode.WorldSpace ||
                       (rootCanvas.renderMode == RenderMode.ScreenSpaceCamera && rootCanvas.worldCamera != null);
            }
        }

        public void Configure(RectTransform rowContainer, NotificationRowView template,
            float x = 0.05f, float y = 0.5f, bool useSafeArea = true)
        {
            container = rowContainer;
            rowPrefab = template;
            ConfigureAnchor(x, y, useSafeArea);
        }

        public void ConfigurePresentation(NotificationPresentationSettings settings)
        {
            presentation = settings.Sanitized();
            if (!presentation.lazyFollow) followMotion.Restore(transform);
            foreach (Slot slot in slots) slot.Motion.Complete();
            for (int i = slots.Count - 1; i >= 0; i--)
                if (i >= presentation.maxVisible || !slots[i].Motion.IsShowing) Retire(i);
            Render(snapshot);
        }

        public void Render(NotificationSnapshot value)
        {
            snapshot = value ?? throw new ArgumentNullException(nameof(value));
            if (container == null || rowPrefab == null) { selected = NotificationSnapshot.Empty; return; }
            selected = NotificationVisibility.Select(snapshot, Presentation.maxVisible);
            PrepareLayout();
            Reconcile();
        }

        private void Reconcile()
        {
            bool animate = Application.isPlaying && isActiveAndEnabled;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                Slot slot = slots[i];
                bool wanted = selected.TryGet(slot.Row.NotificationId, out NotificationItem item);
                if (wanted) slot.Row.Render(item);
                if (wanted != slot.Motion.IsShowing) slot.Motion.SetVisible(wanted, Presentation, animate);
                if (!wanted && slot.Motion.IsHidden) Retire(i);
            }
            foreach (NotificationItem item in selected.Items)
            {
                if (slots.Count >= Presentation.maxVisible) break;
                if (slots.Exists(x => x.Row.NotificationId == item.Id)) continue;
                NotificationRowView row = pool.Count > 0 ? pool.Pop() : Instantiate(rowPrefab, container, false);
                row.gameObject.SetActive(true);
                row.name = "Notification " + item.Id.Value;
                row.Render(item);
                var motion = new NotificationRowMotion(row);
                slots.Add(new Slot { Row = row, Motion = motion });
                row.AppearanceChanged += RefreshOverflowColor;
                motion.SetVisible(true, Presentation, animate);
            }
            Layout();
        }

        private void Update()
        {
            bool released = false;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                slots[i].Motion.Advance(Time.unscaledDeltaTime);
                if (!slots[i].Motion.IsShowing && slots[i].Motion.IsHidden) { Retire(i); released = true; }
            }
            if (released) Reconcile();
        }

        private void Retire(int index)
        {
            NotificationRowView row = slots[index].Row;
            row.AppearanceChanged -= RefreshOverflowColor;
            row.gameObject.SetActive(false);
            pool.Push(row);
            slots.RemoveAt(index);
        }

        private void PrepareLayout()
        {
            // Own row positions so Unity layout passes cannot overwrite the slide transition.
            if (container.TryGetComponent<VerticalLayoutGroup>(out var layout)) layout.enabled = false;
            if (container.TryGetComponent<ContentSizeFitter>(out var fitter)) fitter.enabled = false;
            if (overflow != null) return;
            var label = new GameObject("Notification Overflow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.layer = container.gameObject.layer;
            label.transform.SetParent(container, false);
            overflow = label.GetComponent<TextMeshProUGUI>();
            TMP_Text templateLabel = rowPrefab.GetComponentInChildren<TMP_Text>(true);
            if (templateLabel != null) { overflow.font = templateLabel.font; overflow.fontSize = templateLabel.fontSize; }
            overflow.raycastTarget = false;
            overflow.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void Layout()
        {
            slots.Sort((a, b) => Rank(a.Row.NotificationId).CompareTo(Rank(b.Row.NotificationId)));
            float width = ((RectTransform)rowPrefab.transform).sizeDelta.x;
            float height = ((RectTransform)rowPrefab.transform).sizeDelta.y;
            if (rowPrefab.TryGetComponent<LayoutElement>(out var element) && element.preferredHeight > 0) height = element.preferredHeight;
            float y = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                Slot slot = slots[i];
                var rect = (RectTransform)slot.Row.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(width, height);
                rect.SetSiblingIndex(i);
                slot.Motion.Position(new Vector2(0, -y));
                y += height + spacing;
            }
            if (overflow != null)
            {
                overflow.gameObject.SetActive(OverflowCount > 0);
                overflow.text = "+" + OverflowCount + " more";
                var rect = (RectTransform)overflow.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = new Vector2(width, 32);
                rect.anchoredPosition = new Vector2(0, -y);
                RefreshOverflowColor();
            }
            container.sizeDelta = new Vector2(width, Mathf.Max(0, y - spacing) + (OverflowCount > 0 ? 32 + spacing : 0));
        }

        private void RefreshOverflowColor()
        {
            if (overflow != null && slots.Count > 0) overflow.color = slots[0].Row.BodyColor;
        }

        private int Rank(NotificationId id)
        {
            for (int i = 0; i < selected.Count; i++) if (selected[i].Id == id) return i;
            return int.MaxValue;
        }

        public void ConfigureAnchor(float x, float y, bool useSafeArea)
        {
            normalizedX = Mathf.Clamp01(x); normalizedY = Mathf.Clamp01(y); relativeToSafeArea = useSafeArea;
            ApplyAnchor();
        }

        private void ApplyAnchor()
        {
            if (container == null) return;
            Vector2 anchor = new Vector2(normalizedX, normalizedY);
            if (relativeToSafeArea && Screen.width > 0 && Screen.height > 0)
            {
                Rect safe = Screen.safeArea;
                anchor = new Vector2((safe.xMin + safe.width * normalizedX) / Screen.width,
                    (safe.yMin + safe.height * normalizedY) / Screen.height);
            }
            container.anchorMin = container.anchorMax = anchor;
            container.pivot = new Vector2(0, 0.5f);
            container.anchoredPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            followMotion.Restore(transform);
            foreach (Slot slot in slots) slot.Motion.Complete();
        }
        private void LateUpdate() => followMotion.Advance(transform,
            Presentation.lazyFollow && SupportsLazyFollow, Presentation.follow, Time.unscaledDeltaTime);
        private void OnEnable() { ApplyAnchor(); if (container != null && rowPrefab != null) Render(snapshot); }
        private void OnValidate() { presentation = Presentation; ApplyAnchor(); }
    }
}
