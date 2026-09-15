using Deucarian.Notifications.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Deucarian.Notifications.Editor
{
    /// <summary>Creates or repairs the package-owned reusable notification view defaults.</summary>
    public static class DeucarianNotificationPrefabFactory
    {
        private const string Root =
            "Packages/com.deucarian.notifications/Runtime/Resources/Deucarian/Notifications/Defaults";
        private const string StylePath = Root + "/DefaultNotificationViewStyle.asset";
        private const string PrefabPath = Root + "/DefaultNotificationList.prefab";
        private const string RowPath = Root + "/DefaultNotificationRow.prefab";
        private const string DefaultFontPath = "Packages/com.deucarian.theming/Runtime/Fonts/Inter-Regular SDF.asset";

        [MenuItem("Assets/Create/Deucarian/Notifications/Repair Default View Assets")]
        public static void GenerateDefaults()
        {
            EnsureFolders();
            NotificationViewStyle style = CreateOrRepairStyle();
            var parent = new GameObject("Notification default authoring", typeof(RectTransform));
            NotificationRowView template;
            try
            {
                var row = CreateRowTemplate((RectTransform)parent.transform, style);
                template = PrefabUtility.SaveAsPrefabAsset(row.gameObject, RowPath).GetComponent<NotificationRowView>();
            }
            finally { Object.DestroyImmediate(parent); }
            CreateOrRepairPrefab(template);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static NotificationViewStyle CreateOrRepairStyle()
        {
            NotificationViewStyle style =
                AssetDatabase.LoadAssetAtPath<NotificationViewStyle>(StylePath);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<NotificationViewStyle>();
                AssetDatabase.CreateAsset(style, StylePath);
            }

            style.Configure(
                new Color(0.20f, 0.65f, 1f, 1f),
                new Color(0.20f, 0.80f, 0.45f, 1f),
                new Color(1f, 0.70f, 0.15f, 1f),
                new Color(1f, 0.25f, 0.20f, 1f));
            EditorUtility.SetDirty(style);
            return style;
        }

        private static void CreateOrRepairPrefab(NotificationRowView template)
        {
            GameObject root = new GameObject(
                "Default Notification List",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(NotificationListView));
            try
            {
                RectTransform rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                CanvasGroup group = root.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;

                RectTransform container = CreateContainer(rootRect);
                root.GetComponent<NotificationListView>().Configure(
                    container,
                    template,
                    0.05f,
                    0.5f,
                    true);
                // Serialize the package reference, never a project-specific override used by the authoring editor.
                var serialized = new SerializedObject(root.GetComponent<NotificationListView>());
                serialized.FindProperty("useProjectRowPrefab").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RectTransform CreateContainer(RectTransform root)
        {
            GameObject value = new GameObject(
                "Rows",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            RectTransform rect = (RectTransform)value.transform;
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(620f, 0f);

            VerticalLayoutGroup layout = value.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = value.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rect;
        }

        internal static NotificationRowView CreateRowTemplate(
            RectTransform root,
            NotificationViewStyle style)
        {
            GameObject row = new GameObject(
                "Notification Row Template",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(NotificationRowView),
                typeof(NotificationRowDecoration));
            RectTransform rect = (RectTransform)row.transform;
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(620f, 132f);
            Image background = row.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.10f, 0.92f);
            background.raycastTarget = false;
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/CardSurface.png");
            background.type = Image.Type.Sliced;
            row.GetComponent<LayoutElement>().preferredHeight = 132f;
            var rim = CreateImage(rect, "Border", Vector2.zero, Vector2.zero);
            rim.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/CardBorder.png");
            rim.type = Image.Type.Sliced;
            rim.rectTransform.anchorMin = Vector2.zero;
            rim.rectTransform.anchorMax = Vector2.one;
            rim.rectTransform.offsetMin = rim.rectTransform.offsetMax = Vector2.zero;

            Image accent = CreateImage(rect, "Severity", new Vector2(28f, 0f), new Vector2(44f, 44f));
            accent.preserveAspect = true;
            Sprite LoadIcon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Icons/" + name + ".png");
            accent.sprite = LoadIcon("info");
            row.GetComponent<NotificationRowDecoration>().Configure(accent, rim, accent.sprite,
                LoadIcon("circle-check"), LoadIcon("triangle-alert"), LoadIcon("circle-x"));
            TextMeshProUGUI title = CreateText(rect, "Title", 24f, FontStyles.Bold, -24f, 490f, 32f);
            TextMeshProUGUI body = CreateText(rect, "Body", 23f, FontStyles.Normal, -66f, 490f, 42f);
            title.rectTransform.anchoredPosition = new Vector2(102f, -24f);
            body.rectTransform.anchoredPosition = new Vector2(102f, -66f);
            row.GetComponent<NotificationRowView>().Configure(title, body, accent, style, stretchSeverity: false);
            row.SetActive(false);
            return row.GetComponent<NotificationRowView>();
        }

        private static Image CreateImage(
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            GameObject value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = (RectTransform)value.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = value.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            RectTransform parent,
            string name,
            float size,
            FontStyles style,
            float top,
            float width,
            float height)
        {
            GameObject value = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)value.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, top);
            rect.sizeDelta = new Vector2(width, height);
            TextMeshProUGUI text = value.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontPath);
            if (text.font == null) throw new System.InvalidOperationException("Theming's bundled Inter font is required to generate notification defaults.");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static void EnsureFolders()
        {
            string[] parts = Root.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
