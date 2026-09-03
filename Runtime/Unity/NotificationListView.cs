using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Reusable non-modal uGUI list for an immutable notification snapshot.</summary>
    public sealed class NotificationListView : MonoBehaviour, INotificationListView
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private NotificationRowView rowPrefab;
        [SerializeField, Range(0f, 1f)] private float normalizedX = 0.05f;
        [SerializeField, Range(0f, 1f)] private float normalizedY = 0.5f;
        [SerializeField] private bool relativeToSafeArea = true;

        private readonly List<NotificationRowView> rows = new List<NotificationRowView>();

        public int VisibleCount { get; private set; }

        public void Configure(
            RectTransform rowContainer,
            NotificationRowView template,
            float x = 0.05f,
            float y = 0.5f,
            bool useSafeArea = true)
        {
            container = rowContainer;
            rowPrefab = template;
            ConfigureAnchor(x, y, useSafeArea);
        }

        public void Render(NotificationSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (container == null || rowPrefab == null)
            {
                VisibleCount = 0;
                return;
            }

            EnsureCapacity(snapshot.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                bool visible = i < snapshot.Count;
                NotificationRowView row = rows[i];
                if (visible)
                {
                    row.Render(snapshot[i]);
                    row.transform.SetSiblingIndex(i);
                }

                if (row.gameObject.activeSelf != visible)
                {
                    row.gameObject.SetActive(visible);
                }
            }

            VisibleCount = snapshot.Count;
        }

        public void ConfigureAnchor(float x, float y, bool useSafeArea)
        {
            normalizedX = Mathf.Clamp01(x);
            normalizedY = Mathf.Clamp01(y);
            relativeToSafeArea = useSafeArea;
            ApplyAnchor();
        }

        private void EnsureCapacity(int count)
        {
            while (rows.Count < count)
            {
                NotificationRowView row = Instantiate(rowPrefab, container, false);
                row.name = rowPrefab.name + " " + rows.Count;
                rows.Add(row);
            }
        }

        private void ApplyAnchor()
        {
            if (container == null)
            {
                return;
            }

            Vector2 anchor = ResolveAnchor(normalizedX, normalizedY, relativeToSafeArea);
            container.anchorMin = anchor;
            container.anchorMax = anchor;
            container.pivot = new Vector2(0f, 0.5f);
            container.anchoredPosition = Vector2.zero;
        }

        private static Vector2 ResolveAnchor(float x, float y, bool useSafeArea)
        {
            x = Mathf.Clamp01(x);
            y = Mathf.Clamp01(y);
            if (!useSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return new Vector2(x, y);
            }

            Rect safe = Screen.safeArea;
            return new Vector2(
                (safe.xMin + safe.width * x) / Screen.width,
                (safe.yMin + safe.height * y) / Screen.height);
        }

        private void Awake()
        {
            ApplyAnchor();
        }

        private void OnValidate()
        {
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
            ApplyAnchor();
        }
    }
}
