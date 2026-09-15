using Deucarian.Notifications.Unity;
using Deucarian.Theming;
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
        private const string DefaultFontPath = "Packages/com.deucarian.theming/Runtime/Fonts/Inter-Regular SDF.asset";

        [MenuItem("Assets/Create/Deucarian/Notifications/Repair Default View Assets")]
        public static void GenerateDefaults() => GenerateDefaultsAt(Root);

        /// <summary>Exports authored defaults without modifying an installed, read-only package.</summary>
        public static void ExportDefaults(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder) || !assetFolder.StartsWith("Assets/") || assetFolder.Contains(".."))
                throw new System.ArgumentException("Choose a folder inside Assets.", nameof(assetFolder));
            GenerateDefaultsAt(assetFolder.TrimEnd('/'));
        }

        private static void GenerateDefaultsAt(string folder)
        {
            EnsureFolders(folder);
            NotificationViewStyle style = CreateOrRepairStyle(folder + "/DefaultNotificationViewStyle.asset");
            var parent = new GameObject("Notification default authoring", typeof(RectTransform));
            NotificationRowView template;
            try
            {
                var row = CreateRowTemplate((RectTransform)parent.transform, style);
                template = PrefabUtility.SaveAsPrefabAsset(row.gameObject, folder + "/DefaultNotificationRow.prefab").GetComponent<NotificationRowView>();
            }
            finally { Object.DestroyImmediate(parent); }
            CreateOrRepairPrefab(template, folder + "/DefaultNotificationList.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static NotificationViewStyle CreateOrRepairStyle(string path)
        {
            NotificationViewStyle style =
                AssetDatabase.LoadAssetAtPath<NotificationViewStyle>(path);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<NotificationViewStyle>();
                AssetDatabase.CreateAsset(style, path);
            }

            style.Configure(
                new Color(0.20f, 0.65f, 1f, 1f),
                new Color(0.20f, 0.80f, 0.45f, 1f),
                new Color(1f, 0.70f, 0.15f, 1f),
                new Color(1f, 0.25f, 0.20f, 1f));
            style.ConfigureSurface(DeucarianBuiltinColorRoleIds.Surface, .12f);
            EditorUtility.SetDirty(style);
            return style;
        }

        private static void CreateOrRepairPrefab(NotificationRowView template, string path)
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

                PrefabUtility.SaveAsPrefabAsset(root, path);
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
            rect.sizeDelta = new Vector2(1400f, 0f);

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
            row.SetActive(false);
            RectTransform rect = (RectTransform)row.transform;
            rect.SetParent(root, false);
            rect.sizeDelta = new Vector2(1400f, 108f);
            Image background = row.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.10f, 0.92f);
            background.raycastTarget = false;
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/CardSurface.png");
            background.type = Image.Type.Sliced;
            row.GetComponent<LayoutElement>().preferredHeight = 108f;
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
            TextMeshProUGUI title = CreateText(rect, "Title", 24f, FontStyles.Bold, -20f, 1100f, 28f);
            TextMeshProUGUI body = CreateText(rect, "Body", 23f, FontStyles.Normal, -58f, 1100f, 30f);
            title.rectTransform.anchoredPosition = new Vector2(102f, -20f);
            body.rectTransform.anchoredPosition = new Vector2(102f, -58f);
            CreateResolveButton(rect);
            row.GetComponent<NotificationRowView>().Configure(title, body, accent, style, stretchSeverity: false);
            row.SetActive(false);
            return row.GetComponent<NotificationRowView>();
        }

        private static void CreateResolveButton(RectTransform row)
        {
            var image = CreateImage(row, "Resolve", new Vector2(-28, 0), new Vector2(128, 54));
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, .5f);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/CardSurface.png");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = image.gameObject.AddComponent<DeucarianSelectableThemeColors>();
            DeucarianColorRole Role(string id) => AssetDatabase.LoadAssetAtPath<DeucarianColorRole>(
                "Packages/com.deucarian.theming/Runtime/Resources/Deucarian/Theming/Visual/Defaults/Roles/" + id + ".asset");
            colors.NormalRole = Role(DeucarianBuiltinColorRoleIds.UiNormal);
            colors.HighlightedRole = Role(DeucarianBuiltinColorRoleIds.UiHighlighted);
            colors.PressedRole = Role(DeucarianBuiltinColorRoleIds.UiPressed);
            colors.SelectedRole = Role(DeucarianBuiltinColorRoleIds.UiSelected);
            colors.DisabledRole = Role(DeucarianBuiltinColorRoleIds.UiDisabled);
            var label = CreateText(rect, "Label", 23, FontStyles.Normal, 0, 128, 54);
            label.text = "Resolve";
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var labelColor = label.gameObject.AddComponent<DeucarianGraphicThemeColor>();
            labelColor.ColorRole = Role(DeucarianBuiltinColorRoleIds.TextPrimary);
            row.gameObject.AddComponent<NotificationRowAction>().Configure(button, colors, labelColor);
            image.gameObject.SetActive(false);
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

        private static void EnsureFolders(string folder)
        {
            string[] parts = folder.Split('/');
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
