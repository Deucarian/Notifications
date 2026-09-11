using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    /// <summary>Preserves authored padding while allowing a themed line of text to grow its row.</summary>
    internal sealed class NotificationRowLayout
    {
        private readonly RectTransform row, titleRect, bodyRect, severityRect;
        private readonly TMP_Text title, body;
        private readonly LayoutElement element;
        private readonly Vector2 rowSize, titleSize, bodySize, bodyPosition, severitySize;
        private readonly float preferredHeight, gap, bottom;
        private TextMetrics titleMetrics, bodyMetrics;
        private bool measured;
        private readonly bool supported;

        internal NotificationRowLayout(RectTransform row, TMP_Text title, TMP_Text body, Graphic severity)
        {
            this.row = row; this.title = title; this.body = body;
            titleRect = title != null ? title.rectTransform : null;
            bodyRect = body != null ? body.rectTransform : null;
            severityRect = severity != null ? severity.rectTransform : null;
            if (row == null || titleRect == null || bodyRect == null) return;
            rowSize = row.sizeDelta; titleSize = titleRect.sizeDelta; bodySize = bodyRect.sizeDelta;
            bodyPosition = bodyRect.anchoredPosition;
            severitySize = severityRect != null ? severityRect.sizeDelta : Vector2.zero;
            element = row.GetComponent<LayoutElement>();
            preferredHeight = element != null ? element.preferredHeight : -1;
            gap = titleRect.anchoredPosition.y - titleRect.rect.height - bodyPosition.y;
            bottom = row.rect.height + bodyPosition.y - bodyRect.rect.height;
            supported = titleRect.parent == row && bodyRect.parent == row && gap >= 0 && bottom >= 0
                && titleRect.anchorMin.y == 1 && titleRect.anchorMax.y == 1
                && bodyRect.anchorMin.y == 1 && bodyRect.anchorMax.y == 1;
        }

        private NotificationRowLayout(RectTransform row, TMP_Text title, TMP_Text body, Graphic severity, NotificationRowLayout source)
        {
            this.row = row; this.title = title; this.body = body;
            titleRect = title != null ? title.rectTransform : null;
            bodyRect = body != null ? body.rectTransform : null;
            severityRect = severity != null ? severity.rectTransform : null;
            element = row != null ? row.GetComponent<LayoutElement>() : null;
            rowSize = source.rowSize; titleSize = source.titleSize; bodySize = source.bodySize;
            bodyPosition = source.bodyPosition; severitySize = source.severitySize;
            preferredHeight = source.preferredHeight; gap = source.gap; bottom = source.bottom;
            supported = source.supported && row != null && titleRect != null && bodyRect != null;
        }

        internal NotificationRowLayout CopyTo(RectTransform row, TMP_Text title, TMP_Text body, Graphic severity)
            => new NotificationRowLayout(row, title, body, severity, this);

        internal bool Fit(bool force = false)
        {
            if (!supported) return false;
            var nextTitle = new TextMetrics(title); var nextBody = new TextMetrics(body);
            if (!force && measured && nextTitle.Equals(titleMetrics) && nextBody.Equals(bodyMetrics)) return false;
            measured = true; titleMetrics = nextTitle; bodyMetrics = nextBody;
            float heading = RequiredHeight(title, titleSize.y);
            float instruction = RequiredHeight(body, bodySize.y);
            float height = -titleRect.anchoredPosition.y + heading + gap + instruction + bottom;
            Vector2 nextBodyPosition = new Vector2(bodyPosition.x, titleRect.anchoredPosition.y - heading - gap);
            bool changed = !Mathf.Approximately(row.rect.height, height) || bodyRect.anchoredPosition != nextBodyPosition;
            titleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, heading);
            bodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, instruction);
            bodyRect.anchoredPosition = nextBodyPosition;
            row.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (element != null) element.preferredHeight = Mathf.Max(preferredHeight, height);
            if (severityRect != null && severityRect.anchorMin.y == severityRect.anchorMax.y)
                severityRect.sizeDelta = new Vector2(severitySize.x, severitySize.y + height - rowSize.y);
            return changed;
        }

        internal void Restore()
        {
            if (!supported) return;
            row.sizeDelta = rowSize; titleRect.sizeDelta = titleSize; bodyRect.sizeDelta = bodySize;
            bodyRect.anchoredPosition = bodyPosition;
            if (severityRect != null) severityRect.sizeDelta = severitySize;
            if (element != null) element.preferredHeight = preferredHeight;
            measured = false;
        }

        private static float RequiredHeight(TMP_Text label, float authoredHeight)
        {
            if (label.font == null) return authoredHeight;
            // One full line must fit before TMP can ellipsize long text horizontally.
            float line = label.GetPreferredValues("Ag", float.PositiveInfinity, float.PositiveInfinity).y;
            return float.IsNaN(line) || float.IsInfinity(line) ? authoredHeight : Mathf.Max(authoredHeight, Mathf.Ceil(line) + 1);
        }

        private readonly struct TextMetrics
        {
            private readonly TMP_FontAsset font;
            private readonly float size, lineSpacing;
            private readonly FontStyles style;
            internal TextMetrics(TMP_Text text)
            { font = text.font; size = text.fontSize; lineSpacing = text.lineSpacing; style = text.fontStyle; }
            internal bool Equals(TextMetrics other) => font == other.font && size == other.size
                && style == other.style && lineSpacing == other.lineSpacing;
        }
    }
}
