using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Unity
{
    internal sealed class NotificationRowVisualBaseline
    {
        private readonly Graphic background;
        private readonly Color backgroundColor;
        private readonly TextBaseline title, body;
        internal NotificationRowVisualBaseline(Graphic background, TMP_Text title, TMP_Text body)
        {
            this.background = background;
            backgroundColor = background != null ? background.color : Color.clear;
            this.title = new TextBaseline(title); this.body = new TextBaseline(body);
        }
        private NotificationRowVisualBaseline(Graphic background, TMP_Text title, TMP_Text body, NotificationRowVisualBaseline source)
        {
            this.background = background;
            backgroundColor = source.backgroundColor;
            this.title = new TextBaseline(title, source.title);
            this.body = new TextBaseline(body, source.body);
        }
        internal NotificationRowVisualBaseline CopyTo(Graphic background, TMP_Text title, TMP_Text body)
            => new NotificationRowVisualBaseline(background, title, body, this);
        internal NotificationRowAppearance Colors(Color severity) => new NotificationRowAppearance(backgroundColor, title.Color, body.Color, severity);
        internal void Restore()
        {
            if (background != null) background.color = backgroundColor;
            title.Restore(); body.Restore();
        }
        private readonly struct TextBaseline
        {
            private readonly TMP_Text target;
            private readonly TMP_FontAsset font;
            private readonly float size, characterSpacing, lineSpacing;
            private readonly FontStyles style;
            internal readonly Color Color;
            internal TextBaseline(TMP_Text target)
            {
                this.target = target; font = target != null ? target.font : null;
                Color = target != null ? target.color : UnityEngine.Color.white;
                size = target != null ? target.fontSize : 14;
                style = target != null ? target.fontStyle : FontStyles.Normal;
                characterSpacing = target != null ? target.characterSpacing : 0;
                lineSpacing = target != null ? target.lineSpacing : 0;
            }
            internal TextBaseline(TMP_Text target, TextBaseline source)
            {
                this.target = target; font = source.font; Color = source.Color;
                size = source.size; style = source.style;
                characterSpacing = source.characterSpacing; lineSpacing = source.lineSpacing;
            }
            internal void Restore()
            {
                if (target == null) return;
                if (font != null) target.font = font;
                target.color = Color; target.fontSize = size; target.fontStyle = style;
                target.characterSpacing = characterSpacing; target.lineSpacing = lineSpacing;
            }
        }
    }
}
